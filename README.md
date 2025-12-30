# Introduction

BMG P3 GeneralUtilities are a collection of Unity assets to aid in [P3 game development](https://www.multimorphic.com/p3-pinball-platform/3rd-party-developers/) in the [Unity game engine](https://unity.com/).

<mark>NOTE:</mark> These are <u>debug</u> utilities. Reflection and other non-efficient tactics are used to provide features as plug-n-play as possible. Please take precautions to not inlude them in release candidate builds. 



Features Include:

1. Event Viewer - Editor Window to track what posts events and what listens to them.
2. Mode Viewer - Editor Window to track what modes have been started and which are currently active.
3. Backbox Overlay - Backbox debug overlay showing these event and mode details on the cabinet!



For general troubleshooting / support, you can [Email Me]cspeer504@gmail.com). You can also find me on the [P3 Discord](https://www.multimorphic.com/community/) as ComboChuck.

If you have found a bug, you are also welcome to create an issue on the [github page]https://github.com/cspeer504/BMG-P3-GeneralUtilities), or a pull request if you have a fix / extension.

## Installation and Setups

### Prerequisites

- [Unity 5.6.7f1](https://unity.com/releases/editor/whats-new/5.6.7f1) - The [P3 SDK](https://www.multimorphic.com/support/projects/customer-support/wiki/3rd-Party_Development_Kit) is built on (and requires) Unity 5.6.7f1 and thus so are these utilities. I do not support anything outside of this version.
- [Harmony](https://github.com/pardeike/Harmony/releases/download/v2.2.2.0/Harmony.2.2.2.0.zip) - Extract the Harmony.2.2.2.0.zip somewhere. Then copy `Harmony.2.2.2.0\net35\0Harmony.dll` to your projects assets plugins folder: `\Assets\Plugins\0Harmony.dll`

You can install BMG P3 General Utilities using any of the following methods:

1. __Import Asset Package__
   
   * Download the latest `.unitypackage` file here: [![GitHub tag (latest by date)](https://img.shields.io/github/v/tag/cspeer504/BMG-P3-GeneralUtilities)](https://github.com/cspeer504/BMG-P3-GeneralUtilities/releases/latest)
   * In Unity, Select Toolbar menu item `Assets->Import Package->Custom Package`.
   * Select **Import**.

2. __Initialize__  
   These utilities require an initialization call for it to set up in preperation to perform. Place this line inside the class that derives BaseGameMode:
   
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

This is the minimum setup. Read the feature sections below to find out how to enable additional features.

# Usage

**Note:** When you start Play Mode, an Assets/Resources/BMGSettings.asset file will be created if it does not already exist. This houses all of the settings used by these utilities. To learn more about the settings read .

There are a variety of [features](./documentation/Features.md) available for use.

**Editor Based:**
- [Event Viewer](./documentation/EventViewer.md)
- [Mode Viewer](./documentation/ModeViewer.md)
- [Mode Event Debug Overlay](./documentation/ModeEventDebugOverlay.md)
- [Mode Logging](./documentation/ModeLogging.md)


**Code Based:**

- [API](./documentation/API.md)
