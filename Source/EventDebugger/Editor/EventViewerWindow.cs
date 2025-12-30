// Unity 5.6 / C# 4.0
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Packages.BMG.EventDebugger.Editor
{
    public class EventViewerWindow : EditorWindow
    {
        private Vector2 _scroll;

        private const float _rowPadding = 2f;
        private float _rowHeight;
        private GUIStyle _rowStyle;

        private ulong _seenVersion = 0;
        private ulong _tailedVersion = 0;

        private bool _tailing = false;
        private string _stringToMatchForHighlight = string.Empty;
        private double _nextAllowedUpdateAt;
        private readonly List<TrackedEvent> _snapshot = new List<TrackedEvent>(256);

        // Const Strings to avoid string allocations (Garbage Collection relief).
        private const string _windowTitle = "Event Viewer";
        private const string _eventsPostedLabel = "Events Posted = ";
        private const string _tailingLabel = "Tail";
        private const string _clearRepositoryLabel = "Clear Repository";
        private const string _highlightLabel = "Highlight:";
        private const string _noEventsLabel = "No events posted yet.";

        [MenuItem("BMG/" + _windowTitle, false, 200)]
        public static void ShowWindow()
        {
            GetWindow<EventViewerWindow>(_windowTitle);
        }

        private void OnEnable()
        {
            _rowHeight = EditorGUIUtility.singleLineHeight + _rowPadding;

            // I can't init here because it's too early to grab EditorStyles.label (it can return null). So we lazy
            // init it in the OnGUI method.
            _rowStyle = null;

            TrackedEventRepository.Changed += OnRepositoryChanged;

            // Polling safety net: ensures repaint even when window is unfocused
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            TrackedEventRepository.Changed -= OnRepositoryChanged;
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnRepositoryChanged()
        {
            // Schedule on the next editor loop; safe even if repo updated off-thread
            EditorApplication.delayCall += Repaint;
        }

        private void OnEditorUpdate()
        {
            // Light weight “is stale?” check using Version
            ulong repoVersion = TrackedEventRepository.Version;
            if(repoVersion == _seenVersion) { return; }

            double now = EditorApplication.timeSinceStartup;
            if(! (now >= _nextAllowedUpdateAt)) { return; }

            _seenVersion = repoVersion;
            Repaint();

            // throttle to ~20 fps max under heavy write load
            _nextAllowedUpdateAt = now + 0.05;
        }

        private void ScrollToBottom()
        {
            TrackedEventRepository.FillSnapshot(_snapshot);

            int totalCount = _snapshot.Count;
            if (totalCount <= 0) { return; }

            float contentHeight = totalCount * _rowHeight;
            _scroll.y = Mathf.Max(0f, contentHeight);
        }

        private void OnGUI()
        {
            TrackedEventRepository.FillSnapshot(_snapshot);
            EditorGUILayout.LabelField(_eventsPostedLabel + TrackedEventRepository.NumberOfPostedEvents, EditorStyles.boldLabel);

            // Toolbar
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.FlexibleSpace();

            bool prevTailing = _tailing;
            _tailing = GUILayout.Toggle(_tailing, _tailingLabel, EditorStyles.toolbarButton, GUILayout.Width(60));
            if (_tailing && !prevTailing) { ScrollToBottom(); }

            if (GUILayout.Button(_clearRepositoryLabel, EditorStyles.toolbarButton, GUILayout.Width(110)))
            {
                TrackedEventRepository.Clear();
                TrackedEventRepository.FillSnapshot(_snapshot);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(_highlightLabel, GUILayout.Width(60));
            _stringToMatchForHighlight = EditorGUILayout.TextField(_stringToMatchForHighlight);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            if (_snapshot.Count == 0)
            {
                EditorGUILayout.HelpBox(_noEventsLabel, MessageType.Info);
                return;
            }

            Rect outerRect = GUILayoutUtility.GetRect(
                0, float.MaxValue,
                0, position.height - 88f,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true)
            );

            int totalCount = _snapshot.Count;
            float contentHeight = totalCount * _rowHeight;

            if (_tailing && _seenVersion != _tailedVersion)
            {
                // Clamp so we don't overshoot; this scrolls to the bottom line.
                float maxY = Mathf.Max(0f, contentHeight - outerRect.height + _rowHeight);
                _scroll.y = maxY;
                _tailedVersion = _seenVersion;
            }

            Rect contentRect = new Rect(0, 0, Mathf.Max(outerRect.width - 16f, 0f), contentHeight);

            // Using ScrollView to make list viewing efficient. There could be tens of thousands of events and this
            // will draw only what the events that the user is currently viewing.
            _scroll = GUI.BeginScrollView(outerRect, _scroll, contentRect);

            // Visible range
            float viewTop = _scroll.y;
            float viewBottom = _scroll.y + outerRect.height;
            int firstIndex = Mathf.Clamp((int)Mathf.Floor(viewTop / _rowHeight), 0, Mathf.Max(0, totalCount - 1));
            int lastIndex  = Mathf.Clamp((int)Mathf.Ceil (viewBottom / _rowHeight), 0, Mathf.Max(0, totalCount - 1));
            firstIndex = Mathf.Max(0, firstIndex - 2);
            lastIndex  = Mathf.Min(totalCount - 1, lastIndex + 2);

            for (int i = firstIndex; i <= lastIndex; i++)
            {
                TrackedEvent pe = _snapshot[i];
                if (pe == null) { continue; }

                Rect rowRect = new Rect(0, i * _rowHeight, contentRect.width, _rowHeight);

                // Row striping to help distinguish between event lines.
                if ((i % 2) == 0)
                {
                    EditorGUI.DrawRect(rowRect, Settings.Events.InEditorViewer.RowStripColor);
                }

                string line = pe.ToString();
                // Highlight if this line matches the Find string
                if (!string.IsNullOrEmpty(_stringToMatchForHighlight))
                {
                    if (!string.IsNullOrEmpty(line) &&
                        line.IndexOf(_stringToMatchForHighlight, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        EditorGUI.DrawRect(rowRect, Settings.Events.InEditorViewer.FindHighlightColor);
                    }
                }

                // Color the text based poster v.s. receiver.
                bool isPostedBy = (pe.Action == EventAction.PostedBy);
                Color c = isPostedBy ? Settings.Events.InEditorViewer.PostedByColor : Settings.Events.InEditorViewer.ReceivedByColor;

                if (_rowStyle == null) { _rowStyle = new GUIStyle(EditorStyles.label); }
                _rowStyle.normal.textColor  = c;
                _rowStyle.hover.textColor   = c;
                _rowStyle.active.textColor  = c;
                _rowStyle.focused.textColor = c;
                _rowStyle.font = EditorStyles.label.font;

                EditorGUI.LabelField(rowRect, line, _rowStyle);
            }

            GUI.EndScrollView();
            _seenVersion = TrackedEventRepository.Version;
        }
    }
}
