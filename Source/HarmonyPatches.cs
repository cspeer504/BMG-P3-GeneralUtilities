// Unity 5.6 / C# 4.0
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

using HarmonyLib;

using JetBrains.Annotations;

using Multimorphic.NetProcMachine;
using Multimorphic.NetProcMachine.Machine;
using Multimorphic.P3.Events;

using Packages.BMG.EventDebugger;
using Packages.BMG.ModeDebugger;

namespace Packages.BMG
{
    /// <summary>
    /// Initialize Harmony patches used by tooling in the BMG space.
    /// List of harmony packages:
    /// 1. Patches the <see cref="EventManager.Post"/> and <see cref="SafeEventManager.Post"/> methods to collect
    /// event information via <see cref="TrackedEvent"/> objects and store them in <see cref="TrackedEventRepository"/>
    /// for tooling like the EventViewerWindow to use. Also used to enable logging for every event string. Note that we
    /// cannot get event receiver details in this Harmony patch, but <see cref="EventConsoleTap"/> can parsing logs
    /// enabled by this patch to get receiver info.
    /// 
    /// 2. Patches <see cref="MachineController.AddMode"/> and <see cref="MachineController.RemoveMode"/> to collect
    /// mode information via <see cref="TrackedMode"/> objects and store them in <see cref="TrackedModeRepository"/> for
    /// tooling line ModeViewerWindow to use. This patch will also enable Mode Add/Remove logging if
    /// <see cref="Settings.Modes.Logging.Enabled"/> is set to true.
    /// </summary>
    public static class HarmonyPatches
    {
        private static Harmony s_sHarmony;

        public static void Init()
        {
            s_sHarmony = new Harmony("com.bmg.P3");

            MethodInfo eventManagerPost = typeof(EventManager).GetMethod("Post");
            MethodInfo eventManagerPostPrefix = typeof(EventTracking).GetMethod("EventManagerPostPrefix");
            s_sHarmony.Patch(eventManagerPost, prefix: new HarmonyMethod(eventManagerPostPrefix));

            MethodInfo safeEventManagerPost = typeof(SafeEventManager).GetMethod("Post");
            MethodInfo safeEventManagerPostPrefix = typeof(EventTracking).GetMethod("SafeEventManagerPostPrefix");
            s_sHarmony.Patch(safeEventManagerPost, prefix: new HarmonyMethod(safeEventManagerPostPrefix));

            s_sHarmony.PatchAll();
        }
    }

    /// <summary>
    /// Logs Event names just before they post.
    /// </summary>
    public class EventTracking
    {
        private static readonly HashSet<string> s_eventManagerEvents = new HashSet<string>();
        private static readonly Dictionary<SafeEventManager, HashSet<string>> s_safeEventManagerEvents =
            new Dictionary<SafeEventManager, HashSet<string>>();

        // ReSharper disable once UnusedMember.Global - Used by Harmony
        public static void EventManagerPostPrefix(string eventName, object eventData)
        {
            if (IsLoggableEvent(eventName))
            {
                TrackedEvent trackedEvent = new TrackedEvent();
                MethodBase method = new StackFrame(Settings.Events.Repository.StackFramesToCheckPosterMethod).GetMethod();
                trackedEvent.ClassName = (method.DeclaringType == null) ? "<NotAvailable>" : method.DeclaringType.FullName;
                trackedEvent.MethodName = method.Name;
                trackedEvent.Action = EventAction.PostedBy;
                trackedEvent.EventName = eventName;
                trackedEvent.Timestamp = DateTime.Now.ToString(Settings.Events.InEditorViewer.DateTimeFormat);
                trackedEvent.PayloadString = Misc.EventPayloadParsing.GetEventPayload(eventName, eventData);
                trackedEvent.Direction = EventDirection.ToModes;
                TrackedEventRepository.Add(trackedEvent);

                if (!s_eventManagerEvents.Contains(eventName))
                {
                    s_eventManagerEvents.Add(eventName);
                    EventManager.LogEventName(eventName, true);
                }
            }
        }

