// Unity 5.6 / C# 4.0
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
// ReSharper disable MultipleStatementsOnOneLine
#endif

namespace Packages.BMG
{
    /// <summary>
    /// Global settings access point for BMG tooling.
    /// Proxies to a ScriptableObject asset in the project.
    /// </summary>
    public static class Settings
    {
        private const string _assetName = "BMGSettings";
        private const string _assetNameWithExtension = _assetName + ".asset";
        private const string _resourcesPath = "Assets/Resources";
        private const string _defaultAssetPath =  _resourcesPath + "/" + _assetNameWithExtension;
        private static BMGSettingsAsset s_asset;
        private static bool s_initialized;

        private static BMGSettingsAsset BmgSettingsAsset
        {
            get
            {
                if (s_asset != null) { return s_asset; }

                // 1) Try Resources (works in play mode)
                s_asset = Resources.Load<BMGSettingsAsset>(_assetName);

#if UNITY_EDITOR
                // 2) Try AssetDatabase (edit mode)
                if (s_asset == null)
                {
                    string[] guids = AssetDatabase.FindAssets("t:Packages.BMG.BMGSettingsAsset");
                    if (guids != null && guids.Length > 0)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        s_asset = AssetDatabase.LoadAssetAtPath<BMGSettingsAsset>(path);
                    }
                }
                // 3) Create default if still missing
                if (s_asset == null)
                {
                    Debug.Log("Can not find " + _assetName + " in the resources folder. Creating one at: " + _defaultAssetPath);
                    s_asset = ScriptableObject.CreateInstance<BMGSettingsAsset>();
                    System.IO.Directory.CreateDirectory(_resourcesPath);
                    AssetDatabase.CreateAsset(s_asset, _defaultAssetPath);
                    AssetDatabase.SaveAssets();
                }
#endif
                return s_asset;
            }
        }

        /// <summary>
        /// Settings that involve logging of when modes are active (Added) and non-active (Removed).
        /// </summary>
        public static class Modes
        {
            /// <summary>
            /// Settings that involve logging of when modes are active (Added) and non-active (Removed).
            /// </summary>
            public static class Logging
            {
                /// <summary>
                /// Enables Mode Logging. Requires <see cref="HarmonyPatches.Init()"/> to be called.
                /// </summary>
                public static bool Enabled { get { return BmgSettingsAsset.m_modesLogging.m_enabled; } set { BmgSettingsAsset.m_modesLogging.m_enabled = value; MarkDirty(); } }

                /// <summary>
                /// The string used for indenting sub-modes (when a mode adds another mode).
                /// Requires <see cref="Enabled"/> to be true.
                /// </summary>
                public static string IndentString { get { return BmgSettingsAsset.m_modesLogging.m_indentString; } set { BmgSettingsAsset.m_modesLogging.m_indentString = value; MarkDirty(); } }

                /// <summary>
                /// A string used to visually indicate that a mode was started by the previous mode.
                /// Requires <see cref="Enabled"/> to be true.
                /// </summary>
                public static string StartedByAnotherModeIndicatorString { get { return BmgSettingsAsset.m_modesLogging.m_startedByAnotherModeIndicatorString; } set { BmgSettingsAsset.m_modesLogging.m_startedByAnotherModeIndicatorString = value; MarkDirty(); } }

                /// <summary>
                /// A prefix in the log string.
                /// Requires <see cref="Enabled"/> to be true.
                /// </summary>
                public static string ModeLogPrefix { get { return BmgSettingsAsset.m_modesLogging.m_modeLogPrefix; } set { BmgSettingsAsset.m_modesLogging.m_modeLogPrefix = value; MarkDirty(); } }
            }

            /// <summary>
            /// Settings for the ModeViewer editor tool (BMG->Tools->Mode Viewer).
            /// </summary>
            public static class InEditorViewer
            {
                /// <summary>
                /// The color of the mode text font when it is enabled.
                /// </summary>
                public static Color EnabledColor { get { return BmgSettingsAsset.m_modesInEditorViewer.m_enabledColor; } set { BmgSettingsAsset.m_modesInEditorViewer.m_enabledColor = value; MarkDirty(); } }

                /// <summary>
                /// The color of the mode text font when it is disabled.
                /// </summary>
                public static Color DisabledColor { get { return BmgSettingsAsset.m_modesInEditorViewer.m_disabledColor; } set { BmgSettingsAsset.m_modesInEditorViewer.m_disabledColor = value; MarkDirty(); } }

                /// <summary>
                /// Maximum number of events retained.
                /// </summary>
                public static Color HighlightColor { get { return BmgSettingsAsset.m_modesInEditorViewer.m_highlightColor; } set { BmgSettingsAsset.m_modesInEditorViewer.m_highlightColor = value; MarkDirty(); } }
            }

