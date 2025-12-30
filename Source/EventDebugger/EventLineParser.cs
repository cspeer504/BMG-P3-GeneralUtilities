// Unity 5.6 / C# 4.0
using System;
using System.Text.RegularExpressions;

namespace Packages.BMG.EventDebugger
{
    public static class EventLineParser
    {
        // Example:
        // 20250913T10:02:04.675 : [DEV] Event: Evt_Y received by: Namespace.Type.Handler
        // Timestamp (group 1) is optional, "[DEV]" tag (if present) is ignored.
        private static readonly Regex s_received =
            new Regex(@"^\s*(?:(\d{8}T\d{2}:\d{2}:\d{2}\.\d{3}))?\s*:?\s*(?:\[[^\]]+\]\s*)?Event:\s*(\S+)\s+received by:\s*(.+?)\s*$",
                RegexOptions.Compiled);

        /// <summary>
        /// Try to parse received event details from a log line. Posted events are handled in
        /// <see cref="EventTracking.EventManagerPostPrefix"/>./> and <see cref="EventTracking.SafeEventManagerPostPrefix"/>.
        /// </summary>
        /// <remarks>There are a lot of assumptions on the format of the log line.</remarks>
        /// <returns>Returns true when the line contains an Event in received form.</returns>
        public static bool TryParse(string line, out TrackedEvent result)
        {
            result = null;
            if (string.IsNullOrEmpty(line)) { return false; }

            Match m = s_received.Match(line);
            if(! m.Success) { return false; }

            TrackedEvent pe = new TrackedEvent();
            // Choosing not to use the time stamp in the logs, to shorten the time string (no date values).
            //pe.Timestamp = SafeGroup(m, 1);
            pe.Timestamp = DateTime.Now.ToString(Settings.Events.InEditorViewer.DateTimeFormat);
            pe.EventName = SafeGroup(m, 2);
            pe.Action = EventAction.ReceivedBy;
            string[] parts = SafeGroup(m, 3).Split(new[] { '.' }, 2);
            pe.ClassName = parts[0];
            pe.MethodName  = parts[1];
            result = pe;
            return true;
        }

        private static string SafeGroup(Match m, int index)
        {
            Group g = m.Groups[index];
            return g != null ? g.Value : null;
        }
    }
}
