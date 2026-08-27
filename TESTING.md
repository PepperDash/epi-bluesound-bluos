# Bluesound BluOS API Plugin — Development & Testing Reference

**Session Date:** April 14, 2026  
**Build Status:** ✅ Succeeded (0 errors, 0 warnings)  
**Target Framework:** .NET 4.7.2 (Crestron 4-Series)  
**Minimum Essentials:** 2.24.0

---

## 1. Development Summary

### Phase 1: Initial Architecture (Completed)
- ✅ Scaffold cleanup: removed raw TCP `IBasicCommunication` infrastructure
- ✅ HTTP layer: `BluesoundHttpClient` class handles all BluOS REST API calls
- ✅ Status polling: 30-second configurable `CTimer` with queue-based worker dispatch
- ✅ XML parsing: extracts player state, volume, track/artist/album metadata, album art URL

### Phase 2: Services & Presets Management (Completed)
- ✅ Service browsing: `GET /Browse` (root) or `GET /Browse?key=<browseKey>` — navigates service hierarchy
- ✅ Preset management: `GET /Presets` — returns saved playlists
- ✅ Pagination: 10 items/page with page index-based slicing
- ✅ Paging actions: `NextPage`, `PreviousPage`, `HomePage` methods
- ✅ Selection commands: `SelectService(slotIndex)` and `SelectPreset(slotIndex)`

### Phase 3: Transport & Volume Control (Completed)
- ✅ Transport: `Play()`, `Pause()`, `NextTrack()`, `PreviousTrack()`, `ToggleShuffle()`
- ✅ Volume: `SetVolume(0-100)`, `VolumeUp()`, `VolumeDown()` with configurable step (`volumeStepPercent`)
- ✅ All commands: async dispatch via receive queue, parse response to update state

### Phase 4: EISC Bridge Wiring (Completed)
- ✅ Digital joins: 60 total (online, play/pause, shuffle, transport, paging, selection)
- ✅ Analog joins: 4 total (status, volume, service page, preset page)
- ✅ Serial joins: 50 total (device name, track/artist/album, album art, service/preset names)
- ✅ Feedback → SIMPL: wired all player state feedbacks
- ✅ SIMPL → Device: wired all action input signals with lambda capture for slot selection

### Phase 5: CRPC Bridge Implementation (Completed)
- ✅ CRPC v1.0 & v2.0 support — configurable via `crpcVersion` property
- ✅ JSON-RPC parser: frame reassembly, preamble stripping, multi-message splitting
- ✅ Handler interface: `IBluesoundCrpcHandler` — player delegates callbacks
- ✅ System methods: `Crpc.Register`, `Crpc.GetObjects`, `Crpc.RegisterEvent`
- ✅ Player methods: `Play`, `Pause`, `NextTrack`, `PreviousTrack`, `Shuffle`, `GetProperty`, `RegisterEvent`
- ✅ Properties: `TextLines`, `PropertiesSupported`, `AlbumArtUri`, `PlayerState`
- ✅ Event streaming: `StateChanged` events pushed when client subscribed
- ✅ CRPC framing: chunked output (max 247 bytes/frame) with `205e00LL` (final) / `205c00LL` (continuation) preambles
- ✅ Serial joins S51/S52: wired when `useCrpc: true`

### Phase 6: Code Refactoring (Completed)
- ✅ **`BluesoundHttpClient.cs`** — HTTP GET, request retry, URL resolution
- ✅ **`BluesoundCrpcBridge.cs`** — complete CRPC protocol stack + `IBluesoundCrpcHandler` interface
- ✅ **`BluesoundApiController.cs`** — orchestrator, implements handler, delegates to HTTP/CRPC clients
- ✅ Separation of concerns: HTTP, CRPC, and orchestration logic in separate classes
- ✅ Configurable via properties: CRPC version, instance name, all polling/timeout intervals

---

## 2. Architecture Overview

```
┌─────────────────────────────────────────────────────┐
│          BluesoundApiDevice (EssentialsBridgeableDevice)
│          ├─ Implements IBluesoundCrpcHandler
│          └─ Orchestrator, state holder, feedback owner
├─────────────────────────────────────────────────────┤
│
├─ BluesoundHttpClient (HTTP Communication)          │
│  ├─ SendHttpGet(path, query)                        │
│  ├─ ResolveUrl(path)                                │
│  └─ SetLogger(callback)                             │
│
├─ BluesoundCrpcBridge (CRPC v1.0/v2.0 Protocol)     │
│  ├─ ParseAndHandleCrpc(raw)                         │
│  ├─ SendCrpcEvent(eventName, parameters)            │
│  ├─ HasEventSubscription(eventName)                 │
│  ├─ OnCrpcOutput (callback)                         │
│  ├─ OnClientSubscribed (callback)                   │
│  └─ SetLogger(callback)                             │
│
└─ GenericQueue (Async Task Dispatch)                │
   └─ Receives CommandMessage actions from poll timer
```

