// Unity 5.6 / C# 4.0
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Packages.BMG
{
    /// <summary>
    /// ScriptableObject that stores all persisted BMG settings.
    /// Serialized to a single .asset file in your project.
    /// </summary>
    [CreateAssetMenu(fileName = "BMGSettings", menuName = "BMG/New BMG Settings", order = 51)]
    public class BMGSettingsAsset : ScriptableObject
    {
        [System.Serializable]
        public class ModesLoggingDto
        {
            [FormerlySerializedAs("Enabled")] public bool m_enabled = false;
            [FormerlySerializedAs("IndentString")] public string m_indentString = "  ";
            [FormerlySerializedAs("StartedByAnotherModeIndicatorString")] public string m_startedByAnotherModeIndicatorString = "└> ";
            [FormerlySerializedAs("ModeLogPrefix")] public string m_modeLogPrefix = "<b><color=Teal>[Mode] </color></b>";
        }
        [FormerlySerializedAs("Modes_Logging")] public ModesLoggingDto m_modesLogging = new ModesLoggingDto();

        [System.Serializable]
        public class ModesInEditorViewerDto
        {
            [FormerlySerializedAs("EnabledColor")] public Color m_enabledColor = new Color(28/255f, 28/255f, 200/255f, 1f);
            [FormerlySerializedAs("DisabledColor")] public Color m_disabledColor = Color.grey;
            [FormerlySerializedAs("HighlightColor")] public Color m_highlightColor = new Color(255/255f, 255/255f, 0/255f, 35/255f);
        }
        [FormerlySerializedAs("Modes_InEditorViewer")] public ModesInEditorViewerDto m_modesInEditorViewer = new ModesInEditorViewerDto();

        [System.Serializable]
        public class ModesInGameOverlayDecorationDto
        {
            [FormerlySerializedAs("JustActiveColor")] public Color m_justActiveColor = new Color(0/255f, 255/255f, 0/255f, 255/255f);
            [FormerlySerializedAs("ActiveColor")] public Color m_activeColor = new Color(225/255f, 255/255f, 255/255f, 255/255f);
            [FormerlySerializedAs("InactiveColor")] public Color m_inactiveColor = new Color(97/255f, 97/255f, 97/255f, 255/255f);
            [FormerlySerializedAs("BoldTextIfContains")] public string[] m_boldTextIfContains = new string[0];
        }
        [FormerlySerializedAs("Modes_InGameOverlay_Decoration")] public ModesInGameOverlayDecorationDto m_modesInGameOverlayDecoration = new ModesInGameOverlayDecorationDto();

        [System.Serializable]
        public class ModesInGameOverlayFiltersDto
        {
            [FormerlySerializedAs("ConsolidateConsecutiveModes")] public bool m_consolidateConsecutiveModes = true;
            [FormerlySerializedAs("IgnoredModesPrefixedWith")] public string[] m_ignoredModesPrefixedWith = new string[0];
        }
        [FormerlySerializedAs("Modes_InGameOverlay_Filters")] public ModesInGameOverlayFiltersDto m_modesInGameOverlayFilters = new ModesInGameOverlayFiltersDto();

        [System.Serializable]
        public class EventsRepositoryDto
        {
            [FormerlySerializedAs("MaxCapacity")] public int m_maxCapacity = 4096;
            [FormerlySerializedAs("StackFramesToCheckPosterMethod")] public int m_stackFramesToCheckPosterMethod = 3;
            [FormerlySerializedAs("VerboseEvents")]
            public List<string> m_verboseEvents = new List<string>
            {
                "AccelerometerEvent", "Grid Event", "Evt_GUIBurstEvent", "Evt_GameBurstEvent",
                "Evt_ShowLocationText", "Evt_VerticalLevelValue", "Evt_HorizontalLevelValue",
                "Evt_RunGUIInsertCommand", "Evt_SetLED", "Evt_AddGUIInsertScript",
                "Evt_RemoveGUIInsertScript", "Evt_AddLEDToSimulator"
            };
        }
        [FormerlySerializedAs("Events_Repository")] public EventsRepositoryDto m_eventsRepository = new EventsRepositoryDto();

        [System.Serializable]
        public class EventsInEditorViewerDto
        {
            [FormerlySerializedAs("PostedByColor")] public Color m_postedByColor = new Color(28/255f, 28/255f, 200/255f, 1f);
            [FormerlySerializedAs("ReceivedByColor")] public Color m_receivedByColor = new Color(28/255f, 150/255f, 28/255f, 1f);
            [FormerlySerializedAs("FindHighlightColor")] public Color m_findHighlightColor = new Color(1f, 1f, 0f, 0.14f);
            [FormerlySerializedAs("RowStripColor")] public Color m_rowStripColor = new Color(0f, 0f, 0f, 0.03f);
            [FormerlySerializedAs("DateTimeFormat")] public string m_dateTimeFormat = "HH:mm:ss.fff";
        }
        [FormerlySerializedAs("Events_InEditorViewer")] public EventsInEditorViewerDto m_eventsInEditorViewer = new EventsInEditorViewerDto();

        [System.Serializable]
        public class EventsInGameOverlayDecorationDto
        {
            [FormerlySerializedAs("PostedToModesColor")] public Color m_postedToModesColor = new Color(0/255f, 130/255f, 255/255f, 255/255f);
            [FormerlySerializedAs("PostedToGuiColor")] public Color m_postedToGuiColor = new Color(0/255f, 173/255f, 255/255f, 255/255f);
            [FormerlySerializedAs("HandlingColor")] public Color m_handlingColor = new Color(0/255f, 255/255f, 128/255f, 255/255f);
            [FormerlySerializedAs("BoldTextIfContains")] public string[] m_boldTextIfContains = new string[0];
        }
        [FormerlySerializedAs("Events_InGameOverlay_Decoration")] public EventsInGameOverlayDecorationDto m_eventsInGameOverlayDecoration = new EventsInGameOverlayDecorationDto();

        [System.Serializable]
        public class EventsInGameOverlayFiltersDto
        {
            [FormerlySerializedAs("ConsolidateConsecutiveEvents")] public bool m_consolidateConsecutiveEvents = true;
            [FormerlySerializedAs("IgnoredEvents")] public string[] m_ignoredEvents = new string[0];
            [FormerlySerializedAs("OnlyShowPosterForTheseEvents")] public string[] m_onlyShowPosterForTheseEvents = new string[0];
        }
        [FormerlySerializedAs("Events_InGameOverlay_Filters")] public EventsInGameOverlayFiltersDto m_eventsInGameOverlayFilters = new EventsInGameOverlayFiltersDto();
    }
}
