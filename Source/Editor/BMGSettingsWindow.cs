// Unity 5.6 / C# 4.0
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
// ReSharper disable BadControlBracesLineBreaks
// ReSharper disable MultipleStatementsOnOneLine

namespace Packages.BMG.Editor
{
    // ReSharper disable once InconsistentNaming - BMG is an acronym
    public class BMGSettingsWindow : EditorWindow
    {
        [MenuItem("BMG/Settings", false, 100)]
        public static void ShowWindow() { GetWindow<BMGSettingsWindow>("BMG Settings"); }

        // ---- UI state ----
        private Vector2 _scroll;
        private bool _fModes = true, _fModesLog = true, _fModesView = true, _fModesOverlay = true, _fModesOverlayDec = true, _fModesOverlayFil = true;
        private bool _fEvents = true, _fRepo = true, _fViewer = true, _fOverlay = true, _fOverlayDec = true, _fOverlayFil = true;

        // Working lists + UI lists
        private List<string> _modesBold, _modesIgnored;
        private List<string> _eventsVerbose, _eventsBold, _eventsIgnored, _eventsOnlyPoster;
        private ReorderableList _rlModesBold, _rlModesIgnored, _rlEventsVerbose, _rlEventsBold, _rlEventsIgnored, _rlEventsOnlyPoster;

        // ─────────────────────────────────────────────────────────────────────────────
        // Tooltips (mirrors your XML summaries)
        // ─────────────────────────────────────────────────────────────────────────────
        private static GUIContent L(string text, string tip) { return new GUIContent(text, tip); }

        // Modes/Logging
        private static readonly GUIContent s_modesLogEnabled    = L("Enabled", "Enables Mode Logging. Requires HarmonyPatches.Init() to be called.");
        private static readonly GUIContent s_modesLogIndent     = L("Indent String", "The string used for indenting sub-modes (when a mode adds another mode). Requires Enabled to be true.");
        private static readonly GUIContent s_modesLogStartedBy  = L("Added by Mode", "A string used to visually indicate that a mode was started by the previous mode. Requires Enabled to be true.");
        private static readonly GUIContent s_modesLogPrefix     = L("Log Prefix", "A prefix in the log string. Requires Enabled to be true.");

        // Modes/InEditorViewer
        private static readonly GUIContent s_modesViewerEnabledC   = L("Enabled Color", "The color of the mode text font when it is enabled.");
        private static readonly GUIContent s_modesViewerDisabledC  = L("Disabled Color", "The color of the mode text font when it is disabled.");
        private static readonly GUIContent s_modesViewerHighlightC = L("Highlight Color", "Background highlight color used when Mode Viewer find text matches.");

        // Modes/InGameOverlay Decoration + Filters
        private static readonly GUIContent s_modesOverlayJustC   = L("Just Active", "The color used right after a mode turns on, will lerp to Enabled.");
        private static readonly GUIContent s_modesOverlayActiveC = L("Active", "The color used when a mode is enabled and aged.");
        private static readonly GUIContent s_modesOverlayInactC  = L("Inactive", "The color used when a mode is disabled.");
        private static readonly GUIContent s_modesOverlayBoldArr = L("Bold If Contains *", "Bolds the text if it contains any of these strings. Useful for highlighting specific modes.");
        private static readonly GUIContent s_modesOverlayCons    = L("Consolidate", "When enabled, duplicate modes (same ClassName, IsOn, Priority) are shown as a single line with (xN).");
        private static readonly GUIContent s_modesOverlayIgnored = L("Ignored Prefixes *", "Modes that start with any of these strings will not show up in the list.");

        // Events/Repository (+ Verbose list belongs to repo in your latest layout)
        private static readonly GUIContent s_repoMaxCap  = L("Max Capacity", "Maximum number of events retained by the repository (ring buffer).");
        private static readonly GUIContent s_repoStack   = L("Stack Frames", "How many frames up the stack trace to check to identify the method that posted the event.");
        private static readonly GUIContent s_repoVerbose = L("Events not tracked *", "Events considered spammy that won’t be stored in the Event history repository or shown in the Event Viewer.");

        // Events/InEditorViewer
        private static readonly GUIContent s_eventsViewPostedC = L("PostedBy Color", "The color of the mode text font when it is one relating to the posting of an event.");
        private static readonly GUIContent s_eventsViewRecvC   = L("ReceivedBy Color", "The color of the mode text font when it is one relating to the receiving/handling of an event.");
        private static readonly GUIContent s_eventsViewFindC   = L("Find Highlight", "The color of the mode text background when it matches the highlight string.");
        private static readonly GUIContent s_eventsViewStripC  = L("Row Strip", "The color of the row strip background. Set fully transparent to disable.");
        private static readonly GUIContent s_eventsViewDTFmt   = L("DateTime Format", "The DateTime format string to use when displaying the time of an event occurrence.");