### Data Flow

**Polling:**
1. `CTimer` fires at `pollTimeMs` interval → enqueues `PollWorker` 
2. `PollWorker` calls `httpClient.SendHttpGet("/Status")`
3. HTTP response parsed → state updated → all feedbacks fired
4. If CRPC subscribed: `SendCrpcEvent("StateChanged", ...)` emitted

**CRPC Inbound:**
1. SIMPL sends raw framed string via S51 join
2. `LinkToApi` action lambda → `crpcBridge.ParseAndHandleCrpc(raw)`
3. Bridge routes to system method or player method
4. Player method calls device method → state updated → fires CRPC event if subscribed
5. Event emitted via `OnCrpcOutput` → S52 join updated

**Service/Preset Selection:**
1. SIMPL presses join 21+n (service slot) or 41+n (preset slot)
2. `LinkToApi` lambda captures slot → calls `SelectService(slot)` or `SelectPreset(slot)`
3. Device sends `GET /Play?url=...` or `GET /Preset?id=...` 
4. HTTP response triggers parse → state + feedbacks updated

---

## 3. Key Classes & Files

### Core Plugin Classes

| File | Lines | Purpose | Key Methods |
|---|---|---|---|
| **BluesoundApiController.cs** | ~700 | Orchestrator, state holder, CRPC handler | `Poll()`, `Play()`, `Pause()`, `SetVolume()`, `SelectService()`, `SelectPreset()`, `LinkToApi()`, CRPC handler methods |
| **BluesoundHttpClient.cs** | ~65 | HTTP abstraction layer | `SendHttpGet()`, `ResolveUrl()`, `SetLogger()` |
| **BluesoundCrpcBridge.cs** | ~290 | CRPC protocol stack | `ParseAndHandleCrpc()`, `SendCrpcEvent()`, `HasEventSubscription()`, `SetLogger()` |
| **BluesoundApiBridgeJoinMap.cs** | ~220 | EISC join definitions | (all join metadata) |
| **BluesoundApiPropertiesConfig.cs** | ~70 | Config deserialization | JSON properties + CRPC settings |
| **BluesoundApiFactory.cs** | ~50 | Device instantiation | Factory pattern |

### Supporting Files

| File | Purpose |
|---|---|
| **README.md** | Full documentation: overview, config, bridge config, join map, CRPC field map, references |
| **epi-bluesound-api.4Series.csproj** | Build config, NuGet refs (PepperDashEssentials 2.28.1), output paths |
| **.github/instructions/essentials-plugin-csharp.instructions.md** | C# naming/formatting conventions for this repo |

---

## 4. Configuration Examples

### Minimal (HTTP Only)
```json
{
  "key": "bluesound-zone1",
  "name": "Living Room Speaker",
  "type": "bluesoundapi",
  "group": "audioPlayer",
  "properties": {
    "control": {
      "method": "tcpIp",
      "tcpSshProperties": {
        "address": "192.168.1.50",
        "port": 11000
      }
    }
  }
}
```

### Full (HTTP + CRPC v1.0)
```json
{
  "key": "bluesound-zone1",
  "name": "Living Room Speaker",
  "type": "bluesoundapi",
  "group": "audioPlayer",
  "properties": {
    "control": {
      "method": "tcpIp",
      "tcpSshProperties": {
        "address": "192.168.1.50",
        "port": 11000
      }
    },
    "pollTimeMs": 30000,
    "volumeStepPercent": 5,
    "useCrpc": true,
    "crpcVersion": "1.0",
    "crpcPlayerInstanceName": "BluesoundPlayer1"
  }
}
```

### CRPC v2.0 Variant
```json
{
  "properties": {
    "useCrpc": true,
    "crpcVersion": "2.0",
    "crpcPlayerInstanceName": "CustomPlayerName"
  }
}
```

---

## 5. Testing Checklist

