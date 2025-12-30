// Unity 5.6 / C# 4.0
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Multimorphic.NetProcMachine;
using Multimorphic.P3;

using Packages.BMG.EventDebugger;
using Packages.BMG.ModeDebugger;

namespace Packages.BMG
{
    public static class Utility
    {
        /// <summary>
        /// Initialize the core functionality of tools and utilities.
        /// This call will make the P3 event managers log all events.
        /// </summary>
        public static void Init(P3Controller p3)
        {
            HarmonyPatches.Init();
            TrackedEventRepository.Clear();
            TrackedModeRepository.Clear();

            // Required for the EventViewerWindow to display event receivers.
            EventConsoleTap.Enable();
            Log.ShowPrivateEventDetails(p3);

            // Default Payload Parsers for common events. Obviously, there's no way to know if the sender of an event
            // changes how the payload is defined. These are mostly for convenience. You can override, remove, and add your own.
            Misc.EventPayloadParsing.RegisterPayloadParser("Evt_AddHighScoreCatsToShow", delegate(object o) { return "List<HighScoreCategory>"; });
        }

        /// <summary>
        /// Log related Utilities.
        /// </summary>
        public static class Log
        {
            /// <summary>
            /// Use to filter unwanted logging (i.e. verbose) if desired. This only works for logging done through
            /// <see cref="Multimorphic.P3App.Logging.Logger.Log(string)"/>. Any Logging through
            /// <see cref="UnityEngine.Debug"/> will not be filtered.
            /// </summary>
            /// <remarks>
            /// If both lists are not null or empty, the allowList will be checked first and the blockList will be checked
            /// ONLY IF there was a match in the allowList.
            /// All strings are case-sensitive when used to check for filtering.
            /// </remarks>
            /// <param name="allowList">Use if you only want the logging to write messages that INCLUDE any of the strings in this list.</param>
            /// <param name="blockList">Use if you want the logging to EXCLUDE any logs that of the strings in this list.</param>
            public static void Filter(List<string> allowList, List<string> blockList = null)
            {
                // Filter the log here. For performance reasons, don't overdo it.
                if (allowList != null)
                {
                    foreach (string allow in allowList)
                    {
                        if (!string.IsNullOrEmpty(allow))
                        {
                            Multimorphic.P3App.Logging.Logger.IncludeOnlyMessagesContaining.Add(allow);
                        }
                    }
                }

                if (blockList != null)
                {
                    foreach (string block in blockList)
                    {
                        if (!string.IsNullOrEmpty(block))
                        {
                            Multimorphic.P3App.Logging.Logger.ExcludeMessagesContaining.Add(block);
                        }
                    }
                }
            }

            /// <summary>
            /// Returns a string of all modes that were active at some point in this Unity application execution. Each
            /// mode name is on a new line.
            /// </summary>
            public static string GetAllModesUsed()
            {

                StringBuilder sb = new StringBuilder();
                foreach (TrackedMode mode in TrackedModeRepository.Modes.OrderBy(m => m.FullName))
                {
                    sb.AppendLine(mode.FullName);
                }

                return sb.ToString();
            }

            /// <summary>
            /// The following code is used to filter event logging of private token values, instead of generically
            /// referencing them as private.
            /// </summary>
            /// <param name="p3">Must be invoked by an entity that has access to the <see cref="P3Controller"/>.</param>
            /// <param name="show">If true, will show the private event details. If false, will hide the private event details.</param>
            public static void ShowPrivateEventDetails(P3Controller p3, bool show = true)
            {
                EventManager.FilterFromLog("Multimorphic.P3App.", !show);
                EventManager.FilterFromLog("Multimorphic.P3.", !show);
                EventManager.FilterFromLog("Multimorphic.NetProcMachine.", !show);

                p3.GUIToModesEventManager.FilterFromLog("Multimorphic.P3App.", !show);
                p3.GUIToModesEventManager.FilterFromLog("Multimorphic.P3.", !show);
                p3.GUIToModesEventManager.FilterFromLog("Multimorphic.NetProcMachine.", !show);
                p3.ModesToGUIEventManager.FilterFromLog("Multimorphic.P3App.", !show);
                p3.ModesToGUIEventManager.FilterFromLog("Multimorphic.P3.", !show);
                p3.ModesToGUIEventManager.FilterFromLog("Multimorphic.NetProcMachine.", !show);
            }
        }
    }
}