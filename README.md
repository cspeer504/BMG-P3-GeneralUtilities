# Introduction
BMG P3 GeneralUtilities are a collection of Unity assets to aid in [P3 game development](https://www.multimorphic.com/p3-pinball-platform/3rd-party-developers/) in the [Unity game engine](https://unity.com/).

Features Include:
1. Event Viewer - Editor Window to track what posts events and what listens to them.
2. Mode Viewer - Editor Window to track what modes have been started and which are currently active.
3. Backbox Overlay - Backbox debug overlay showing these event and mode details on the cabinet!

For general troubleshooting / support, you can [Email Me]cspeer504@gmail.com). You can also find me on the [P3 Discord](https://www.multimorphic.com/community/) as ComboChuck.

If you have found a bug, you are also welcome to create an issue on the [github page]https://github.com/cspeer504/BMG-P3-GeneralUtilities), or a pull request if you have a fix / extension.

## Installation and Setup

### Prerequisites
- [Unity 5.6.7f1](https://unity.com/releases/editor/whats-new/5.6.7f1) - The [P3 SDK](https://www.multimorphic.com/support/projects/customer-support/wiki/3rd-Party_Development_Kit) is built on (and requires) Unity 5.6.7f1 and thus so are these utilities. I do not support anything outside of this version.
- [Harmony](https://github.com/pardeike/Harmony/releases/download/v2.2.2.0/Harmony.2.2.2.0.zip) - Extract the Harmony.2.2.2.0.zip somewhere. Then copy `Harmony.2.2.2.0\net35\0Harmony.dll` to your projects assets plugins folder: `\Assets\Plugins\0Harmony.dll`

You can install BMG P3 General Utilities using any of the following methods:

1. __Install Source__
    * TODO
  
2. __Initialize__  
   These utilities require an initialization call for it to set up in preperation to perform. Place this line inside the class that derives BaseGameMode:
  ```csharp
  public class MyBaseGameMode : BaseGameMode
  {
		public MyBaseGameMode (P3Controller controller)	: base(controller)
		{
		  Packages.BMG.Utility.Init(p3);
      // All other constructor code.
    }
  }
  ```

This is the minimum setup. Read the feature sections below to find out how to enable additional features.

# Features Detailed

## Event Viewer
This is an Editor Window that will keep track of posted events so you can see what classes post events and what classes receive those events. 

You can configure some features of the Event Viewer window through the BMG Settings.

## Mode Viewer
This is an Editor Window that will keep track of modes that become active so you can see which modes are currently active and which have been disabled.

You can configure some features of the Mode Viewer window through the BMG Settings.

## BMG Overlay
The `BMGOverlay.prefab` will show Event and Mode details on the back box so you can watch what's happening as you play your game.

You can configure some features of the Overlays through the BMG Settings.

## API
