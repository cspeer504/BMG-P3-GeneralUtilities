// Unity 5.6 / C# 4.0
using System;
using System.Collections.Generic;
using System.Text;

using Packages.BMG.Misc;

using UnityEngine;
using UnityEngine.UI;

namespace Packages.BMG.EventDebugger
{
    public class EventInGameOverlay : MonoBehaviour
    {
        [Tooltip("The Text components to display the events in. One per line.")]
        [SerializeField] private Text[] m_eventTexts;
        [Tooltip("Cut off the text after this many characters. Too many characters will write into the Mode view area.")]
        [SerializeField] private int  m_maxCharactersPerLine = 154;

        private readonly List<TrackedEvent> _snapshot = new List<TrackedEvent>(256);

        private const string _emptyString = "";
        private ulong _lastReadVersion = 0;
        private bool _active = false;

        // Perf: prebuilt sets for O(1) lookups (Ordinal)
        private HashSet<string> _ignoredSet;
        private HashSet<string> _onlyPosterSet;
        private HashSet<string> _boldSet;

        // Perf: reuse one StringBuilder
        private readonly StringBuilder _sb = new StringBuilder(256);

        private static readonly StringComparison s_ordinal = StringComparison.Ordinal;

        private void Awake()
        {
            DontDestroyOnLoad(this.gameObject);
            EnsureSetsBuilt();
            ClearAllLines();
        }

        private void OnValidate()
        {
            // Rebuild sets when arrays change in Inspector
            EnsureSetsBuilt();
            m_maxCharactersPerLine = Math.Max(m_maxCharactersPerLine, 1);
        }

        private void OnEnable()
        {
            m_maxCharactersPerLine = Math.Max(m_maxCharactersPerLine, 1);
            _active = true;
        }

        private void OnDisable()
        {
            _active = false;
            ClearAllLines();
        }

        private void EnsureSetsBuilt()
        {
            _ignoredSet = BuildSet(Settings.Events.InGameOvervlay.Filters.IgnoredEvents);
            _onlyPosterSet = BuildSet(Settings.Events.InGameOvervlay.Filters.OnlyShowPosterForTheseEvents);
            _boldSet = BuildSet(Settings.Events.InGameOvervlay.Decoration.BoldTextIfContains);
        }

        private static HashSet<string> BuildSet(string[] items)
        {
            if (items == null || items.Length == 0)
            {
                return null;
            }

            HashSet<string> set = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < items.Length; i++)
            {
                if (!string.IsNullOrEmpty(items[i]))
                {
                    set.Add(items[i]);
                }
            }
            return set;
        }