### HTTP Communication Layer
- [ ] Device goes **online** when BluOS device is reachable (`isOnline` FB = true, status FB = 2)
- [ ] Device goes **offline** when BluOS device is unreachable (`isOnline` FB = false, status FB = 0)
- [ ] Poll timer fires at configured interval (default 30s)
- [ ] `/Status` endpoint response parsed correctly:
  - [ ] `playState` → `IsPlayingFeedback` / `IsPausedFeedback` working
  - [ ] `volume` → `VolumeLevelFeedback` shows 0–100
  - [ ] `artist`, `name`, `album` → serial feedbacks show correct text
  - [ ] `image` path → resolved to absolute URL in `AlbumArtUrlFeedback`

### Transport Commands
- [ ] **Play** join (D5) → device plays
- [ ] **Pause** join (D6) → device pauses
- [ ] **Next Track** join (D7) → skips forward
- [ ] **Previous Track** join (D8) → goes back
- [ ] **Shuffle** join (D4) → toggles shuffle, feedback reflects state

### Volume Control
- [ ] **Volume Set** analog (A2) with value 0–100 → device volume changes
- [ ] **Volume Feedback** (A2) → shows current volume 0–100
- [ ] **Volume Up** digital (D9) → increments by step (default 2%)
- [ ] **Volume Down** digital (D10) → decrements by step
- [ ] **Step size** configurable via `volumeStepPercent` (test with value 5, 10, etc.)

### Services & Presets (Pagination)
- [ ] **Service List** populated from `/RadioBrowse?service=Capture`
  - [ ] Serial 11–30 show service names (page 1, slots 1–20)
  - [ ] **Service NextPage** (D11) → advances page, names update
  - [ ] **Service PreviousPage** (D12) → goes back
  - [ ] **Service HomePage** (D13) → resets to page 1
  - [ ] **Service Page Number** (A3) shows 1-based page count
- [ ] **Preset List** populated from `/Presets`
  - [ ] Serial 31–50 show preset names
  - [ ] **Preset NextPage/PreviousPage/HomePage** (D14–16) work
  - [ ] **Preset Page Number** (A4) shows correct page
- [ ] **Select Service** join (D21–40) by slot → device plays selected service
- [ ] **Select Preset** join (D41–60) by slot → device plays selected preset

### CRPC Bridge (if enabled via `useCrpc: true`)
- [ ] **Crpc.Register** request → device responds with version + UUID
- [ ] **Crpc.GetObjects** request → returns player instance name from config
- [ ] **RegisterEvent** for "StateChanged" → subscription tracked
- [ ] **Play/Pause/Transport** methods → device responds + plays
- [ ] **GetProperty "TextLines"** → returns [track, artist, album, ""]
- [ ] **GetProperty "TextLines"** + state change → `StateChanged` event emitted via S52
- [ ] **GetProperty "PlayerState"** → returns "Playing" / "Paused" / "Stopped"
- [ ] **Frame reassembly** — send multi-frame message (>247 bytes) → correctly parsed
- [ ] **CRPC version** respects config (`crpcVersion: "1.0"` or `"2.0"`)
- [ ] **Instance name** reflects config (`crpcPlayerInstanceName: "CustomName"`)

### EISC Bridge Wiring
- [ ] Device name appears on S1 after trilist comes online
- [ ] All digital feedback joins updated (D1–4 for online/play/pause/shuffle)
- [ ] All analog feedback joins updated (A1–2 for status/volume)
- [ ] All serial feedback joins updated (S1–50 for metadata + lists)
- [ ] SIMPL action presses are received (verify via logging)

### Error Handling & Edge Cases
- [ ] Device unreachable → polls until timeout, then goes offline (no crashes)
- [ ] Malformed XML in `/Status` → warning logged, state preserved
- [ ] Invalid CRPC JSON → warning logged, bridge stays online
- [ ] Large service/preset lists (>20 items) → pagination works, no index out of bounds
- [ ] Rapid selection commands → queue processes in order, no race conditions
- [ ] Very large frame (>10KB) → CRPC chunking sends multiple 247-byte frames

### Build & Deployment
- [ ] `dotnet build` succeeds with 0 errors, 0 warnings
- [ ] `.cplz` output file created in `/output/`
- [ ] `PepperDashEssentials` NuGet 2.28.1 installed
- [ ] All `using` statements resolve (no missing imports)
- [ ] No obsolete API warnings from Essentials

---

## 6. Known Limitations & Future Scope

### Current Implementation
- **HTTP only** — BluOS API is read-mostly (GET endpoints); no PUT/POST commands
- **Local zone volume** — only controls main zone, not grouped zones
- **No seek** — `Seek()` method not implemented (CRPC only)
- **Static service list** — refreshed on device online transition + periodically on state change
- **No deep browsing** — services/presets are flat lists, not hierarchical
- **No authentication** — BluOS HTTP API on local network assumes no auth required

