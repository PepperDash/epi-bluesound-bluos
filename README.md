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
    "volumeStepPercent": 2,
    "useCrpc": false,
    "crpcVersion": "1.0",
    "crpcPlayerInstanceName": "BluesoundPlayer1"
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
| `useCrpc` | bool | `false` | Activates CRPC serial bridge joins (S10 in/out) alongside the standard EISC joins |
| `crpcVersion` | string | `"1.0"` | CRPC protocol version (`"1.0"` or `"2.0"`) |
| `crpcPlayerInstanceName` | string | `"BluesoundPlayer1"` | CRPC Media Player instance name exposed to the router |

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
| 13 | `ServicePreviousPage` | From SIMPL | Go to previous service list page |
| 14 | `ServiceBack` | From SIMPL | Go back one level in service browse hierarchy |
| 16 | `PresetHomePage` | From SIMPL | Reset preset list to first page |
| 17 | `PresetNextPage` | From SIMPL | Advance preset list to next page |
| 18 | `PresetPreviousPage` | From SIMPL | Go to previous preset list page |
| 19 | `PresetBack` | From SIMPL | Reserved for future preset back navigation |
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
| 10 | `CrpcIn` | From SIMPL | Raw CRPC-framed string from SIMPL Media Player Router → plugin (`useCrpc: true` only) |
| 10 | `CrpcOut` | To SIMPL | Raw CRPC-framed string from plugin → SIMPL Media Player Router (`useCrpc: true` only) |
| 21–30 | `ServiceNames` | To SIMPL | Service/input names for current page (slots 1–10) |
| 31–40 | `PresetNames` | To SIMPL | Preset names for current page (slots 1–10) |

---

## CRPC Field Map (Secondary Reference)

This plugin's active transport and parsing logic is BluOS HTTP/XML. The CRPC map below is provided as a secondary reference for future integration work and troubleshooting only.

### Framing

| Layer | Field | Type | Notes |
|---|---|---|---|
| Transport prefix | `preamble` | 8-char hex string | Observed before JSON body in Media Player CRPC streams |
| Payload | JSON object | JSON-RPC style | Contains `jsonrpc`, `id`, and one of `method`/`params` or `result`/`error` |

### Core Message Fields

| Field | Type | Direction | Description |
|---|---|---|---|
| `jsonrpc` | string | Request/Response | Protocol version string (typically JSON-RPC style) |
| `id` | int | Request/Response | Correlation id linking response to request |
| `method` | string | Request/Event | Method call name, often `Object.MethodSig` |
| `params` | object | Request/Event | Input arguments for method/event |
| `result` | bool/object | Response | Success payload (can be boolean or object map) |
| `error` | object | Response | Error payload object |

### Common Params Fields

| Field | Type | Usage |
|---|---|---|
| `ev` | string | Event name for register/event calls |
| `parameters` | object/string | Event payload details |
| `propName` | string | Property name for get-property calls |
| `name` | string | Registration/object naming |

### Error Object Fields

| Field | Type | Description |
|---|---|---|
| `code` | int | Error code |
| `message` | string | Human-readable error message |
| `data` | object | Optional structured details |

### Practical Parsing Notes

- Requests often parse into `MethodObject` and `MethodSig` by splitting `method` on `.`.
- Responses may be either boolean (`result: true/false`) or object (`result: { ... }`).
- Event types commonly encountered in CRPC tooling: `CallMethod`, `RegisterEvent`, `GetProperty`, `Event`, `Result`, `CrpcError`.

## References

- BluOS Custom Integration API v1.7 — [`documents/BluOS-Custom-Integration-API_v1.7.pdf`](documents/BluOS-Custom-Integration-API_v1.7.pdf)
- Media Player SDK v2.0 (Crestron Application Market) — https://applicationmarket.crestron.com/media-player-sdk-v2-0/
- Media Player SDK Getting Started PDF — https://applicationmarket.crestron.com/content/Help/Crestron/MediaPlayer/SDK%20Getting%20Started.pdf
- Crestron Core 3 Media Objects PDF — https://applicationmarket.crestron.com/content/Help/Crestron/MediaPlayer/Crestron%20Core%203%20Media%20Objects.pdf
- PepperDash Essentials framework — https://github.com/PepperDash/Essentials
- PepperDash plugin library — https://github.com/PepperDash
- pyblu reference client (endpoint mapping) — https://github.com/LouisChrist/pyblu
- Home Assistant Bluesound integration (reference architecture) — https://github.com/home-assistant/core/tree/dev/homeassistant/components/bluesound
- CRPC Visualiser (community reverse-engineering aid) — https://github.com/OlHall/CrpcVisualiser
- Community Crestron Media Player SDK (TypeScript) — https://github.com/KittyKatMiauwMiauw/Crestron-MediaPlayer-SDK
- HTML5 Media Player Framework / CRPC client reference (TypeScript) — https://github.com/JayLiaProgramming/MediaPlayer

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