        private void Update()
        {
            if (!_active)
            {
                return;
            }

            // Only redraw if repo changed
            if (TrackedEventRepository.Version == _lastReadVersion)
            {
                return;
            }

            // In the event the version change involved clearing the repo, just quickly clear out lines and move on.
            TrackedEventRepository.FillSnapshot(_snapshot);
            if (_snapshot == null || _snapshot.Count == 0)
            {
                ClearAllLines();
                _lastReadVersion = TrackedEventRepository.Version;
                return;
            }

            // Early exit if we have no UI lines
            int totalTexts = m_eventTexts != null ? m_eventTexts.Length : 0;
            if (totalTexts == 0)
            {
                _lastReadVersion = TrackedEventRepository.Version;

                return;
            }

            // Fill from bottom (newest) to top (older)
            int iEvent = _snapshot.Count - 1;    // newest
            int iText  = totalTexts - 1;          // last line
            int drawn  = 0;                       // how many we used

            while (iEvent >= 0 && iText >= 0)
            {
                TrackedEvent trackedEvent = _snapshot[iEvent];
                if (trackedEvent == null)
                {
                    iEvent--;
                    continue;
                }

                // Filters (O(1) lookups)
                if (_ignoredSet != null && _ignoredSet.Contains(trackedEvent.EventName))
                {
                    iEvent--; // skip event, do not consume UI line
                    continue;
                }

                if (_onlyPosterSet != null &&
                    trackedEvent.Action == EventAction.ReceivedBy &&
                    _onlyPosterSet.Contains(trackedEvent.EventName))
                {
                    iEvent--; // skip handler entries
                    continue;
                }

                Text text = m_eventTexts[iText];

                // Consolidation: count consecutive duplicates backward
                if (Settings.Events.InGameOvervlay.Filters.ConsolidateConsecutiveEvents)
                {
                    string line;
                    int consumed;
                    ConsolidateConsecutiveEventsBackward(_snapshot, iEvent, m_maxCharactersPerLine, out line, out consumed);

                    text.text = line;

                    // Decorators
                    bool isPosted = (trackedEvent.Action == EventAction.PostedBy);
                    if (isPosted)
                    {
                        bool isToModes = (trackedEvent.Direction == EventDirection.ToModes);
                        text.color = isToModes ?
                            Settings.Events.InGameOvervlay.Decoration.PostedToModesColor :
                            Settings.Events.InGameOvervlay.Decoration.PostedToGuiColor;
                    }
                    else
                    {
                        text.color = Settings.Events.InGameOvervlay.Decoration.HandlingColor;
                    }
                    text.fontStyle = ShouldBold(text.text) ? FontStyle.Bold : FontStyle.Normal;

                    drawn++;
                    iText--;
                    iEvent -= consumed;
                }
                else
                {
                    // No consolidation: format (truncate from left of ClassName) only if needed
                    string line = trackedEvent.ToString();
                    if (line.Length > m_maxCharactersPerLine)
                    {
                        line = FormatEventLine(trackedEvent, m_maxCharactersPerLine);
                    }

                    text.text = line;

                    bool isPosted = (trackedEvent.Action == EventAction.PostedBy);
                    if (isPosted)
                    {
                        bool isToModes = (trackedEvent.Direction == EventDirection.ToModes);
                        text.color = isToModes ?
                            Settings.Events.InGameOvervlay.Decoration.PostedToModesColor :
                            Settings.Events.InGameOvervlay.Decoration.PostedToGuiColor;
                    }
                    else
                    {
                        text.color = Settings.Events.InGameOvervlay.Decoration.HandlingColor;
                    }
                    text.fontStyle = ShouldBold(line) ? FontStyle.Bold : FontStyle.Normal;

                    drawn++;
                    iText--;
                    iEvent--;
                }
            }

            // Clear only the unused head
            for (int i = 0; i <= iText; i++)
            {
                if (!string.IsNullOrEmpty(m_eventTexts[i].text))
                {
                    m_eventTexts[i].text = _emptyString;
                }
            }

            _lastReadVersion = TrackedEventRepository.Version;
        }