        // ReSharper disable once InconsistentNaming : __instance must be named this way.
        [PublicAPI]
        public static void SafeEventManagerPostPrefix(SafeEventManager __instance, string eventName, object eventData)
        {
            HashSet<string> seen;
            if (!s_safeEventManagerEvents.TryGetValue(__instance, out seen))
            {
                seen = new HashSet<string>();
                s_safeEventManagerEvents.Add(__instance, seen);
            }

            if (IsLoggableEvent(eventName))
            {
                TrackedEvent trackedEvent = new TrackedEvent();
                MethodBase method = new StackFrame(Settings.Events.Repository.StackFramesToCheckPosterMethod).GetMethod();
                trackedEvent.ClassName = (method.DeclaringType == null) ? "<NotAvailable>" : method.DeclaringType.FullName;
                trackedEvent.MethodName = method.Name;
                trackedEvent.Action = EventAction.PostedBy;
                trackedEvent.EventName = eventName;
                trackedEvent.Timestamp = DateTime.Now.ToString(Settings.Events.InEditorViewer.DateTimeFormat);
                trackedEvent.PayloadString = Misc.EventPayloadParsing.GetEventPayload(eventName, eventData);
                trackedEvent.Direction = EventDirection.ToGui;
                TrackedEventRepository.Add(trackedEvent);

                if((! seen.Contains(eventName)))
                {
                    seen.Add(eventName);
                    __instance.LogEventName(eventName, true);
                }
            }
        }

        private static bool IsLoggableEvent(string eventName)
        {
            for (int i = 0; i < Settings.Events.Repository.VerboseEvents.Count; i++)
            {
                string prefix = Settings.Events.Repository.VerboseEvents[i];
                if(! string.IsNullOrEmpty(prefix) && eventName.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return false;
                }
            }
            return true;
        }
    }

    /// <summary>
    /// Collaborates with <see cref="TrackedModeRepository"/> to keep track of active modes.
    /// </summary>
    // ReSharper disable once UnusedType.Global - used by Harmony
    public class ModeTracking
    {
        private static int s_indent;
        private static readonly StringBuilder s_sb = new StringBuilder(256);
        private static readonly object s_lock = new object();
        private const string _added = "Added";
        private const string _removed = "Removed:";

        /// <remarks>
        /// Designed to minimalize string allocations.
        /// Designed to be thread safe. StringBuild is not a thread-safe construct.
        /// </remarks>
        private static void TraceMethod(string method, Mode mode)
        {
            if(Settings.Modes.Logging.Enabled == false) { return; }

            lock (s_lock)
            {
                s_sb.Length = 0;

                s_sb.Append(Settings.Modes.Logging.ModeLogPrefix);

                if (s_indent > 0)
                {
                    for (int i = 0; i < s_indent; i++)
                    {
                        s_sb.Append(Settings.Modes.Logging.IndentString);
                    }
                    s_sb.Append(Settings.Modes.Logging.StartedByAnotherModeIndicatorString);
                }

                s_sb.Append(method);
                s_sb.Append(' ');
                s_sb.Append(mode);

                Multimorphic.P3App.Logging.Logger.Log(s_sb.ToString());
            }

        }

        // ReSharper disable once UnusedType.Global - used by Harmony
        [HarmonyPatch(typeof(MachineController), "AddMode")]
        public class AddModePatch
        {
            // ReSharper disable once ArrangeTypeMemberModifiers
            // ReSharper disable once UnusedMember.Local - used by Harmony
            static void Prefix(Mode mode)
            {
                TrackedModeRepository.SetEnableFlag(mode, true);
                TraceMethod(_added, mode);
                ++s_indent;
            }
            // ReSharper disable once ArrangeTypeMemberModifiers
            // ReSharper disable once UnusedMember.Local - used by Harmony
            static void Postfix()
            {
                --s_indent;
            }
        }
        // ReSharper disable once UnusedType.Global - used by Harmony
        [HarmonyPatch(typeof(MachineController), "RemoveMode")]
        public class RemoveModePatch
        {
            // ReSharper disable once ArrangeTypeMemberModifiers
            // ReSharper disable once UnusedMember.Local - used by Harmony
            static void Prefix(Mode mode)
            {
                TrackedModeRepository.SetEnableFlag(mode, false);
                TraceMethod(_removed, mode);
                ++s_indent;
            }
            // ReSharper disable once ArrangeTypeMemberModifiers
            // ReSharper disable once UnusedMember.Local - used by Harmony
            static void Postfix()
            {
                --s_indent;
            }
        }
    }
}
