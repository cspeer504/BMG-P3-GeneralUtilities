using System;
using System.Collections.Generic;
using System.Linq;

namespace Packages.BMG.Misc
{
    public static class EventPayloadParsing
    {
        private static readonly Dictionary<string, Func<object, string>> s_payloadParsers =
            new Dictionary<string, Func<object, string>>();
        private static readonly object s_parsersLock = new object();

        /// <summary>Registers a custom payload stringifier for a specific event name.</summary>
        /// <remarks>This can be used to fix/enhance the "parameter" output of events.</remarks>
        /// <param name="eventName">The name of the event to custom parse.</param>
        /// <param name="parser">The method that will handle the custom parsing.</param>
        public static void RegisterPayloadParser(string eventName, Func<object, string> parser)
        {
            if (string.IsNullOrEmpty(eventName) || parser == null) {return;}
            lock (s_parsersLock)
            {
                s_payloadParsers[eventName] = parser;
            }
        }

        /// <summary>Removes a previously registered payload parser.</summary>
        /// <param name="eventName">The name of the event to remove custom parsing for.</param>
        public static void UnregisterPayloadParser(string eventName)
        {
            if (string.IsNullOrEmpty(eventName)) {return;}
            lock (s_parsersLock)
            {
                if (s_parsersLock != null) {s_payloadParsers.Remove(eventName);}
            }
        }

        public static string GetEventPayload(string eventName, object eventData)
        {
            string payload;

            // 1) Null fast-path
            if (eventData == null)
            {
                return "null";
            }

            // 2) Check for a Custom parser (exact event name match).
            Func<object, string> parser = null;
            lock (s_parsersLock)
            {
                if (s_payloadParsers.ContainsKey(eventName))
                {
                    parser = s_payloadParsers[eventName];
                }
            }
            if (parser != null)
            {
                try
                {
                    payload = parser(eventData);
                    if (!string.IsNullOrEmpty(payload)) {return "\"\"";}
                }
                catch
                {
                    // fall through to generic handling
                }
            }

            // 3) Generic fallback handling
            try
            {
                payload = eventData.ToString();
                if (string.IsNullOrEmpty(payload))
                {
                    payload = "\"\"";
                }
                else if (LooksLikeClassName(payload))
                {
                    payload = GetClassName(payload);
                }
                else
                {
                    // Try to pretty-print common generic types if ToString returned a type-like name
                    Type type = Type.GetType(payload);
                    if (type != null && type.IsGenericType)
                    {
                        string genericTypeName = type.Name; // e.g. "List`1"
                        int tickIndex = genericTypeName.IndexOf('`');
                        if (tickIndex >= 0)
                        {
                            genericTypeName = genericTypeName.Substring(0, tickIndex); // "List"
                        }

                        Type[] args = type.GetGenericArguments();
                        string[] argNames = new string[args.Length];
                        for (int i = 0; i < args.Length; i++)
                        {
                            argNames[i] = args[i].Name;
                        }

                        payload = genericTypeName + '<' + string.Join(", ", argNames) + '>';
                    }
                }
            }
            catch
            {
                payload = "N/A";
            }

            return payload;
        }

        /// <remarks> This isn't foolproof, but it's quicker than running reflection. </remarks>
        private static bool LooksLikeClassName(string line)
        {
            if (string.IsNullOrEmpty(line)) {return false;}

            // Must contain at least one period
            int lastDot = line.LastIndexOf('.');
            if(lastDot <= 0 || lastDot == line.Length - 1)
            {
                return false;
            }

            string className = line.Substring(lastDot + 1);

            // Class part should start with a letter (usually uppercase)
            if(! char.IsLetter(className[0]))
            {
                return false;
            }

            // Ensure all parts are valid identifiers
            string[] parts = line.Split('.');
            foreach (string part in parts)
            {
                if (string.IsNullOrEmpty(part)) {return false;}
                if (!char.IsLetter(part[0]) && part[0] != '_') {return false;}

                if(part.Any(c => ! (char.IsLetterOrDigit(c) || c == '_' || c == '[' || c == '<')))
                {
                    return false;
                }
            }

            return true;
        }

        private static string GetClassName(string line)
        {
            return line.Substring(line.LastIndexOf('.') + 1);
        }
    }
}