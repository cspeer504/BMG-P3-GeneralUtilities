// Unity 5.6 / C# 4.0
using System;
using System.Collections.Generic;
using System.Text;
using Packages.BMG.EventDebugger;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Packages.BMG.ModeDebugger
{
    public class ModeInGameOverlay : MonoBehaviour
    {
        [FormerlySerializedAs("mModeTexts")]
        [FormerlySerializedAs("m_modeTexts")]
        [Tooltip("The Text components to display the modes in. One per line.")]
        [SerializeField] private Text[] m_mModeTexts;

        private const string _emptyString = "";
        private ulong _lastReadVersion = 0;
        private bool _active = false;
        private bool _anyAging = false;

        // Performance helpers
        private HashSet<string> _boldSet; // O(1) tests for bold tokens
        private readonly StringBuilder _sb = new StringBuilder(32); // for postfix " (xN)" only
        private readonly List<TrackedMode> _snapshot = new List<TrackedMode>(256);

        private static readonly StringComparison s_ordinal = StringComparison.Ordinal;

        private void Awake()
        {
            BuildBoldSet();
            ClearList();
        }

        private void OnValidate()
        {
            BuildBoldSet();
        }

        private void OnEnable()
        {
            _active = true;
        }

        private void OnDisable()
        {
            _active = false;
            ClearList();
        }

        private void BuildBoldSet()
        {
            if (Settings.Modes.InGameOverlay.Decoration.BoldTextIfContains == null ||
                Settings.Modes.InGameOverlay.Decoration.BoldTextIfContains.Length == 0)
            {
                _boldSet = null;
                return;
            }

            if(_boldSet == null)
            {
                _boldSet = new HashSet<string>(StringComparer.Ordinal);
            }
            else
            {
                _boldSet.Clear();
            }

            for (int i = 0; i < Settings.Modes.InGameOverlay.Decoration.BoldTextIfContains.Length; i++)
            {
                string s = Settings.Modes.InGameOverlay.Decoration.BoldTextIfContains[i];
                if(! string.IsNullOrEmpty(s))
                {
                    _boldSet.Add(s);
                }
            }
        }

        private void Update()
        {
            if (!_active) {return;}

            // Keep repainting while any item/group is still aging
            if (TrackedEventRepository.Version == _lastReadVersion && !_anyAging) {return;}
            _anyAging = false;

            _lastReadVersion = TrackedEventRepository.Version;

            TrackedModeRepository.FillSnapshot(_snapshot);
            if (_snapshot.Count == 0)
            {
                ClearList();
                return;
            }

            // Clear all lines up-front (simple & cheap for UI)
            int maxLines = m_mModeTexts != null ? m_mModeTexts.Length : 0;
            for (int i = 0; i < maxLines; i++)
            {
                if (m_mModeTexts != null)
                {
                    m_mModeTexts[i].text = _emptyString;
                }
            }

            if (!Settings.Modes.InGameOverlay.Filters.ConsolidateConsecutiveModes)
            {
                // -------- No consolidation: one line per mode --------
                int iText = 0;
                for (int i = 0; i < _snapshot.Count && iText < maxLines; i++)
                {
                    TrackedMode mode = _snapshot[i];
                    if (mode == null) {continue;}
                    if (ShouldIgnore(mode.ClassName))
                    {
                        continue; // do not consume a line
                    }

                    Text text = m_mModeTexts[iText];

                    // Text & color (per-item Age lerp)
                    SetModeTextColor(mode, text);
                    ApplyBoldIfMatch(text);

                    if(! mode.IsAged)
                    {
                        _anyAging = true;
                    }

                    iText++;
                }
                return;
            }

            // -------- Consolidation: one-pass grouping O(n) --------
            // Group by (ClassName, IsOn, Priority) while preserving first-appearance order.
            Dictionary<string, GroupStats> groups = new Dictionary<string, GroupStats>(64, StringComparer.Ordinal);
            List<string> order = new List<string>(64);

            for (int i = 0; i < _snapshot.Count; i++)
            {
                TrackedMode m = _snapshot[i];
                if (m == null) {continue;}
                if (ShouldIgnore(m.ClassName)) {continue;}

                string key = BuildGroupKey(m.ClassName, m.IsOn, m.Priority);

                GroupStats gs;
                if (!groups.TryGetValue(key, out gs))
                {
                    gs = new GroupStats();
                    gs.FirstIndex = i;      // preserve display order
                    gs.Head = m;            // any representative; ToString() and IsOn/Priority are fine
                    gs.Count = 0;
                    gs.AnyNotAged = false;
                    gs.MinAge = 1f;
                    groups.Add(key, gs);
                    order.Add(key);
                }

                gs.Count++;
                if (!m.IsAged)
                {
                    gs.AnyNotAged = true;
                    float a = m.Age;
                    if (a < gs.MinAge) {gs.MinAge = a;}
                }

                groups[key] = gs; // struct copy-back for C# 4.0
            }

            // Emit in first-seen order, up to maxLines
            int write = 0;
            for (int i = 0; i < order.Count && write < maxLines; i++)
            {
                string key = order[i];
                GroupStats gs = groups[key];
                TrackedMode head = gs.Head;
                if (head == null) {continue;}

                Text text = m_mModeTexts[write];

                // Base line
                string line = head.ToString();
                if (gs.Count >= 2)
                {
                    // Build postfix without intermediate strings
                    _sb.Length = 0;
                    _sb.Append(line);
                    _sb.Append(" (x");
                    _sb.Append(gs.Count);
                    _sb.Append(')');
                    line = _sb.ToString();
                }

                text.text = line;

                if (head.IsOn)
                {
                    if (gs.AnyNotAged)
                    {
                        text.color = Color.Lerp(Settings.Modes.InGameOverlay.Decoration.JustActiveColor,
                            Settings.Modes.InGameOverlay.Decoration.ActiveColor, gs.MinAge);
                        _anyAging = true; // keep animating
                    }
                    else
                    {
                        text.color = Settings.Modes.InGameOverlay.Decoration.ActiveColor;
                    }
                }
                else
                {
                    text.color = Settings.Modes.InGameOverlay.Decoration.InactiveColor;
                }

                ApplyBoldIfMatch(text);

                write++;
            }
        }

        private struct GroupStats
        {
            public int FirstIndex;   // for display order (not used for sorting here, just preserved)
            public TrackedMode Head; // representative
            public int Count;
            public bool AnyNotAged;
            public float MinAge;
        }

        private bool ShouldIgnore(string className)
        {
            if (Settings.Modes.InGameOverlay.Filters.IgnoredModesPrefixedWith == null ||
                Settings.Modes.InGameOverlay.Filters.IgnoredModesPrefixedWith.Length == 0) {return false;}
            if (string.IsNullOrEmpty(className)) {return false;}

            for (int i = 0; i < Settings.Modes.InGameOverlay.Filters.IgnoredModesPrefixedWith.Length; i++)
            {
                string pre = Settings.Modes.InGameOverlay.Filters.IgnoredModesPrefixedWith[i];
                if (!string.IsNullOrEmpty(pre) && className.StartsWith(pre)) {return true;}
            }
            return false;
        }

        private static string BuildGroupKey(string className, bool isOn, int priority)
        {
            // Compact & stable key; C# 4.0 friendly (no tuples)
            return (className ?? string.Empty) + "|" + (isOn ? "1" : "0") + "|" + priority.ToString();
        }

        private void ApplyBoldIfMatch(Text text)
        {
            if (_boldSet == null)
            {
                text.fontStyle = FontStyle.Normal;
                return;
            }

            string s = text.text;
            if (string.IsNullOrEmpty(s))
            {
                text.fontStyle = FontStyle.Normal;
                return;
            }

            bool bold = false;
            foreach (string token in _boldSet)
            {
                if (!string.IsNullOrEmpty(token) && s.IndexOf(token, s_ordinal) >= 0)
                {
                    bold = true;
                    break;
                }
            }
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        }

        private void SetModeTextColor(TrackedMode mode, Text text)
        {
            // Keep your display format
            text.text = mode.ToString();

            if (mode.IsOn)
            {
                if (!mode.IsAged)
                {
                    text.color = Color.Lerp(Settings.Modes.InGameOverlay.Decoration.JustActiveColor, Settings.Modes.InGameOverlay.Decoration.ActiveColor, mode.Age);
                    _anyAging = true;
                }
                else
                {
                    text.color = Settings.Modes.InGameOverlay.Decoration.ActiveColor;
                }
            }
            else
            {
                text.color = Settings.Modes.InGameOverlay.Decoration.InactiveColor;
            }
        }

        private void ClearList()
        {
            if (m_mModeTexts == null) {return;}
            for (int i = 0; i < m_mModeTexts.Length; i++)
            {
                m_mModeTexts[i].text = _emptyString;
            }
        }
    }
}
