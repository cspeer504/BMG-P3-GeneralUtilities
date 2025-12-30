// Unity 5.6 / C# 4.0
using System;
using System.Collections.Generic;

using UnityEditor;

using UnityEngine;

namespace Packages.BMG.ModeDebugger.Editor
{
public class ModeViewerWindow : EditorWindow
{
    private const string _toolName = "Mode Viewer";
    private const string _highlightLabel = "Highlight:";
    private const string _formatLabel = "ModeClassName_priority";
    private const string _separatorLabel = "---------------------------";
    private const string _nullLabel = "<null>";
    private Vector2 _scrollPosition;

    private readonly List<TrackedMode> _snapshot = new List<TrackedMode>(256);
    private ulong _liveVersion = 0;
    private bool _isDirty = false;

    private string _highlightFilter = "";

    [MenuItem("BMG/"+_toolName, false, 200)]
    public static void ShowWindow()
    {
        GetWindow<ModeViewerWindow>(_toolName);
    }

    private void OnEnable()
    {
        SubscribeToSource();

        RefreshFromSource(true);

        EditorApplication.update += OnEditorUpdate;
    }

    private void OnDisable()
    {
        UnsubscribeFromSource();
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnEditorUpdate()
    {
        if (_isDirty)
        {
            Repaint();
            _isDirty = false;
        }
    }

    private void SubscribeToSource()
    {
        TrackedModeRepository.Changed += OnSourceChanged;
    }

    private void UnsubscribeFromSource()
    {
        TrackedModeRepository.Changed -= OnSourceChanged;
    }

    private void OnSourceChanged()
    {
        RefreshFromSource(false);
    }

    private void RefreshFromSource(bool force)
    {
        if (force || TrackedModeRepository.Version != _liveVersion)
        {
            TrackedModeRepository.FillSnapshot(_snapshot);
            _liveVersion = TrackedModeRepository.Version;
            _isDirty = true;
        }
    }

    private void OnGUI()
    {
        // search field for highlight filter
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(_highlightLabel, GUILayout.Width(60));
        _highlightFilter = EditorGUILayout.TextField(_highlightFilter);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        // Header
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(_formatLabel, EditorStyles.miniBoldLabel);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(_separatorLabel, EditorStyles.miniBoldLabel);
        EditorGUILayout.EndHorizontal();

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        double now = EditorApplication.timeSinceStartup;

        for (int i = 0; i < _snapshot.Count; i++)
        {
            TrackedMode vm = _snapshot[i];
            if (vm == null)
            {
                EditorGUILayout.LabelField(_nullLabel, EditorStyles.label);
                continue;
            }

            // ---------- Hightlight the text if it matches the filter
            Rect rowRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);

            bool isHighlighted = !string.IsNullOrEmpty(_highlightFilter) &&
                                 !string.IsNullOrEmpty(vm.ClassName) &&
                                 vm.ClassName.IndexOf(_highlightFilter, StringComparison.OrdinalIgnoreCase) >= 0;

            if (isHighlighted)
            {
                EditorGUI.DrawRect(rowRect, Settings.Modes.InEditorViewer.HighlightColor);
            }

            // ---------- Color the font based on mode's active state
            Color rowColor = GetModeColor(vm, now);

            GUIStyle rowStyle = new GUIStyle(EditorStyles.label);
            rowStyle.normal.textColor = rowColor;

            // Finally draw the label text on top
            EditorGUI.LabelField(rowRect, vm.ToString(), rowStyle);
        }

        EditorGUILayout.EndScrollView();
    }

    private Color GetModeColor(TrackedMode vm, double now)
    {
        if (!vm.IsOn) { return Settings.Modes.InEditorViewer.DisabledColor; }
        return Settings.Modes.InEditorViewer.EnabledColor;
    }

    private static int CompareByPriorityDescThenId(TrackedMode a, TrackedMode b)
    {
        if (a == null && b == null) {return 0;}
        if (a == null) {return 1;}
        if (b == null) {return -1;}

        if (a.Priority != b.Priority)
        {
            return b.Priority.CompareTo(a.Priority); // higher priority first
        }

        string ida = a.ClassName == null ? string.Empty : a.ClassName;
        string idb = b.ClassName == null ? string.Empty : b.ClassName;
        return string.Compare(ida, idb, StringComparison.Ordinal);
    }
}
}