        // Events/InGameOverlay Decoration + Filters
        private static readonly GUIContent s_eventsOvPostedModesC = L("Posted Modes Color", "The color of the font that represents an event being posted to the Modes.");
        private static readonly GUIContent s_eventsOvPostedGuiC   = L("Posted GUI Color", "The color of the font that represents an event being posted to the GUIs.");
        private static readonly GUIContent s_eventsOvHandlingC    = L("Handling Color", "The color of the font that represents an event being handled.");
        private static readonly GUIContent s_eventsOvBoldArr      = L("Bold If Contains *", "Bolds the text if it contains any of these strings. Useful for highlighting specific events.");
        private static readonly GUIContent s_eventsOvCons         = L("Consolidate", "Multiple consecutive events with the same name will be consolidated into one line with (xN).");
        private static readonly GUIContent s_eventsOvIgnored      = L("Ignored Events *", "Events with this name will not show up in the list.");
        private static readonly GUIContent s_eventsOvOnlyPoster   = L("Only Show Poster For *", "Events with this name will only show the poster to save space.");

        private const string _changesApplyOutsidePlayMode = "Changes apply outside of play mode.";

        private void OnEnable() { BuildWorkingListsFromAsset(); }

        private void OnGUI()
        {
            if (_rlEventsVerbose == null) { BuildWorkingListsFromAsset(); }
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "These settings are stored in a ScriptableObject asset (per-project).\n\n" +
                "Value changes to settings with * inside of play mode require play mode to stop and start again (domain refresh).",
                MessageType.Info
            );
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Be sure to invoke Packages.BMG.Utilities.Init(p3) in your BaseGameMode for these utilities to be 100% effective.",
                MessageType.Warning
            );
            EditorGUILayout.Space();

            DrawModes();
            EditorGUILayout.Space();
            DrawEvents();