        private bool ShouldBold(string text)
        {
            if (string.IsNullOrEmpty(text) || _boldSet == null)
            {
                return false;
            }

            // Iterate HashSet: we need "contains substring", so we traverse keys
            foreach (string token in _boldSet)
            {
                if (!string.IsNullOrEmpty(token) &&
                    text.IndexOf(token, s_ordinal) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        /// Consolidates consecutive duplicates backward from startIndex.
        private void ConsolidateConsecutiveEventsBackward(
            IList<TrackedEvent> eventsList,
            int startIndex,
            int maxChars,
            out string line,
            out int consumedCount)
        {
            line = string.Empty;
            consumedCount = 0;

            if (eventsList == null || eventsList.Count == 0 || startIndex < 0 || startIndex >= eventsList.Count)
            {
                return;
            }

            TrackedEvent head = eventsList[startIndex];
            if (head == null)
            {
                consumedCount = 1;
                return;
            }

            int count = 1;
            while (startIndex - count >= 0)
            {
                TrackedEvent prev = eventsList[startIndex - count];
                if (!AreConsecutiveDuplicates(head, prev))
                {
                    break;
                }

                count++;
            }

            int postfixLen = (count > 1) ? 4 /*" (x"*/ + CountDigits(count) + 1 /*")"*/ : 0;
            int baseBudget = maxChars - postfixLen;
            if (baseBudget < 0)
            {
                baseBudget = 0;
            }

            string baseLine = FormatEventLine(head, baseBudget);

            if (count > 1)
            {
                // Build final with single StringBuilder
                _sb.Length = 0;
                _sb.Append(baseLine);
                _sb.Append(" (x");
                AppendInt(_sb, count);
                _sb.Append(')');
                line = _sb.ToString();
            }
            else
            {
                line = baseLine;
            }

            consumedCount = count;
        }

        private static int CountDigits(int n)
        {
            if (n < 10)
            {
                return 1;
            }

            if (n < 100)
            {
                return 2;
            }

            if (n < 1000)
            {
                return 3;
            }

            if (n < 10000)
            {
                return 4;
            }

            return n.ToString().Length; // rare path
        }

        private static void AppendInt(StringBuilder sb, int n)
        {
            // Minimal alloc-free append for small n
            sb.Append(n);
        }

        private static bool AreConsecutiveDuplicates(TrackedEvent a, TrackedEvent b)
        {
            if (a == null || b == null)
            {
                return false;
            }

            if (a.Action != b.Action)
            {
                return false;
            }

            if (a.Action == EventAction.PostedBy)
            {
                if (a.Direction != b.Direction)
                {
                    return false;
                }
            }

            string aEvent = (a.EventName ?? string.Empty);
            string bEvent = (b.EventName ?? string.Empty);
            if (!string.Equals(aEvent, bEvent, StringComparison.Ordinal))
            {
                return false;
            }

            string aPayload = (a.PayloadString ?? string.Empty);
            string bPayload = (b.PayloadString ?? string.Empty);
            if (!string.Equals(aPayload, bPayload, StringComparison.Ordinal))
            {
                return false;
            }

            string aFull = (a.FullMethodName ?? string.Empty);
            string bFull = (b.FullMethodName ?? string.Empty);
            return string.Equals(aFull, bFull, StringComparison.Ordinal);
        }

        // Truncates from the beginning of ClassName; inserts "..." if trimmed.
        private string FormatEventLine(TrackedEvent e, int maxChars)
        {
            // Fast path: try cached ToString first
            string baseLine = e.ToString();
            if (baseLine.Length <= maxChars)
            {
                return baseLine;
            }

            // Build: "<Timestamp>  <EventName> <ActionDesc> " + class + "." + method
            string actionDesc = e.Action.GetDescription();
            string className  = e.ClassName ?? string.Empty;
            string methodName = e.MethodName ?? string.Empty;

            _sb.Length = 0;
            _sb.Append(e.Timestamp);
            _sb.Append("  ");

            _sb.Append(e.EventName);
            _sb.Append(' ');

            if (e.Action == EventAction.PostedBy)
            {
                _sb.Append('[');
                _sb.Append(e.Direction.GetDescription());
                _sb.Append(']');
                _sb.Append(' ');
            }

            _sb.Append(actionDesc);
            _sb.Append(' ');

            int idxClassStart = _sb.Length; // mark where className will start
            _sb.Append(className);
            _sb.Append('.');
            _sb.Append(methodName);

            if (e.Action == EventAction.PostedBy)
            {
                _sb.Append('(');
                _sb.Append(e.PayloadString);
                _sb.Append(')');
            }

            // If total fits, return
            if (_sb.Length <= maxChars)
            {
                return _sb.ToString();
            }

            // Compute budget for class name
            int suffixLen = 1 + methodName.Length; // "." + method
            if (e.Action == EventAction.PostedBy)
            {
                suffixLen += 2 + (e.PayloadString != null ? e.PayloadString.Length : 0);
            }
            int prefixLen = idxClassStart;
            int budgetForClass = maxChars - prefixLen - suffixLen;
            if (budgetForClass < 0)
            {
                budgetForClass = 0;
            }

            // If we must trim className, insert "..." and keep rightmost portion
            if (className.Length > budgetForClass)
            {
                int tailBudget = budgetForClass - 3; // reserve for "..."
                if (tailBudget < 0)
                {
                    tailBudget = 0;
                }

                int tailStart = className.Length - tailBudget;

                // Rebuild: prefix + "..." + class tail + "." + method
                _sb.Length = prefixLen;
                _sb.Append("...");
                // (StringBuilder.Append(string, int, int) not guaranteed in old profiles)
                string tail = (tailBudget > 0) ? className.Substring(tailStart, tailBudget) : string.Empty;
                _sb.Append(tail);
                _sb.Append('.');
                _sb.Append(methodName);
                if (e.Action == EventAction.PostedBy)
                {
                    _sb.Append("(");
                    _sb.Append(e.PayloadString);
                    _sb.Append(")");
                }

                // Final clamp if something exceeded (very rare)
                string s = _sb.ToString();
                if (s.Length > maxChars)
                {
                    s = s.Substring(s.Length - maxChars);
                }

                return s;
            }
            else
            {
                // No class trim needed; just clamp rightmost if somehow still long
                string s = _sb.ToString();
                if (s.Length > maxChars)
                {
                    s = s.Substring(s.Length - maxChars);
                }

                return s;
            }
        }

        private void ClearAllLines()
        {
            if (m_eventTexts == null)
            {
                return;
            }

            for (int i = 0; i < m_eventTexts.Length; i++)
            {
                if (m_eventTexts[i] != null)
                {
                    m_eventTexts[i].text = _emptyString;
                }
            }
        }
    }
}
