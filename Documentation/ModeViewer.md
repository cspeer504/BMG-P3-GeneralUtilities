# Mode Viewer (Editor Window)

This is a dockable Editor Window that will keep track of posted events so you can see what modes have started and which are still active.

## Usage

---

**IMPORTANT:** You must ensure that you pass the p3 object to the init method in the constructor of your BaseGameMode override class:

```csharp
public class MyBaseGameMode : BaseGameMode
{
     public MyBaseGameMode (P3Controller controller)    : base(controller)
     {
       // Take precautions to not include in release candidate builds.
       Packages.BMG.Utility.Init(p3);
       // All other constructor code.
     }
}
```

To open, use this toolbar menu: `BMG->Mode Viewer`

## Basics

---

As modes are started/added (in Play Mode), you will see them added to the list of modes in this window. Modes are shown with this format:

`<Name>_<Priority>`

- `<Name>` = Name of the Mode. This is the name of the C# class that represents this mode.
- `<Priority>` = This is the Priority of the mode. See P3 SDK docs for more info.

Modes that have been started/added are shown in blue (configurable) and modes that have since been stopped/removed are shown in grey (configurable). Modes that exist but have not be started/added are not displayed in this window until they are.

## GUI

---

 ![](.\Media\ModeViewer-GeneralExample.png)

**Highlight (Text Box)**
    Entering text in here will highlight lines yellow (configurable) that contain this string. This does NOT use RegEx, it simply uses [string.IndexOf](https://learn.microsoft.com/en-us/dotnet/api/System.String.IndexOf?view=netframework-2.0).

## Configuration

---

You can configure some features of the Event Viewer window through the BMG Settings. Toolbar: `BMG->Settings`

- Settings in `Modes/Modes: In-Editor Viewer` will affects the viewer window.

---

<mark>Note:</mark> Configuration fields are not documented in this git page. It would be redundant and a bit useless here because there are tooltips when hovering over each field to provide documentation inside the tool itself.
