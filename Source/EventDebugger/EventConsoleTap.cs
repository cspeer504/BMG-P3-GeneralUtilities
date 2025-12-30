// Unity 5.6 / C# 4.0
using System;
using UnityEngine;

namespace Packages.BMG.EventDebugger
{
/// <summary>
/// Listens to Application.logMessageReceived and captures <see cref="TrackedEvent"/> items when lines match.
/// </summary>
public static class EventConsoleTap
{
    private static readonly string s_eventTag = "Event:";
    private static readonly string s_receivedByTag = "received by:";

    public static void Enable()
    {
        Application.logMessageReceived += HandleLog;
    }

    public static void Disable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private static void HandleLog(string condition, string stackTrace, LogType type)
    {
        // Only parse lines that use expected strings for a received event.
        if (condition == null) { return; }
        if (condition.IndexOf(s_eventTag, StringComparison.Ordinal) < 0) { return; }
        if (condition.IndexOf(s_receivedByTag, StringComparison.Ordinal) < 0) { return; }

        TrackedEvent pe;
        if (EventLineParser.TryParse(condition, out pe))
        {
            TrackedEventRepository.Add(pe);
        }
    }
}
}