            EditorGUILayout.EndScrollView();
        }

        // ====== Sections ======
        private void DrawModes()
        {
            _fModes = EditorGUILayout.Foldout(_fModes, "Modes", true);
            if (!_fModes)
            {
                return;
            }

            EditorGUI.indentLevel++;

            _fModesLog = EditorGUILayout.Foldout(_fModesLog, "Logging", true);
            if (_fModesLog)
            {
                EditorGUI.indentLevel++;

                // Enabled
                bool enabled = EditorGUILayout.Toggle(s_modesLogEnabled, Settings.Modes.Logging.Enabled);
                if (enabled != Settings.Modes.Logging.Enabled)
                {
                    Settings.Modes.Logging.Enabled = enabled;
                    Settings.Save();
                }

                EditorGUI.BeginDisabledGroup(!Settings.Modes.Logging.Enabled);

                // Indent, AddedBy, Prefix (dimmed when disabled)
                string indent = EditorGUILayout.TextField(s_modesLogIndent, Settings.Modes.Logging.IndentString ?? string.Empty);
                if (indent != Settings.Modes.Logging.IndentString) { Settings.Modes.Logging.IndentString = indent; Settings.Save(); }

                string startedBy = EditorGUILayout.TextField(s_modesLogStartedBy, Settings.Modes.Logging.StartedByAnotherModeIndicatorString ?? string.Empty);
                if (startedBy != Settings.Modes.Logging.StartedByAnotherModeIndicatorString) { Settings.Modes.Logging.StartedByAnotherModeIndicatorString = startedBy; Settings.Save(); }

                string prefix = EditorGUILayout.TextField(s_modesLogPrefix, Settings.Modes.Logging.ModeLogPrefix ?? string.Empty);
                if (prefix != Settings.Modes.Logging.ModeLogPrefix) { Settings.Modes.Logging.ModeLogPrefix = prefix; Settings.Save(); }

                EditorGUI.EndDisabledGroup();

                EditorGUI.indentLevel--;
            }

            _fModesView = EditorGUILayout.Foldout(_fModesView, "Modes: In-Editor Viewer", true);
            if (_fModesView)
            {
                EditorGUI.indentLevel++;

                Color c1 = EditorGUILayout.ColorField(s_modesViewerEnabledC, Settings.Modes.InEditorViewer.EnabledColor);
                if (c1 != Settings.Modes.InEditorViewer.EnabledColor) { Settings.Modes.InEditorViewer.EnabledColor = c1; Settings.Save(); }

                Color c2 = EditorGUILayout.ColorField(s_modesViewerDisabledC, Settings.Modes.InEditorViewer.DisabledColor);
                if (c2 != Settings.Modes.InEditorViewer.DisabledColor) { Settings.Modes.InEditorViewer.DisabledColor = c2; Settings.Save(); }

                Color c3 = EditorGUILayout.ColorField(s_modesViewerHighlightC, Settings.Modes.InEditorViewer.HighlightColor);
                if (c3 != Settings.Modes.InEditorViewer.HighlightColor) { Settings.Modes.InEditorViewer.HighlightColor = c3; Settings.Save(); }

                EditorGUI.indentLevel--;
            }

            _fModesOverlay = EditorGUILayout.Foldout(_fModesOverlay, "Modes: In-Game Overlay", true);
            if (_fModesOverlay)
            {
                EditorGUI.indentLevel++;

                _fModesOverlayDec = EditorGUILayout.Foldout(_fModesOverlayDec, "Decoration", true);
                if (_fModesOverlayDec)
                {
                    EditorGUI.indentLevel++;

                    Color jc = EditorGUILayout.ColorField(s_modesOverlayJustC, Settings.Modes.InGameOverlay.Decoration.JustActiveColor);
                    if (jc != Settings.Modes.InGameOverlay.Decoration.JustActiveColor) { Settings.Modes.InGameOverlay.Decoration.JustActiveColor = jc; Settings.Save(); }

                    Color ac = EditorGUILayout.ColorField(s_modesOverlayActiveC, Settings.Modes.InGameOverlay.Decoration.ActiveColor);
                    if (ac != Settings.Modes.InGameOverlay.Decoration.ActiveColor) { Settings.Modes.InGameOverlay.Decoration.ActiveColor = ac; Settings.Save(); }

                    Color ic = EditorGUILayout.ColorField(s_modesOverlayInactC, Settings.Modes.InGameOverlay.Decoration.InactiveColor);
                    if (ic != Settings.Modes.InGameOverlay.Decoration.InactiveColor) { Settings.Modes.InGameOverlay.Decoration.InactiveColor = ic; Settings.Save(); }

                    _rlModesBold.DoLayoutListWithHeaderTooltip(s_modesOverlayBoldArr.text, s_modesOverlayBoldArr.tooltip);

                    EditorGUI.indentLevel--;
                }

                _fModesOverlayFil = EditorGUILayout.Foldout(_fModesOverlayFil, "Filters", true);
                if (_fModesOverlayFil)
                {
                    EditorGUI.indentLevel++;

                    bool cons = EditorGUILayout.Toggle(s_modesOverlayCons, Settings.Modes.InGameOverlay.Filters.ConsolidateConsecutiveModes);
                    if (cons != Settings.Modes.InGameOverlay.Filters.ConsolidateConsecutiveModes) { Settings.Modes.InGameOverlay.Filters.ConsolidateConsecutiveModes = cons; Settings.Save(); }

                    _rlModesIgnored.DoLayoutListWithHeaderTooltip(s_modesOverlayIgnored.text, s_modesOverlayIgnored.tooltip);

                    EditorGUI.indentLevel--;
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.indentLevel--;
        }

        private void DrawEvents()
        {
            _fEvents = EditorGUILayout.Foldout(_fEvents, "Events", true);
            if (!_fEvents)
            {
                return;
            }

            EditorGUI.indentLevel++;

            _fRepo = EditorGUILayout.Foldout(_fRepo, "Repository", true);
            if (_fRepo)
            {
                EditorGUI.indentLevel++;

                int cap = EditorGUILayout.IntField(s_repoMaxCap, Settings.Events.Repository.MaxCapacity);
                if (cap != Settings.Events.Repository.MaxCapacity) { Settings.Events.Repository.MaxCapacity = Mathf.Max(0, cap); Settings.Save(); }

                int stack = EditorGUILayout.IntField(s_repoStack, Settings.Events.Repository.StackFramesToCheckPosterMethod);
                if (stack != Settings.Events.Repository.StackFramesToCheckPosterMethod) { Settings.Events.Repository.StackFramesToCheckPosterMethod = stack; Settings.Save(); }

                _rlEventsVerbose.DoLayoutListWithHeaderTooltip(s_repoVerbose.text, s_repoVerbose.tooltip);

                EditorGUI.indentLevel--;
            }

            _fViewer = EditorGUILayout.Foldout(_fViewer, "Events: In-Editor Viewer", true);
            if (_fViewer)
            {
                EditorGUI.indentLevel++;

                Color p = EditorGUILayout.ColorField(s_eventsViewPostedC, Settings.Events.InEditorViewer.PostedByColor);
                if (p != Settings.Events.InEditorViewer.PostedByColor) { Settings.Events.InEditorViewer.PostedByColor = p; Settings.Save(); }

                Color r = EditorGUILayout.ColorField(s_eventsViewRecvC, Settings.Events.InEditorViewer.ReceivedByColor);
                if (r != Settings.Events.InEditorViewer.ReceivedByColor) { Settings.Events.InEditorViewer.ReceivedByColor = r; Settings.Save(); }

                Color fh = EditorGUILayout.ColorField(s_eventsViewFindC, Settings.Events.InEditorViewer.FindHighlightColor);
                if (fh != Settings.Events.InEditorViewer.FindHighlightColor) { Settings.Events.InEditorViewer.FindHighlightColor = fh; Settings.Save(); }

                Color rs = EditorGUILayout.ColorField(s_eventsViewStripC, Settings.Events.InEditorViewer.RowStripColor);
                if (rs != Settings.Events.InEditorViewer.RowStripColor) { Settings.Events.InEditorViewer.RowStripColor = rs; Settings.Save(); }

                string fmt = EditorGUILayout.TextField(s_eventsViewDTFmt, Settings.Events.InEditorViewer.DateTimeFormat ?? string.Empty);
                if (fmt != Settings.Events.InEditorViewer.DateTimeFormat) { Settings.Events.InEditorViewer.DateTimeFormat = fmt; Settings.Save(); }

                EditorGUI.indentLevel--;
            }

            _fOverlay = EditorGUILayout.Foldout(_fOverlay, "Events: In-Game Overlay", true);
            if (_fOverlay)
            {
                EditorGUI.indentLevel++;

                _fOverlayDec = EditorGUILayout.Foldout(_fOverlayDec, "Decoration", true);
                if (_fOverlayDec)
                {
                    EditorGUI.indentLevel++;

                    Color pm = EditorGUILayout.ColorField(s_eventsOvPostedModesC, Settings.Events.InGameOvervlay.Decoration.PostedToModesColor);
                    if (pm != Settings.Events.InGameOvervlay.Decoration.PostedToModesColor) { Settings.Events.InGameOvervlay.Decoration.PostedToModesColor = pm; Settings.Save(); }

                    Color pg = EditorGUILayout.ColorField(s_eventsOvPostedGuiC, Settings.Events.InGameOvervlay.Decoration.PostedToGuiColor);
                    if (pg != Settings.Events.InGameOvervlay.Decoration.PostedToGuiColor) { Settings.Events.InGameOvervlay.Decoration.PostedToGuiColor = pg; Settings.Save(); }

                    Color hc = EditorGUILayout.ColorField(s_eventsOvHandlingC, Settings.Events.InGameOvervlay.Decoration.HandlingColor);
                    if (hc != Settings.Events.InGameOvervlay.Decoration.HandlingColor) { Settings.Events.InGameOvervlay.Decoration.HandlingColor = hc; Settings.Save(); }

                    _rlEventsBold.DoLayoutListWithHeaderTooltip(s_eventsOvBoldArr.text, s_eventsOvBoldArr.tooltip);

                    EditorGUI.indentLevel--;
                }

                _fOverlayFil = EditorGUILayout.Foldout(_fOverlayFil, "Filters", true);
                if (_fOverlayFil)
                {
                    EditorGUI.indentLevel++;

                    bool cons = EditorGUILayout.Toggle(s_eventsOvCons, Settings.Events.InGameOvervlay.Filters.ConsolidateConsecutiveEvents);
                    if (cons != Settings.Events.InGameOvervlay.Filters.ConsolidateConsecutiveEvents) { Settings.Events.InGameOvervlay.Filters.ConsolidateConsecutiveEvents = cons; Settings.Save(); }

                    _rlEventsIgnored.DoLayoutListWithHeaderTooltip(s_eventsOvIgnored.text, s_eventsOvIgnored.tooltip);
                    _rlEventsOnlyPoster.DoLayoutListWithHeaderTooltip(s_eventsOvOnlyPoster.text, s_eventsOvOnlyPoster.tooltip);

                    EditorGUI.indentLevel--;
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.indentLevel--;
        }

        // ====== Lists & plumbing ======
        private void BuildWorkingListsFromAsset()
        {
            // Modes overlay lists
            _modesBold    = new List<string>(Settings.Modes.InGameOverlay.Decoration.BoldTextIfContains ?? new string[0]);
            _modesIgnored = new List<string>(Settings.Modes.InGameOverlay.Filters.IgnoredModesPrefixedWith ?? new string[0]);

            _rlModesBold = MakeList(_changesApplyOutsidePlayMode, _modesBold, delegate
            {
                Settings.Modes.InGameOverlay.Decoration.BoldTextIfContains = _modesBold.ToArray();
                Settings.Save();
            });

            _rlModesIgnored = MakeList(_changesApplyOutsidePlayMode, _modesIgnored, delegate
            {
                Settings.Modes.InGameOverlay.Filters.IgnoredModesPrefixedWith = _modesIgnored.ToArray();
                Settings.Save();
            });

            // Events repo verbose
            _eventsVerbose = new List<string>(Settings.Events.Repository.VerboseEvents != null
                ? Settings.Events.Repository.VerboseEvents.ToArray()
                : new string[0]);

            _rlEventsVerbose = MakeList(_changesApplyOutsidePlayMode, _eventsVerbose, delegate
            {
                Settings.Events.Repository.VerboseEvents = new System.Collections.ObjectModel.ReadOnlyCollection<string>(
                    new List<string>(_eventsVerbose)
                );
                Settings.Save();
            });

            // Events overlay lists
            _eventsBold       = new List<string>(Settings.Events.InGameOvervlay.Decoration.BoldTextIfContains ?? new string[0]);
            _eventsIgnored    = new List<string>(Settings.Events.InGameOvervlay.Filters.IgnoredEvents ?? new string[0]);
            _eventsOnlyPoster = new List<string>(Settings.Events.InGameOvervlay.Filters.OnlyShowPosterForTheseEvents ?? new string[0]);

            _rlEventsBold = MakeList(_changesApplyOutsidePlayMode, _eventsBold, delegate
            {
                Settings.Events.InGameOvervlay.Decoration.BoldTextIfContains = _eventsBold.ToArray();
                Settings.Save();
            });
            _rlEventsIgnored = MakeList(_changesApplyOutsidePlayMode, _eventsIgnored, delegate
            {
                Settings.Events.InGameOvervlay.Filters.IgnoredEvents = _eventsIgnored.ToArray();
                Settings.Save();
            });
            _rlEventsOnlyPoster = MakeList(_changesApplyOutsidePlayMode, _eventsOnlyPoster, delegate
            {
                Settings.Events.InGameOvervlay.Filters.OnlyShowPosterForTheseEvents = _eventsOnlyPoster.ToArray();
                Settings.Save();
            });
        }

        private ReorderableList MakeList(string header, List<string> backing, System.Action onChanged)
        {
            ReorderableList rl = new ReorderableList(backing, typeof(string), true, true, true, true);
            rl.drawHeaderCallback = delegate(Rect r)
            {
                EditorGUI.LabelField(r, new GUIContent(header, "Click to edit. Drag to reorder. Add/remove entries with the +/- buttons."), EditorStyles.boldLabel);
            };
            rl.drawElementCallback = delegate(Rect rect, int index, bool isActive, bool isFocused)
            {
                rect.height = EditorGUIUtility.singleLineHeight;
                rect.y += 1f;
                if (index < 0 || index >= backing.Count)
                {
                    return;
                }

                string cur = backing[index] ?? string.Empty;
                string next = EditorGUI.TextField(rect, new GUIContent("", "Edit list item"), cur);
                if (next != cur) { backing[index] = next; if (onChanged != null)
                    {
                        onChanged();
                    }
                }
            };
            rl.onAddCallback = delegate
            {
                backing.Add(string.Empty); if (onChanged != null)
                {
                    onChanged();
                }
            };
            rl.onRemoveCallback = delegate(ReorderableList list)
            {
                if (list.index >= 0 && list.index < backing.Count) { backing.RemoveAt(list.index); if (onChanged != null)
                    {
                        onChanged();
                    }
                }
            };
            rl.onReorderCallback = delegate
            {
                if (onChanged != null)
                {
                    onChanged();
                }
            };
            return rl;
        }
    }

    internal static class ReorderableListExtensions
    {
        public static void DoLayoutListWithHeaderTooltip(this ReorderableList rl, string title, string tooltip)
        {
            Rect r = GUILayoutUtility.GetRect(1, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));
            EditorGUI.LabelField(r, new GUIContent(title, tooltip), EditorStyles.boldLabel);
            rl.DoLayoutList();
        }
    }
}
