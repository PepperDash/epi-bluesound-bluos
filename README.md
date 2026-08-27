![PepperDash Essentials Plugin Logo](/images/essentials-plugin-blue.png)

# Bluesound BluOS API Essentials Plugin (c) 2026

## License

Provided under MIT license

## Overview

PepperDash Essentials plugin for Bluesound BluOS network audio devices. Communicates with the [BluOS Custom Integration API](documents/BluOS-Custom-Integration-API_v1.7.pdf) over HTTP (port 11000) to provide music service/input selection, preset/playlist management, transport controls, and volume control of the local zone.

**Core feature set:**
- Select service or physical input from `/Browse` root menu (paginated, up to 10/page)
- List and select saved presets/playlists (paginated, up to 10/page)
- Play, pause, next track, previous track
- Shuffle toggle with feedback
- Volume control — local zone (set, up, down with feedback)
- Now-playing feedback: track name, artist, album, album art URL
- Online/offline detection

**Device type name:** `bluesoundapi`

**Minimum Essentials version:** 2.24.0

---

## Dependencies

- [PepperDash Essentials](https://github.com/PepperDash/Essentials) ≥ 2.24.0 (referenced via NuGet as `PepperDashEssentials`)
- Target framework: .NET 4.7.2 (Crestron 4-Series)

---

## Device Configuration

Add a device entry to your Essentials configuration JSON. The `type` must be `bluesoundapi`. The `control` object uses `tcpIp` method with the Bluesound device's IP address and port 11000.

```json
{
  "key": "bluesound-1",
  "name": "Bluesound Node",
  "type": "bluesoundapi",
  "group": "audioPlayer",
  "properties": {
    "control": {
      "method": "tcpIp",
      "tcpSshProperties": {
        "address": "192.168.1.100",
        "port": 11000
      }
    },
    "pollTimeMs": 30000,
    "warningTimeoutMs": 60000,
    "errorTimeoutMs": 120000,
    "volumeStepPercent": 2
  }
}
```

| Property | Type | Default | Description |
|---|---|---|---|
| `control.tcpSshProperties.address` | string | — | IP address of the Bluesound device |
| `control.tcpSshProperties.port` | int | `11000` | BluOS HTTP API port |
| `pollTimeMs` | long | `30000` | How often (ms) to poll `/Status` |
| `warningTimeoutMs` | long | `60000` | Unused (reserved for future monitor) |
| `errorTimeoutMs` | long | `120000` | Unused (reserved for future monitor) |
| `volumeStepPercent` | int | `2` | Step size for VolumeUp/VolumeDown (1–10) |
| `defaultService` | string | — | Pin the browse root to a preferred service name (e.g. `"SoundMachine"`). The plugin auto-navigates into this service on load and constrains Home/Back navigation to its root level. |

---

## Bridge Configuration

Add an EISC bridge entry and reference the device key.

```json
{
  "key": "eisc-bluesound",
  "name": "EISC Bluesound",
  "type": "eiscApiAdvanced",
  "group": "api",
  "properties": {
    "control": {
      "method": "ipidTcp",
      "ipid": "A0",
      "tcpSshProperties": {
        "address": "127.0.0.2",
        "port": 0
      }
    },
    "devices": [
      {
        "deviceKey": "bluesound-1",
        "joinStart": 1
      }
    ]
  }
}
```

The `joinStart` value offsets all join numbers in the table below.  
With `joinStart: 1` the join numbers are as listed. With `joinStart: 101` add 100 to each join number.

---

## Join Map

### Digital Joins

| Join | Name | Direction | Description |
|---|---|---|---|
| 1 | `IsOnline` | To SIMPL | High when device is reachable |
| 2 | `Play` | From SIMPL | Press to resume playback |
| 2 | `IsPlaying` | To SIMPL | High when state is `play` or `stream` |
| 3 | `Pause` | From SIMPL | Press to pause playback |
| 3 | `IsPaused` | To SIMPL | High when state is `pause` |
| 4 | `ShuffleState` | To/From SIMPL | FB = current shuffle state · Press = toggle shuffle |
| 5 | `ServiceHomePageVisible` | To SIMPL | High when not at root browse level — shows Home button |
| 5 | `NextTrack` | From SIMPL | Press to skip to next track |
| 6 | `ServiceBackPageVisible` | To SIMPL | High when not at root browse level — shows Back button |
| 6 | `PreviousTrack` | From SIMPL | Press to go to previous track |
| 7 | `VolumeUp` | From SIMPL | Press to increment volume by step |
| 8 | `VolumeDown` | From SIMPL | Press to decrement volume by step |
| 9 | `PollServiceList` | From SIMPL | Press to re-poll the service/input list |
| 10 | `PollPresetList` | From SIMPL | Press to re-poll the preset list |
| 11 | `ServiceHomePage` | From SIMPL | Return service browse to root level |
| 12 | `ServiceNextPage` | From SIMPL | Advance service list to next page |
| 12 | `ServiceNextPageVisible` | To SIMPL | High when a next service page exists |
| 13 | `ServicePreviousPage` | From SIMPL | Go to previous service list page |
| 13 | `ServicePreviousPageVisible` | To SIMPL | High when a previous service page exists |
| 14 | `ServiceBack` | From SIMPL | Go back one level in service browse hierarchy |
| 16 | `PresetHomePage` | From SIMPL | Reset preset list to first page |
| 17 | `PresetNextPage` | From SIMPL | Advance preset list to next page |
| 17 | `PresetNextPageVisible` | To SIMPL | High when a next preset page exists |
| 18 | `PresetPreviousPage` | From SIMPL | Go to previous preset list page |
| 18 | `PresetPreviousPageVisible` | To SIMPL | High when a previous preset page exists |
| 19 | `PresetBack` | From SIMPL | Reset preset list to first page (alias for PresetHomePage) |
| 21–30 | `SelectServices` | From SIMPL | Press join 21+n to select service slot n (0-based) on current page |
| 31–40 | `SelectPresets` | From SIMPL | Press join 31+n to select preset slot n (0-based) on current page |

### Analog Joins

| Join | Name | Direction | Description |
|---|---|---|---|
| 1 | `Status` | To SIMPL | `0` = offline · `2` = online |
| 2 | `VolumeLevel` | To/From SIMPL | FB = current volume (0–100) · Set = target volume (0–100) |
| 3 | `ServicePageNumber` | To SIMPL | Current service list page number (1-based) |
| 4 | `PresetPageNumber` | To SIMPL | Current preset list page number (1-based) |

### Serial Joins

| Join | Name | Direction | Description |
|---|---|---|---|
| 1 | `DeviceName` | To SIMPL | Essentials device name |
| 2 | `CurrentTrackName` | To SIMPL | Now-playing track title |
| 3 | `CurrentArtist` | To SIMPL | Now-playing artist |
| 4 | `CurrentAlbum` | To SIMPL | Now-playing album |
| 5 | `AlbumArtUrl` | To SIMPL | Album art absolute URL (relative paths resolved to `http://ip:port/...`) |
| 6 | `CurrentServicesMenu` | To SIMPL | Current services menu name ("Home" at root, service name when browsing) |
| 21–30 | `ServiceNames` | To SIMPL | Service/input names for current page (slots 1–10) |
| 31–40 | `PresetNames` | To SIMPL | Preset names for current page (slots 1–10) |

---

## References

- BluOS Custom Integration API v1.7 — [`documents/BluOS-Custom-Integration-API_v1.7.pdf`](documents/BluOS-Custom-Integration-API_v1.7.pdf)
- PepperDash Essentials framework — https://github.com/PepperDash/Essentials
- PepperDash plugin library — https://github.com/PepperDash
- pyblu reference client (endpoint mapping) — https://github.com/LouisChrist/pyblu
- Home Assistant Bluesound integration (reference architecture) — https://github.com/home-assistant/core/tree/dev/homeassistant/components/bluesound

---

## Build

```bash
cd src
dotnet build epi-bluesound-api.4Series.csproj
```

The build outputs a `.cplz` package in `/output/` ready for deployment to a 4-Series processor.

## Generating NuGet Package

A NuGet package is automatically generated on build. To modify package metadata, edit the following in the `.csproj`:

1. `PackageId` — NuGet package name
2. `PackageProjectUrl` — should match this repo URL
3. `AssemblyTitle` — DLL file name shown on processor
<!-- START Minimum Essentials Framework Versions -->
### Minimum Essentials Framework Versions

- 2.24.0
<!-- END Minimum Essentials Framework Versions -->
<!-- START Config Example -->
### Config Example

```json
{
    "key": "GeneratedKey",
    "uid": 1,
    "name": "GeneratedName",
    "type": "bluesoundapi",
    "group": "Group",
    "properties": {
        "control": "SampleValue",
        "pollTimeMs": 0,
        "warningTimeoutMs": 0,
        "errorTimeoutMs": 0,
        "volumeStepPercent": 0,
        "defaultService": "SampleString"
    }
}
```
<!-- END Config Example -->
<!-- START Supported Types -->
### Supported Types

- bluesoundapi
<!-- END Supported Types -->
<!-- START Join Maps -->
### Join Maps

#### Digitals

| Join | Type (RW) | Description |
| --- | --- | --- |
| 1 | R | Device Is Online |
| 2 | R | Play (Press) |
| 2 | R | Is Playing (state = play or stream) |
| 3 | R | Pause (Press) |
| 3 | R | Is Paused |
| 4 | R | Shuffle State (FB) / Toggle Shuffle (Press) |
| 5 | R | Service Home Page Visible (FB) |
| 6 | R | Service Back Page Visible (FB) |
| 5 | R | Next Track (Press) |
| 6 | R | Previous Track (Press) |
| 7 | R | Volume Up — local zone (Press) |
| 8 | R | Volume Down — local zone (Press) |
| 9 | R | Poll Service List (Press) |
| 10 | R | Poll Preset List (Press) |
| 11 | R | Service List — Home Page (Press) |
| 12 | R | Service List — Next Page (Press) |
| 12 | R | Service List — Next Page Visible (FB) |
| 13 | R | Service List — Previous Page (Press) |
| 13 | R | Service List — Previous Page Visible (FB) |
| 14 | R | Service List — Back (Press) |
| 16 | R | Preset List — Home Page (Press) |
| 17 | R | Preset List — Next Page (Press) |
| 17 | R | Preset List — Next Page Visible (FB) |
| 18 | R | Preset List — Previous Page (Press) |
| 18 | R | Preset List — Previous Page Visible (FB) |
| 19 | R | Preset List — Back (Press) |
| 21 | R | Select Service 1-10 (Press, joins 21-30) |
| 31 | R | Select Preset 1-10 (Press, joins 31-40) |

#### Analogs

| Join | Type (RW) | Description |
| --- | --- | --- |
| 1 | R | Device Status (0=Offline, 2=Online) |
| 2 | R | Volume Level 0-100 — local zone (FB/Set) |
| 3 | R | Service List Current Page Number (1-based) |
| 4 | R | Preset List Current Page Number (1-based) |

#### Serials

| Join | Type (RW) | Description |
| --- | --- | --- |
| 1 | R | Device Name |
| 2 | R | Current Track Name |
| 3 | R | Current Artist |
| 4 | R | Current Album |
| 21 | R | Service Names 1-10 (serials 21-30) |
| 31 | R | Preset Names 1-10 (serials 31-40) |
<!-- END Join Maps -->
<!-- START Interfaces Implemented -->

<!-- END Interfaces Implemented -->
<!-- START Base Classes -->
### Base Classes

- EssentialsBridgeableDevice
- JoinMapBaseAdvanced
<!-- END Base Classes -->
<!-- START Public Methods -->
### Public Methods

- public void SetLogger(Action<string> warn)
- public string SendHttpGet(string path, string query = null, int timeoutMs = 5000)
- public string SendLongPollGet(string path, string query = null, int timeoutMs = 35000)
- public void AbortLongPoll()
- public string ResolveUrl(string path)
- public void Dispatch()
- public void Poll()
- public void ServiceNextPage()
- public void ServicePreviousPage()
- public void ServiceHomePage()
- public void ServiceBack()
- public void PresetNextPage()
- public void PresetPreviousPage()
- public void PresetHomePage()
- public void Play()
- public void Pause()
- public void NextTrack()
- public void PreviousTrack()
- public void ToggleShuffle()
- public void SelectService(int slotIndex)
- public void SelectPreset(int slotIndex)
- public void SetVolume(int level)
- public void VolumeUp()
- public void VolumeDown()
<!-- END Public Methods -->
<!-- START Bool Feedbacks -->
### Bool Feedbacks

- OnlineFeedback
- ConnectFeedback
- IsPlayingFeedback
- IsPausedFeedback
- ShuffleFeedback
- ServiceHomePageVisibleFeedback
- ServiceBackPageVisibleFeedback
- ServiceNextPageVisibleFeedback
- ServicePreviousPageVisibleFeedback
- PresetNextPageVisibleFeedback
- PresetPreviousPageVisibleFeedback
<!-- END Bool Feedbacks -->
<!-- START Int Feedbacks -->
### Int Feedbacks

- StatusFeedback
- VolumeLevelFeedback
- ServicePageFeedback
- PresetPageFeedback
<!-- END Int Feedbacks -->
<!-- START String Feedbacks -->
### String Feedbacks

- CurrentTrackNameFeedback
- CurrentArtistFeedback
- CurrentAlbumFeedback
- AlbumArtUrlFeedback
- CurrentServicesMenuFeedback
<!-- END String Feedbacks -->