### Out of Scope (Future Enhancements)
- [ ] Volume control for grouped/follower zones
- [ ] Deep service browsing (e.g., Spotify categories → playlists → tracks)
- [ ] Seek progress tracking
- [ ] Playlist creation/editing
- [ ] Multi-device grouping management
- [ ] Offline playback sync

---

## 7. Reference Links

### Official Documentation
- [BluOS Custom Integration API v1.7](documents/BluOS-Custom-Integration-API_v1.7.pdf)
- [PepperDash Essentials](https://github.com/PepperDash/Essentials) ≥ 2.24.0
- [Crestron Media Player SDK v2.0](https://applicationmarket.crestron.com/media-player-sdk-v2-0/)

### CRPC Protocol Reference
- [Crestron Core 3 Media Objects PDF](https://applicationmarket.crestron.com/content/Help/Crestron/MediaPlayer/Crestron%20Core%203%20Media%20Objects.pdf)
- [CRPC Visualiser (community tool)](https://github.com/OlHall/CrpcVisualiser)
- [Crestron Media Player SDK (TypeScript)](https://github.com/KittyKatMiauwMiauw/Crestron-MediaPlayer-SDK)
- [HTML5 Media Player Framework (CRPC reference)](https://github.com/JayLiaProgramming/MediaPlayer)

### Related Integrations
- [pyblu (Python BluOS client)](https://github.com/LouisChrist/pyblu)
- [Home Assistant Bluesound integration](https://github.com/home-assistant/core/tree/dev/homeassistant/components/bluesound)

---

## 8. Quick Commands

### Build
```bash
cd src
dotnet build epi-bluesound-api.4Series.csproj
```

### Test Build Output
```bash
ls -lh output/
```

### Check for warnings/errors
```bash
dotnet build epi-bluesound-api.4Series.csproj 2>&1 | grep -E "error|warning"
```

### NuGet restore
```bash
dotnet restore epi-bluesound-api.4Series.csproj
```

---

## 9. Testing Environment Setup

### On Crestron 4-Series Processor
1. Copy `.cplz` from `output/` to processor
2. Add device config to Essentials JSON (see section 4)
3. Add EISC bridge config (see README.md)
4. Verify device type: `bluesoundapi`
5. Check BluOS device IP + port 11000 reachable from processor network

### Simulating BluOS Device (Local Dev Testing)
- **Python mock server**: `python -m http.server 11000` in a directory with mock XML responses
- **Example `/Status` response**:
```xml
<?xml version="1.0" encoding="UTF-8"?>
<product>
  <name>TestPlayer</name>
  <state>play</state>
  <shuffle>0</shuffle>
  <volume>45</volume>
  <artist>Test Artist</artist>
  <album>Test Album</album>
  <image>/album_art.jpg</image>
</product>
```

---

## 10. Commit/Session Log

| Phase | What | When | Status |
|---|---|---|---|
| 1 | HTTP layer + polling | Session start | ✅ Complete |
| 2 | Services/presets + paging | Early session | ✅ Complete |
| 3 | Transport + volume cmds | Mid session | ✅ Complete |
| 4 | EISC bridge wiring | Mid session | ✅ Complete |
| 5 | CRPC protocol v1.0/v2.0 | Late session | ✅ Complete |
| 6 | Refactor HTTP/CRPC → classes | End of session | ✅ Complete |

**Total Development Time:** ~6 hours (scaffold → full plugin with CRPC)  
**Lines of Code:** ~1500 (plugin logic) + 220 (bridge join map) + 70 (config)  
**Test Coverage:** Manual testing matrix (see section 5)

---

## Next Steps for Testing

1. **Unit/Integration Testing Tomorrow:**
   - Deploy to 4-Series processor
   - Verify device comes online
   - Test each control path (transport, volume, paging, selection)
   - Verify CRPC messages if enabled

2. **Live BluOS Device Testing:**
   - Connect to actual Bluesound device on network
   - Verify poll finds current state (song, volume, shuffle)
   - Test all transport commands
   - Verify service/preset list populated from real device
   - Test CRPC registration + command routing

3. **Edge Case Testing:**
   - Network disconnect → device offline
   - Rapid commands → queue processes in order
   - Large service list (>100 items) → pagination works
   - CRPC malformed JSON → error handling

4. **Performance & Stability:**
   - 24-hour uptime test (repeated polling)
   - Memory leak check (monitor receive queue)
   - High-frequency commands test (press faster than poll interval)