            /// <summary>
            /// Settings for the in-game overlay of active modes.
            /// </summary>
            public static class InGameOverlay
            {
                public static class Decoration
                {
                    /// <summary>
                    /// Highlight modes that are not defined in <see cref="Literals.P3SdkModeNames"/>. Used to see if
                    /// new values should be defined. I don't want to expose this setting because it's only useful
                    /// to BMG library maintenance.
                    /// </summary>
                    public static bool HighlightUnknown = false;

                    /// <summary>
                    /// The color applied immediately after a mode turns on; lerps to Active.
                    /// </summary>
                    public static Color JustActiveColor { get { return BmgSettingsAsset.m_modesInGameOverlayDecoration.m_justActiveColor; } set { BmgSettingsAsset.m_modesInGameOverlayDecoration.m_justActiveColor = value; MarkDirty(); } }

                    /// <summary>
                    /// The color used when a mode is enabled and has aged.
                    /// </summary>
                    public static Color ActiveColor { get { return BmgSettingsAsset.m_modesInGameOverlayDecoration.m_activeColor; } set { BmgSettingsAsset.m_modesInGameOverlayDecoration.m_activeColor = value; MarkDirty(); } }

                    /// <summary>
                    /// The color used when a mode is disabled.
                    /// </summary>
                    public static Color InactiveColor { get { return BmgSettingsAsset.m_modesInGameOverlayDecoration.m_inactiveColor; } set { BmgSettingsAsset.m_modesInGameOverlayDecoration.m_inactiveColor = value; MarkDirty(); } }

                    /// <summary>
                    /// Substrings that, if contained in the mode name, cause the overlay to render it bold.
                    /// </summary>
                    public static string[] BoldTextIfContains { get { return BmgSettingsAsset.m_modesInGameOverlayDecoration.m_boldTextIfContains; } set { BmgSettingsAsset.m_modesInGameOverlayDecoration.m_boldTextIfContains = (value ?? new string[0]); MarkDirty(); } }
                }

                public static class Filters
                {
                    /// <summary>
                    /// When enabled, duplicate modes (same ClassName, IsOn, Priority) are shown as a single line with (xN).
                    /// </summary>
                    public static bool ConsolidateConsecutiveModes { get { return BmgSettingsAsset.m_modesInGameOverlayFilters.m_consolidateConsecutiveModes; } set { BmgSettingsAsset.m_modesInGameOverlayFilters.m_consolidateConsecutiveModes = value; MarkDirty(); } }

                    /// <summary>
                    /// Modes whose ClassName starts with any of these prefixes will not appear in the in-game overlay.
                    /// </summary>
                    public static string[] IgnoredModesPrefixedWith { get { return BmgSettingsAsset.m_modesInGameOverlayFilters.m_ignoredModesPrefixedWith; } set { BmgSettingsAsset.m_modesInGameOverlayFilters.m_ignoredModesPrefixedWith = (value ?? new string[0]); MarkDirty(); } }
                }
            }
        }

        /// <summary>
        /// Settings that involve the storage of Event History.
        /// </summary>
        public static class Events
        {
            /// <summary>
            /// Settings that involve the storage of Event History.
            /// </summary>
            public static class Repository
            {
                /// <summary>
                /// Maximum number of events retained.
                /// </summary>
                public static int MaxCapacity { get { return BmgSettingsAsset.m_eventsRepository.m_maxCapacity; } set { BmgSettingsAsset.m_eventsRepository.m_maxCapacity = Mathf.Max(0, value); MarkDirty(); } }

                /// <summary>
                /// The tooling uses the stack trace to determine the method that posted an event. This setting determines how many
                /// lines back in the stack trace to check.
                /// </summary>
                public static int StackFramesToCheckPosterMethod { get { return BmgSettingsAsset.m_eventsRepository.m_stackFramesToCheckPosterMethod; } set { BmgSettingsAsset.m_eventsRepository.m_stackFramesToCheckPosterMethod = value; MarkDirty(); } }

                /// <summary>
                /// A list of event names that are considered "spammy" and should be logged at the verbose level.
                /// These will also not be shown in the Event Viewer tool window.
                /// </summary>
                public static ReadOnlyCollection<string> VerboseEvents { get { return new ReadOnlyCollection<string>(BmgSettingsAsset.m_eventsRepository.m_verboseEvents); } set { BmgSettingsAsset.m_eventsRepository.m_verboseEvents = (value != null) ? new List<string>(value) : new List<string>(); MarkDirty(); } }
            }

            /// <summary>
            /// Settings for the EventViewer editor tool (BMG->Tools->Event Viewer).
            /// </summary>
            public static class InEditorViewer
            {
                /// <summary> The color of the mode text font when it is one relating to the posting of an event. </summary>
                public static Color PostedByColor { get { return BmgSettingsAsset.m_eventsInEditorViewer.m_postedByColor; } set { BmgSettingsAsset.m_eventsInEditorViewer.m_postedByColor = value; MarkDirty(); } }

