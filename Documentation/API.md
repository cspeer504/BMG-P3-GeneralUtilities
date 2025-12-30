## Utility API

These are helper methods in this namespace. To see full descriptions of the methods (params, return, exceptions, additional remarks, ...) please see the XML doc on each method. IDE intelligence should also pick it up. This doc is merely a means to help you know the method exists.

### Packages.BMG.Utility.Log (Namespace)
---

#### Filter

  ```csharp
public static void Filter(List<string> allowList, List<string> blockList = null)
  ```

Use to filter unwanted logging (i.e. verbose) if desired. This only works for logging done through `Multimorphic.P3App.Logging.Logger.Log(string)`. Any Logging through `UnityEngine.Debug` will not be filtered.

#### GetAllModesUsed

```csharp
public static string GetAllModesUsed()
```

Returns a string of all modes that were active at some point in this Unity application execution. Each  mode name is on a new line. Can be used by developer to log all known modes at any given time.

#### ShowPrivateEventDetails

```csharp
public static void ShowPrivateEventDetails(P3Controller p3, bool show = true)
```

The following code is used to filter event logging of private token values, instead of generically referencing them as private. 
==NOTE:== This method is called by `Packages.BMG.Utility.Init`, but can be used separately/independently if you like. 


### Packages.BMG.Misc (Namespace)
---

#### RegisterPayloadParser

```csharp
public static void RegisterPayloadParser(string eventName, Func<object, string> parser)
```

Registers a custom payload stringifier for a specific event name. This can be used to fix/enhance the "parameter" output of events in the viewer and overlay tools.

#### UnregisterPayloadParser

```csharp
public static void UnregisterPayloadParser(string eventName)
```

Removes a previously registered payload parser.