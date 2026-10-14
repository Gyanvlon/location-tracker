# Location Tracker

Location Tracker is a C# and .NET MAUI Android app that saves the user's location
in an SQLite database and displays the saved history as a heat map on a native
.NET MAUI Maps control. It helps visualize where the user spends time: areas with
more recorded observations become warmer on the map.

Repository: [LocationTracker on GitHub](https://github.com/Gyanvlon/location-tracker)

## What the app does

- Requests location permission before starting GPS tracking.
- Records the latest known position every **10 seconds** while tracking is active
  and the app is in the foreground, including when the device is stationary.
- Saves latitude, longitude, UTC recording time and a demo flag in a private SQLite
  database named `locations.db3`.
- Reloads saved history when the app is reopened.
- Displays relative location density using **blue, cyan, yellow and red** circles.
- Shows both the recording time and the GPS fix time, so a reused fix is identifiable.
- Provides a **Demo** route with 405 synthetic observations for testing the heat map.
- Stops recording when **Stop tracking** is tapped or the app moves to the background.
- Asks for confirmation before **Clear** deletes all saved history.

Android determines when fresh GPS fixes arrive. The recording timer saves the
latest available fix every 10 seconds; it does not guarantee a new GPS measurement
every 10 seconds. No location is saved until a fix is available. Demo samples are
identified separately and do not increase automatically while tracking.

## Requirements

This repository currently targets **Android only** (`net9.0-android35.0`). The other
platform folders contain template entry points and are not configured or validated
application targets.

| Component | Required setup for this project |
| --- | --- |
| .NET SDK | .NET 9 SDK; `global.json` selects an installed 9.0 feature band |
| MAUI workload | `maui-android`, or the corresponding MAUI tools installed through Visual Studio |
| Java | JDK 17 for this project's Android toolchain |
| Android SDK | Android SDK Platform 35, Build Tools, Platform Tools and Emulator |
| Test device | Android emulator with Google APIs/Google Play, or a connected Android device |
| Maps | Google Maps SDK for Android API key authorized for the app's package and signing certificate |
| Network | Internet access for dependency restoration and Google Maps tiles |

The project declares Android API 23 as its minimum supported OS version. An API 35
emulator with Google Play is a convenient development target.

On Windows, use Visual Studio with the **.NET Multi-platform App UI development**
workload. On macOS, use VS Code with Microsoft's **.NET MAUI** extension and Android
Studio's SDK and Device Managers. The command-line workflow below also works with
the required SDKs installed.

## Set up the project

### 1. Get the source code

Clone the repository and open its directory:

```sh
git clone
cd location-tracker
```

If you downloaded the source ZIP instead, extract it and open the folder containing
`LocationTrackerApp.csproj` and `LocationTrackerApp.sln`.

### 2. Install and verify .NET and MAUI

Install the [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0).
From the project directory, check the selected SDK and install the Android workload:

```sh
dotnet --version
dotnet workload install maui-android
dotnet workload list
```

`dotnet --version` should report `9.0.x`. A newer major SDK by itself does not satisfy
this project's `global.json`. Install the workload for the selected .NET 9 SDK.
If installing into a system-owned SDK directory requires elevation, run the workload
installation from an administrator terminal on Windows or with `sudo` on macOS.

### 3. Configure Java and the Android SDK

Install JDK 17. In Android Studio, open **SDK Manager** and install **Android 15 / API
35**, Android SDK Build Tools, Android SDK Platform Tools and Android Emulator.

Set `JAVA_HOME` to the JDK 17 directory and `ANDROID_HOME` to the Android SDK directory.
Examples for the current terminal session follow; replace the Java path with your
actual installation location.

macOS terminal:

```sh
export JAVA_HOME="/path/to/jdk-17/Contents/Home"
export ANDROID_HOME="$HOME/Library/Android/sdk"
```

Windows PowerShell, when using the usual Android Studio SDK location:

```powershell
$env:JAVA_HOME = "C:\path\to\jdk-17"
$env:ANDROID_HOME = "$env:LOCALAPPDATA\Android\Sdk"
```

The SDK location can differ when installed through Visual Studio. Use the location
shown by your SDK Manager. Add its `platform-tools` and `emulator` folders to `PATH`
if you want to run `adb` and `emulator` by name.

In VS Code, **.NET MAUI: Configure Android** in the Command Palette can also select
the Java and Android SDK directories. Restart the IDE after changing persistent
environment variables.

### 4. Configure Google Maps

In your Google Cloud project, enable **Maps SDK for Android**, configure billing as
required by Google, and create an API key. Authorize the Android application using:

- Package name: `com.companyname.locationtrackerapp`
- SHA-1 fingerprint of the signing certificate actually used by your build

Open `Platforms/Android/AndroidManifest.xml` and set the following metadata entry
inside `<application>` to your configured key:

```xml
<meta-data
    android:name="com.google.android.geo.API_KEY"
    android:value="YOUR_GOOGLE_MAPS_API_KEY" />
```

Replace the existing value rather than adding a duplicate entry. Rebuild the app
after changing the manifest. A new machine or signing keystore may require a new
certificate fingerprint in the key's Android application restrictions.

### 5. Prepare an emulator or phone

For an emulator, open Android Studio's **Device Manager**, create a virtual device
using a Google APIs or Google Play image, and start it. On Apple Silicon Macs,
choose an ARM64 image.

For a physical phone, enable **Developer options** and **USB debugging**, connect
it by USB, and accept the computer's debugging authorization prompt. Enable Android
location services and an internet connection on either device.

Check that ADB sees the device:

```sh
adb devices
```

The target should appear with the state `device`, rather than `offline` or
`unauthorized`. For the simplest command-line run, keep one target connected.

## Run the app

### From a terminal

Run these commands from the folder containing `LocationTrackerApp.csproj`:

```sh
dotnet restore LocationTrackerApp.csproj
dotnet build LocationTrackerApp.csproj -f net9.0-android35.0
dotnet build LocationTrackerApp.csproj -t:Run -f net9.0-android35.0
```

The final command builds, installs and launches the app on the connected Android
target. The first build can take several minutes while dependencies are restored.

If automatic SDK discovery selects the wrong Java or Android installation, provide
both paths explicitly. For example, on macOS:

```sh
dotnet build LocationTrackerApp.csproj -t:Run -f net9.0-android35.0 -p:AndroidSdkDirectory="$ANDROID_HOME" -p:JavaSdkDirectory="$JAVA_HOME"
```

In PowerShell, use `$env:ANDROID_HOME` and `$env:JAVA_HOME` for those path values.

### From an IDE

**Visual Studio on Windows:** open `LocationTrackerApp.sln`, set the app as the
startup project, select an Android emulator or connected phone, and press **F5**.

**VS Code on macOS:** open the project folder, use the .NET MAUI extension's device
selector to choose an Android target, then start debugging with **F5**.

### Build a standalone APK

To generate a signed debug APK with its assemblies included, use:

```sh
dotnet build LocationTrackerApp.csproj -t:SignAndroidPackage -f net9.0-android35.0 -p:RuntimeIdentifier=android-arm64 -p:OutputPath=bin/apk/
```

This example targets ARM64 devices and Apple Silicon Android emulators. Use
`android-x64` instead for an x64 emulator. Install and launch the APK with:

```sh
adb install -r bin/apk/com.companyname.locationtrackerapp-Signed.apk
adb shell monkey -p com.companyname.locationtrackerapp -c android.intent.category.LAUNCHER 1
```

`adb install -r` preserves existing app data when updating an APK signed with the
same certificate. This command creates a development build, not a store release.

## Use tracking and demo mode

1. Open the app and tap **Start tracking**.
2. Grant location access when Android asks. Allow precise location for the most
   accurate results.
3. Keep the screen open. After the first GPS fix, the app saves an initial sample
   and then records the latest known position every 10 seconds.
4. Watch the **saved locations** count increase. The status shows the last save
   time and the time of the GPS fix used.
5. Tap **Stop tracking** to stop recording. The saved locations remain available.

To demonstrate the map without moving, tap **Demo**. It loads a synthetic Cupertino
route with 405 samples, including repeated observations that create yellow and red
areas. Tapping Demo again replaces earlier demo samples while preserving GPS history.
Demo loading does not require location permission, but map tiles need internet and
an authorized Maps key.

A label such as **419 saved locations (405 demo)** means 405 synthetic observations
and 14 GPS observations. Demo points are not evidence of physical travel. To test
movement in an emulator, use its **Extended controls → Location** panel to send
simulated coordinates while tracking is active.

**Clear** opens a confirmation dialog. Confirming deletes both GPS and demo history.
If tracking remains active, the next scheduled observation can add a new row. Stop
tracking first if you want the database to remain empty.

## How heat-map colors work

`HeatMapRenderer` extends MAUI Maps through its `MapElements` collection. It groups
positions into approximately 25-metre cells, counts observations, and normalizes
those counts against the busiest cell. Square-root scaling makes moderate activity
visible. Translucent concentric circles represent each cell:

| Color | Meaning |
| --- | --- |
| Blue | Low relative observation density |
| Cyan | Moderate relative observation density |
| Yellow | High relative observation density |
| Red | Highest relative observation density |

Saving repeatedly in one place records time spent there and can make its color
warmer. Colors are relative to the current rendered history, so they do not change
after every insert or represent a fixed number of visits. The newest 2,000 samples
contribute to the overlay; SQLite retains the full history. Existing native circles
are reused to keep regular count and color updates responsive.

## Troubleshooting

| Problem | What to check |
| --- | --- |
| Compatible .NET SDK cannot be found | Install .NET 9 and run `dotnet --version` from this project directory. |
| `NETSDK1147` or missing MAUI workload | Install `maui-android` for the selected SDK, then restore again. |
| JDK version is unsupported | Select JDK 17 through `JAVA_HOME`, the IDE configuration or `JavaSdkDirectory`. |
| API 35 `android.jar` is missing | Install Android SDK Platform 35 and verify the SDK directory. |
| No deployment target | Start the emulator or authorize USB debugging, then check `adb devices`. |
| Map is blank or shows only a grid | Check internet access, Maps SDK enablement, billing, API key, package name and signing certificate restrictions. |
| Saved count does not increase | Tap Start tracking, allow location, keep the app in the foreground, and read the status. No samples are saved before the first fix. |
| GPS fix time stays unchanged | A stationary emulator may reuse a fix. The timer should still record it every 10 seconds. Send another simulated position to test fresh GPS delivery. |
| APK update reports a signature mismatch | Use the original signing certificate. Uninstalling removes app history, so preserve any needed data before choosing that option. |

## Project structure and coding practices

| File | Responsibility |
| --- | --- |
| `MainViewModel.cs` | Tracking commands, permissions, recording timer, GPS snapshots, status and demo actions |
| `DatabaseService.cs` | SQLite model, asynchronous schema initialization, validation, transactions and queries |
| `HeatMapRenderer.cs` | Density grouping and reusable native map circles |
| `MainPage.xaml` | Map, sample count, legend, status and action controls |
| `MainPage.xaml.cs` | Page lifecycle, history refresh and confirmed deletion |
| `MauiProgram.cs` | MAUI Maps setup and dependency injection |
| `App.xaml.cs` | Window creation and stopping tracking when backgrounded |
| `Platforms/Android/AndroidManifest.xml` | Android permissions and Google Maps metadata |
| `.editorconfig` | Shared formatting settings |

The app uses MVVM, constructor injection, asynchronous persistence, UTC timestamps,
cancellable recording and UI-thread notifications. GPS snapshots are protected
across threads, and invalid coordinates are rejected before insertion. XML summaries
and inline comments explain the app logic and lifecycle decisions. The generated
`Resources/Fonts/FluentUI.cs` catalog retains its generator-defined glyph names.


## References

- [Microsoft .NET MAUI installation and Android setup](https://learn.microsoft.com/en-us/dotnet/maui/get-started/installation?view=net-maui-9.0&tabs=visual-studio-code)
- [Google Maps SDK for Android setup and API keys](https://developers.google.com/maps/documentation/android-sdk/get-api-key)
- [MAUI Map control](https://learn.microsoft.com/en-us/dotnet/maui/user-interface/controls/map?view=net-maui-9.0)
- [MAUI geolocation](https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/device/geolocation?view=net-maui-9.0)
- [Common C# code conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