                /// <summary> The color of the mode text font when it is one relating to the receiving/handling of an event. </summary>
                public static Color ReceivedByColor { get { return BmgSettingsAsset.m_eventsInEditorViewer.m_receivedByColor; } set { BmgSettingsAsset.m_eventsInEditorViewer.m_receivedByColor = value; MarkDirty(); } }

                /// <summary> The color of the mode text background when it matches the highlight string. </summary>
                public static Color FindHighlightColor { get { return BmgSettingsAsset.m_eventsInEditorViewer.m_findHighlightColor; } set { BmgSettingsAsset.m_eventsInEditorViewer.m_findHighlightColor = value; MarkDirty(); } }

                /// <summary>
                /// The color of the row strip background. This can make it easier to see individual rows.
                /// Set color to pure alpha to disable.
                /// </summary>
                public static Color RowStripColor { get { return BmgSettingsAsset.m_eventsInEditorViewer.m_rowStripColor; } set { BmgSettingsAsset.m_eventsInEditorViewer.m_rowStripColor = value; MarkDirty(); } }

                /// <summary>
                /// The DateTime format string to use when displaying the time of an event occurrence.
                /// https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-date-and-time-format-strings
                /// </summary>
                public static string DateTimeFormat { get { return BmgSettingsAsset.m_eventsInEditorViewer.m_dateTimeFormat; } set { BmgSettingsAsset.m_eventsInEditorViewer.m_dateTimeFormat = value; MarkDirty(); } }
            }

            /// <summary>
            /// Settings for the EventViewer in-game overlay.
            /// </summary>
            public static class InGameOvervlay
            {
                public static class Decoration
                {
                    /// <summary> The color of the mode text font when it is one relating to the posting of an event to modes. </summary>
                    public static Color PostedToModesColor { get { return BmgSettingsAsset.m_eventsInGameOverlayDecoration.m_postedToModesColor; } set { BmgSettingsAsset.m_eventsInGameOverlayDecoration.m_postedToModesColor = value; MarkDirty(); } }

                    /// <summary> The color of the mode text font when it is one relating to the posting of an event to GUIs. </summary>
                    public static Color PostedToGuiColor { get { return BmgSettingsAsset.m_eventsInGameOverlayDecoration.m_postedToGuiColor; } set { BmgSettingsAsset.m_eventsInGameOverlayDecoration.m_postedToGuiColor = value; MarkDirty(); } }

                    /// <summary> The color of the mode text font when it is one relating to handling an event. </summary>
                    public static Color HandlingColor { get { return BmgSettingsAsset.m_eventsInGameOverlayDecoration.m_handlingColor; } set { BmgSettingsAsset.m_eventsInGameOverlayDecoration.m_handlingColor = value; MarkDirty(); } }

                    /// <summary>
                    /// Substrings that, if contained in the event name, cause the overlay to render it bold.
                    /// </summary>
                    public static string[] BoldTextIfContains { get { return BmgSettingsAsset.m_eventsInGameOverlayDecoration.m_boldTextIfContains; } set { BmgSettingsAsset.m_eventsInGameOverlayDecoration.m_boldTextIfContains = (value ?? new string[0]); MarkDirty(); } }
                }

                public static class Filters
                {
                    /// <summary>
                    /// When enabled, multiple consecutive events with the same name are collapsed into a single "(xN)" line.
                    /// </summary>
                    public static bool ConsolidateConsecutiveEvents { get { return BmgSettingsAsset.m_eventsInGameOverlayFilters.m_consolidateConsecutiveEvents; } set { BmgSettingsAsset.m_eventsInGameOverlayFilters.m_consolidateConsecutiveEvents = value; MarkDirty(); } }

                    /// <summary>
                    /// Events with an exact name match here will not be shown in the in-game overlay.
                    /// </summary>
                    public static string[] IgnoredEvents { get { return BmgSettingsAsset.m_eventsInGameOverlayFilters.m_ignoredEvents; } set { BmgSettingsAsset.m_eventsInGameOverlayFilters.m_ignoredEvents = (value ?? new string[0]); MarkDirty(); } }

                    /// <summary>
                    /// For events listed here, only the posting entries are shown (receives/handlers hidden) to save space.
                    /// </summary>
                    public static string[] OnlyShowPosterForTheseEvents { get { return BmgSettingsAsset.m_eventsInGameOverlayFilters.m_onlyShowPosterForTheseEvents; } set { BmgSettingsAsset.m_eventsInGameOverlayFilters.m_onlyShowPosterForTheseEvents = (value ?? new string[0]); MarkDirty(); } }
                }
            }
        }

        private static void MarkDirty()
        {
#if UNITY_EDITOR
            if (s_asset != null)
            {
                EditorUtility.SetDirty(s_asset);
                // Optionally: AssetDatabase.SaveAssets();
            }
#endif
        }

        public static void Save()
        {
#if UNITY_EDITOR
            if (s_asset == null) { return; }
            EditorUtility.SetDirty(s_asset);
            AssetDatabase.SaveAssets();
#endif
        }
    }
}
