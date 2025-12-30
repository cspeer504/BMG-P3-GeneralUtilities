// Unity 5.6 / C# 4.0
using System;
using System.ComponentModel;

using Packages.BMG.Misc;

namespace Packages.BMG.EventDebugger
{
public enum EventAction
{
    [Description("<")]
    PostedBy = 0,
    [Description(">")]
    ReceivedBy = 1
}
public enum EventDirection
{
    [Description("Mode")]
    ToModes = 0,
    [Description("GUI")]
    ToGui = 1
}

[Serializable]
public sealed class TrackedEvent
{
    // ReSharper disable InconsistentNaming - These aren't serialized fields for the Unity editor
    public string EventName;
    public EventAction Action;
    public string ClassName;
    public string MethodName;
    public string PayloadString;
    public EventDirection Direction;

    public string FullMethodName
    {
        get { return string.Format("{0}.{1}", ClassName, MethodName); }
    }
    public string Timestamp;
    private string _line;

    /// <inheritdoc />
    public override string ToString()
    {
        if(string.IsNullOrEmpty(_line))
        {
            if (Action == EventAction.PostedBy)
            {
                _line = string.Format("{0}  {1} [{2}] {3} {4}.{5}({6})", Timestamp, EventName, Direction.GetDescription(), Action.GetDescription(), ClassName, MethodName, PayloadString);
            }
            else
            {
                _line = string.Format("{0}  {1} {2} {3}.{4}", Timestamp, EventName, Action.GetDescription(), ClassName, MethodName);
            }
        }
        return _line;
    }
    // ReSharper restore InconsistentNaming
}
}
