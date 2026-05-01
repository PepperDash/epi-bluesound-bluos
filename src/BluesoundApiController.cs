// For Basic SIMPL# Classes
// For Basic SIMPL#Pro classes

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Core;
using PepperDash.Core.Logging;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.Queues;

namespace PepperDash.Essentials.Plugin
{
    /// <summary>
    /// Bluesound BluOS HTTP API Essentials plugin device.
    /// Communicates with the BluOS REST API on port 11000 via BluesoundHttpClient.
    /// </summary>
    public class BluesoundApiDevice : EssentialsBridgeableDevice
    {
        private const int pageSize = 10;

        private readonly BluesoundApiPropertiesConfig config;
        private readonly GenericQueue receiveQueue;
        private readonly BluesoundHttpClient httpClient;
        private CTimer pollTimer;
        private readonly long pollIntervalMs;

        // Player state
        private string playState = string.Empty;
        private bool shuffleState;
        private int volumeLevel;
        private string currentTrackName = string.Empty;
        private string currentArtist = string.Empty;
        private string currentAlbum = string.Empty;
        private string albumArtUrl = string.Empty;
        private bool isOnline;

        // Services (streaming services + physical inputs)
        private readonly List<ServiceEntry> allServices = new List<ServiceEntry>();
        private readonly Stack<string> browseKeyStack = new Stack<string>();
        private readonly Stack<string> browseNameStack = new Stack<string>();
        private string currentServicesMenu = "Home";
        private int servicePageIndex;

        // Presets (user-saved items)
        private readonly List<PresetEntry> allPresets = new List<PresetEntry>();
        private int presetPageIndex;

        // Feedback objects
        /// <summary>Reports device online state</summary>
        public BoolFeedback OnlineFeedback { get; private set; }
        /// <summary>Reports device online state (alias)</summary>
        public BoolFeedback ConnectFeedback { get; private set; }
        /// <summary>Reports numeric status (0=offline, 2=online)</summary>
        public IntFeedback StatusFeedback { get; private set; }
        /// <summary>True when playback state is play or stream</summary>
        public BoolFeedback IsPlayingFeedback { get; private set; }
        /// <summary>True when playback state is paused</summary>
        public BoolFeedback IsPausedFeedback { get; private set; }
        /// <summary>Current shuffle state</summary>
        public BoolFeedback ShuffleFeedback { get; private set; }
        /// <summary>Current volume level 0-100</summary>
        public IntFeedback VolumeLevelFeedback { get; private set; }
        /// <summary>Currently playing track name</summary>
        public StringFeedback CurrentTrackNameFeedback { get; private set; }
        /// <summary>Currently playing artist</summary>
        public StringFeedback CurrentArtistFeedback { get; private set; }
        /// <summary>Currently playing album</summary>
        public StringFeedback CurrentAlbumFeedback { get; private set; }
        /// <summary>Album art absolute URL</summary>
        public StringFeedback AlbumArtUrlFeedback { get; private set; }
        /// <summary>Current service list page number (1-based)</summary>
        public IntFeedback ServicePageFeedback { get; private set; }
        /// <summary>Current preset list page number (1-based)</summary>
        public IntFeedback PresetPageFeedback { get; private set; }
        /// <summary>Service name feedback slots for the current page (PageSize slots)</summary>
        public StringFeedback[] ServiceNameFeedbacks { get; private set; }
        /// <summary>Preset name feedback slots for the current page (PageSize slots)</summary>
        public StringFeedback[] PresetNameFeedbacks { get; private set; }
        /// <summary>Current services menu name ("Home" at root, service name when browsing)</summary>
        public StringFeedback CurrentServicesMenuFeedback { get; private set; }
        /// <summary>True when not at root browse level — shows Home button visibility</summary>
        public BoolFeedback ServiceHomePageVisibleFeedback { get; private set; }
        /// <summary>True when not at root browse level — shows Back button visibility</summary>
        public BoolFeedback ServiceBackPageVisibleFeedback { get; private set; }

        private sealed class ServiceEntry
        {
            public string Name { get; set; }
            public string Url { get; set; }
            public string BrowseKey { get; set; }
        }

        private sealed class PresetEntry
        {
            public string Name { get; set; }
            public int Id { get; set; }
        }

        private sealed class CommandMessage : IQueueMessage
        {
            private readonly Action action;
            internal CommandMessage(Action a) { action = a; }
            public void Dispatch() { action?.Invoke(); }
        }

        /// <summary>
        /// Plugin device constructor for the BluOS HTTP API
        /// </summary>
        /// <param name="key">Device key</param>
        /// <param name="name">Device friendly name</param>
        /// <param name="config">Deserialized properties config</param>
        public BluesoundApiDevice(string key, string name, BluesoundApiPropertiesConfig config)
            : base(key, name)
        {
            this.LogInformation("Constructing new {0} instance", name);

            this.config = config;

            var tcpProps = config?.Control?.TcpSshProperties;
            var address = tcpProps?.Address ?? string.Empty;
            var port = tcpProps != null && tcpProps.Port > 0 ? tcpProps.Port : 11000;

            httpClient = new BluesoundHttpClient(address, port);
            httpClient.SetLogger(msg => this.LogWarning(msg));
            receiveQueue = new GenericQueue(key + "-rxqueue");

            OnlineFeedback = new BoolFeedback("online", () => isOnline);
            ConnectFeedback = new BoolFeedback("connect", () => isOnline);
            StatusFeedback = new IntFeedback("status", () => isOnline ? 2 : 0);
            IsPlayingFeedback = new BoolFeedback("isPlaying", () => playState == "play" || playState == "stream");
            IsPausedFeedback = new BoolFeedback("isPaused", () => playState == "pause");
            ShuffleFeedback = new BoolFeedback("shuffle", () => shuffleState);
            VolumeLevelFeedback = new IntFeedback("volumeLevel", () => (int)((volumeLevel * 65535L + 50) / 100));
            CurrentTrackNameFeedback = new StringFeedback("trackName", () => currentTrackName);
            CurrentArtistFeedback = new StringFeedback("artist", () => currentArtist);
            CurrentAlbumFeedback = new StringFeedback("album", () => currentAlbum);
            AlbumArtUrlFeedback = new StringFeedback("albumArt", () => albumArtUrl);
            ServicePageFeedback = new IntFeedback("servicePage", () => servicePageIndex + 1);
            PresetPageFeedback = new IntFeedback("presetPage", () => presetPageIndex + 1);
            CurrentServicesMenuFeedback = new StringFeedback("servicesMenu", () => currentServicesMenu);
            ServiceHomePageVisibleFeedback = new BoolFeedback("svcHomeVis", () => browseKeyStack.Count > 0);
            ServiceBackPageVisibleFeedback = new BoolFeedback("svcBackVis", () => browseKeyStack.Count > 0);

            ServiceNameFeedbacks = new StringFeedback[pageSize];
            PresetNameFeedbacks = new StringFeedback[pageSize];

            for (var i = 0; i < pageSize; i++)
            {
                var slot = i;
                ServiceNameFeedbacks[i] = new StringFeedback("svcName" + slot, () => GetServicePagedName(slot));
                PresetNameFeedbacks[i] = new StringFeedback("presetName" + slot, () => GetPresetPagedName(slot));
            }

            pollIntervalMs = config != null && config.PollTimeMs > 0 ? config.PollTimeMs : 30000L;
            pollTimer = new CTimer(o => PollWorker(), pollIntervalMs);
        }

        /// <summary>
        /// Called by the framework after construction — sets Enabled so the bridge operates normally
        /// </summary>
        public override bool CustomActivate()
        {
            Enabled = true;
            return base.CustomActivate();
        }

        #region Polling and Status Parsing

        /// <summary>
        /// Polls the device by issuing GET /Status.
        /// Called periodically by the internal poll timer.
        /// </summary>
        public void Poll()
        {
            if (pollTimer != null)
                pollTimer.Reset(100);
        }

        private void PollWorker()
        {
            try
            {
                var pollTimeoutSec = config != null && config.PollTimeMs > 0
                    ? (int)(config.PollTimeMs / 1000)
                    : 30;
                var httpTimeoutMs = (pollTimeoutSec + 5) * 1000;
                this.LogDebug("PollWorker — GET /Status timeout={timeout}s, httpTimeout={httpTimeout}ms", pollTimeoutSec.ToString(), httpTimeoutMs.ToString());
                var response = httpClient.SendLongPollGet("/Status", "timeout=" + pollTimeoutSec, httpTimeoutMs);
                if (response == null)
                {
                    this.LogDebug("PollWorker — /Status returned null, wasOnline={wasOnline}", isOnline.ToString());
                    if (isOnline)
                    {
                        isOnline = false;
                        FireStatusFeedbacks();
                    }
                    return;
                }

                var wasOffline = !isOnline;
                isOnline = true;
                this.LogDebug("PollWorker — /Status OK, wasOffline={wasOffline}", wasOffline.ToString());

                if (wasOffline)
                {
                    FireStatusFeedbacks();
                    receiveQueue.Enqueue(new CommandMessage(() => RefreshServices()));
                }

                ParseStatusResponse(response);
            }
            finally
            {
                if (pollTimer != null)
                    pollTimer.Reset(pollIntervalMs);
            }
        }

        private void ParseStatusResponse(string xml)
        {
            this.LogDebug("ParseStatusResponse — parsing XML response ({len} chars)", xml.Length.ToString());

            try
            {
                var doc = XDocument.Parse(xml);
                var root = doc.Root;
                if (root == null) return;

                var newPlayState = (string)root.Element("state") ?? string.Empty;
                var newShuffle = (string)root.Element("shuffle") == "1";
                var newVolume = ParseInt((string)root.Element("volume"), volumeLevel);
                // Some services use <name> for track title; others (e.g. Radio Paradise)
                // use <title2> for the song name and <title1> for the station/channel
                var newTrack = (string)root.Element("name")
                    ?? (string)root.Element("title2")
                    ?? string.Empty;
                var newArtist = (string)root.Element("artist")
                    ?? (string)root.Element("title3")
                    ?? string.Empty;
                var newAlbum = (string)root.Element("album") ?? string.Empty;
                var artPath = (string)root.Element("image") ?? string.Empty;
                var newArtUrl = httpClient.ResolveUrl(artPath);

                var transportChanged = newPlayState != playState;
                var shuffleChanged = newShuffle != shuffleState;
                var volumeChanged = newVolume != volumeLevel;
                var trackChanged = newTrack != currentTrackName || newArtist != currentArtist
                    || newAlbum != currentAlbum || newArtUrl != albumArtUrl;

                playState = newPlayState;
                shuffleState = newShuffle;
                volumeLevel = newVolume;
                currentTrackName = newTrack;
                currentArtist = newArtist;
                currentAlbum = newAlbum;
                albumArtUrl = newArtUrl;

                if (transportChanged)
                {
                    IsPlayingFeedback.FireUpdate();
                    IsPausedFeedback.FireUpdate();
                }
                if (shuffleChanged)
                    ShuffleFeedback.FireUpdate();
                if (volumeChanged)
                    VolumeLevelFeedback.FireUpdate();
                if (trackChanged)
                {
                    CurrentTrackNameFeedback.FireUpdate();
                    CurrentArtistFeedback.FireUpdate();
                    CurrentAlbumFeedback.FireUpdate();
                    AlbumArtUrlFeedback.FireUpdate();
                }
            }
            catch (Exception ex)
            {
                this.LogWarning("ParseStatusResponse failed: {ex}", ex.Message);
            }
        }

        #endregion

        #region Services and Presets

        private void RefreshServices()
        {
            if (BrowseServices(null))
                FireServiceFeedbacks();
        }

        private const int browseTimeoutMs = 15000;

        private bool BrowseServices(string browseKey)
        {
            // Abort any in-flight long-poll so the device connection is freed immediately
            httpClient.AbortLongPoll();

            string response;
            if (string.IsNullOrEmpty(browseKey))
            {
                this.LogDebug("BrowseServices — fetching /Browse (root)");
                response = httpClient.SendHttpGet("/Browse", null, browseTimeoutMs);
            }
            else
            {
                this.LogDebug("BrowseServices — fetching /Browse?key={key}", browseKey);
                // browseKey values contain percent-encoded segments (e.g. %2F, %3F) that must
                // survive on the wire. .NET's Uri class decodes them during URL construction,
                // so re-encode '%' as '%25' to compensate for one level of Uri normalization.
                // The '&' separators in the key are intentional extra query parameters.
                var escapedKey = browseKey.Replace("%", "%25");
                response = httpClient.SendHttpGet("/Browse", "key=" + escapedKey, browseTimeoutMs);
            }

            if (response == null)
            {
                this.LogDebug("BrowseServices — /Browse returned null");
                return false;
            }

            try
            {
                var doc = XDocument.Parse(response);
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var items = doc.Descendants("item")
                    .Where(el => !el.Elements("item").Any())
                    .Select(el => new ServiceEntry
                    {
                        Name = (string)el.Attribute("text") ?? string.Empty,
                        Url = (string)el.Attribute("playURL") ?? string.Empty,
                        BrowseKey = (string)el.Attribute("browseKey") ?? string.Empty
                    })
                    .Where(s => !string.IsNullOrEmpty(s.Name) && seen.Add(s.Name))
                    .ToList();

                this.LogDebug("BrowseServices — found {count} items", items.Count.ToString());

                allServices.Clear();
                allServices.AddRange(items);
                servicePageIndex = 0;
                return true;
            }
            catch (Exception ex)
            {
                this.LogWarning("BrowseServices failed: {ex}", ex.Message);
                return false;
            }
        }

        private void RefreshPresets()
        {
            this.LogDebug("RefreshPresets — fetching /Presets");
            // Abort any in-flight long-poll so the device connection is freed immediately
            httpClient.AbortLongPoll();
            var response = httpClient.SendHttpGet("/Presets");
            if (response == null)
            {
                this.LogDebug("RefreshPresets — /Presets returned null");
                return;
            }

            try
            {
                var doc = XDocument.Parse(response);
                var items = doc.Descendants("preset")
                    .Select(el => new PresetEntry
                    {
                        Name = (string)el.Attribute("name") ?? string.Empty,
                        Id = ParseInt((string)el.Attribute("id"), -1)
                    })
                    .Where(p => !string.IsNullOrEmpty(p.Name) && p.Id >= 0)
                    .ToList();

                this.LogDebug("RefreshPresets — found {count} presets", items.Count.ToString());
                allPresets.Clear();
                allPresets.AddRange(items);
                presetPageIndex = 0;
                FirePresetFeedbacks();
            }
            catch (Exception ex)
            {
                this.LogWarning("RefreshPresets failed: {ex}", ex.Message);
            }
        }

        #endregion

        #region Paging

        /// <summary>Advance the services list to the next page</summary>
        public void ServiceNextPage()
        {
            if (servicePageIndex < GetServiceMaxPage())
            {
                servicePageIndex++;
                FireServiceFeedbacks();
            }
        }

        /// <summary>Go to the previous services page</summary>
        public void ServicePreviousPage()
        {
            if (servicePageIndex > 0)
            {
                servicePageIndex--;
                FireServiceFeedbacks();
            }
        }

        /// <summary>Reset the services list to the root /Browse level</summary>
        public void ServiceHomePage()
        {
            this.LogDebug("ServiceHomePage — returning to root");
            receiveQueue.Enqueue(new CommandMessage(() =>
            {
                browseKeyStack.Clear();
                browseNameStack.Clear();
                currentServicesMenu = "Home";
                // Clear service names before browsing so stale slots don't persist
                allServices.Clear();
                servicePageIndex = 0;
                FireServiceFeedbacks();
                BrowseServices(null);
                FireServiceFeedbacks();
            }));
        }

        /// <summary>Go back one level in the service browse hierarchy</summary>
        public void ServiceBack()
        {
            receiveQueue.Enqueue(new CommandMessage(() =>
            {
                if (browseKeyStack.Count == 0)
                {
                    this.LogDebug("ServiceBack — already at root");
                    return;
                }
                browseKeyStack.Pop();
                browseNameStack.Pop();
                var parentKey = browseKeyStack.Count > 0 ? browseKeyStack.Peek() : null;
                currentServicesMenu = browseNameStack.Count > 0 ? browseNameStack.Peek() : "Home";
                this.LogDebug("ServiceBack — navigating to key={key}", parentKey ?? "(root)");
                // Clear service names before browsing so stale slots don't persist
                allServices.Clear();
                servicePageIndex = 0;
                FireServiceFeedbacks();
                BrowseServices(parentKey);
                FireServiceFeedbacks();
            }));
        }

        /// <summary>Advance the presets list to the next page</summary>
        public void PresetNextPage()
        {
            if (presetPageIndex < GetPresetMaxPage())
            {
                presetPageIndex++;
                FirePresetFeedbacks();
            }
        }

        /// <summary>Go to the previous presets page</summary>
        public void PresetPreviousPage()
        {
            if (presetPageIndex > 0)
            {
                presetPageIndex--;
                FirePresetFeedbacks();
            }
        }

        /// <summary>Reset the presets list to the first page</summary>
        public void PresetHomePage()
        {
            presetPageIndex = 0;
            FirePresetFeedbacks();
        }

        private int GetServiceMaxPage()
        {
            return allServices.Count == 0 ? 0 : (allServices.Count - 1) / pageSize;
        }

        private int GetPresetMaxPage()
        {
            return allPresets.Count == 0 ? 0 : (allPresets.Count - 1) / pageSize;
        }

        #endregion

        #region Transport Commands

        /// <summary>Resume playback via GET /Play</summary>
        public void Play()
        {
            this.LogDebug("Play pressed");
            SendCommandAsync("/Play");
        }

        /// <summary>Play a specific URL via GET /Play?query</summary>
        /// <param name="query">Full query string (e.g. "url=RadioParadise%3A...")</param>
        private void PlayUrl(string query)
        {
            this.LogDebug("PlayUrl query={query}", query);
            SendCommandAsync("/Play", query);
        }

        /// <summary>Pause playback via GET /Pause</summary>
        public void Pause()
        {
            this.LogDebug("Pause pressed");
            SendCommandAsync("/Pause");
        }

        /// <summary>Skip to next track via GET /Skip</summary>
        public void NextTrack()
        {
            this.LogDebug("NextTrack pressed");
            SendCommandAsync("/Skip");
        }

        /// <summary>Go to previous track via GET /Back</summary>
        public void PreviousTrack()
        {
            this.LogDebug("PreviousTrack pressed");
            SendCommandAsync("/Back");
        }

        /// <summary>Toggle shuffle on/off via GET /Shuffle</summary>
        public void ToggleShuffle()
        {
            var newState = shuffleState ? "0" : "1";
            this.LogDebug("ToggleShuffle pressed, new state={state}", newState);
            SendCommandAsync("/Shuffle", "state=" + newState);
        }

        /// <summary>Select a service/input by 0-based slot index on the current page</summary>
        /// <param name="slotIndex">0-based slot within the current page</param>
        public void SelectService(int slotIndex)
        {
            receiveQueue.Enqueue(new CommandMessage(() => SelectServiceWorker(slotIndex)));
        }

        private void SelectServiceWorker(int slotIndex)
        {
            var abs = servicePageIndex * pageSize + slotIndex;
            this.LogDebug("SelectService slot={slot}, abs={abs}, count={count}", slotIndex.ToString(), abs.ToString(), allServices.Count.ToString());
            if (abs >= allServices.Count)
            {
                this.LogDebug("SelectService — index out of range, ignoring");
                return;
            }

            var entry = allServices[abs];

            // Link-type items have a browseKey — drill into them
            if (!string.IsNullOrEmpty(entry.BrowseKey))
            {
                this.LogDebug("SelectService — browsing into '{name}' key={key}", entry.Name, entry.BrowseKey);
                // Clear service names before browsing so stale slots don't persist
                allServices.Clear();
                servicePageIndex = 0;
                FireServiceFeedbacks();
                if (BrowseServices(entry.BrowseKey))
                {
                    browseKeyStack.Push(entry.BrowseKey);
                    browseNameStack.Push(entry.Name);
                    currentServicesMenu = entry.Name;
                    FireServiceFeedbacks();
                }
                return;
            }

            // Audio-type items have a playURL — play them
            var url = entry.Url;
            if (string.IsNullOrEmpty(url))
            {
                this.LogDebug("SelectService — no URL or browseKey for '{name}', ignoring", entry.Name);
                return;
            }
            this.LogDebug("SelectService — playing '{name}' url={url}", entry.Name, url);

            // playURL from /Browse is a complete path (e.g. "/Play?url=Capture%3A...")
            // Route through PlayUrl so the play action goes through a single code path
            if (url.StartsWith("/Play?"))
            {
                PlayUrl(url.Substring("/Play?".Length));
            }
            else if (url.StartsWith("/"))
            {
                var qIndex = url.IndexOf('?');
                if (qIndex >= 0)
                    SendCommandAsync(url.Substring(0, qIndex), url.Substring(qIndex + 1));
                else
                    SendCommandAsync(url);
            }
            else
            {
                PlayUrl("url=" + Uri.EscapeDataString(url));
            }
        }

        /// <summary>Select a preset by 0-based slot index on the current page</summary>
        /// <param name="slotIndex">0-based slot within the current page</param>
        public void SelectPreset(int slotIndex)
        {
            var abs = presetPageIndex * pageSize + slotIndex;
            this.LogDebug("SelectPreset slot={slot}, abs={abs}, count={count}", slotIndex.ToString(), abs.ToString(), allPresets.Count.ToString());
            if (abs >= allPresets.Count)
            {
                this.LogDebug("SelectPreset — index out of range, ignoring");
                return;
            }
            this.LogDebug("SelectPreset — loading '{name}' id={id}", allPresets[abs].Name, allPresets[abs].Id.ToString());
            SendCommandAsync("/Preset", "id=" + allPresets[abs].Id);
        }

        #endregion

        #region Volume Commands

        /// <summary>Set the local zone volume level (0-100) via GET /Volume</summary>
        /// <param name="level">Volume level 0-100</param>
        public void SetVolume(int level)
        {
            level = Math.Max(0, Math.Min(100, level));
            this.LogDebug("SetVolume level={level}", level.ToString());
            SendCommandAsync("/Volume", "level=" + level);
        }

        /// <summary>Increment volume by the configured step (default 2%)</summary>
        public void VolumeUp()
        {
            var step = config != null && config.VolumeStepPercent > 0 ? config.VolumeStepPercent : 2;
            this.LogDebug("VolumeUp current={current}, step={step}", volumeLevel.ToString(), step.ToString());
            SetVolume(volumeLevel + step);
        }

        /// <summary>Decrement volume by the configured step (default 2%)</summary>
        public void VolumeDown()
        {
            var step = config != null && config.VolumeStepPercent > 0 ? config.VolumeStepPercent : 2;
            this.LogDebug("VolumeDown current={current}, step={step}", volumeLevel.ToString(), step.ToString());
            SetVolume(volumeLevel - step);
        }

        #endregion

        #region Helpers

        private void SendCommandAsync(string path, string query = null)
        {
            this.LogDebug("SendCommandAsync enqueuing {path}?{query}", path, query ?? string.Empty);
            receiveQueue.Enqueue(new CommandMessage(() =>
            {
                this.LogDebug("SendCommandAsync executing GET {path}?{query}", path, query ?? string.Empty);
                // Abort the in-flight long-poll so PollWorker returns quickly
                httpClient.AbortLongPoll();
                var response = httpClient.SendHttpGet(path, query);
                if (response != null)
                {
                    this.LogDebug("SendCommandAsync {path} response received ({len} chars)", path, response.Length.ToString());
                    ParseStatusResponse(response);
                }
                else
                {
                    this.LogWarning("SendCommandAsync {path} returned null", path);
                }

                // Kick the poll timer so /Status long-poll resumes
                if (pollTimer != null)
                    pollTimer.Reset(200);
            }));
        }

        private string GetServicePagedName(int slotIndex)
        {
            var idx = servicePageIndex * pageSize + slotIndex;
            return idx < allServices.Count ? allServices[idx].Name : string.Empty;
        }

        private string GetPresetPagedName(int slotIndex)
        {
            var idx = presetPageIndex * pageSize + slotIndex;
            return idx < allPresets.Count ? allPresets[idx].Name : string.Empty;
        }

        private static int ParseInt(string value, int fallback)
        {
            int result;
            return int.TryParse(value, out result) ? result : fallback;
        }

        private void FireStatusFeedbacks()
        {
            OnlineFeedback.FireUpdate();
            ConnectFeedback.FireUpdate();
            StatusFeedback.FireUpdate();
        }

        private void FireAllFeedbacks()
        {
            FireStatusFeedbacks();
            IsPlayingFeedback.FireUpdate();
            IsPausedFeedback.FireUpdate();
            ShuffleFeedback.FireUpdate();
            VolumeLevelFeedback.FireUpdate();
            CurrentTrackNameFeedback.FireUpdate();
            CurrentArtistFeedback.FireUpdate();
            CurrentAlbumFeedback.FireUpdate();
            AlbumArtUrlFeedback.FireUpdate();
        }

        private void FireServiceFeedbacks()
        {
            foreach (var fb in ServiceNameFeedbacks) fb.FireUpdate();
            ServicePageFeedback.FireUpdate();
            CurrentServicesMenuFeedback.FireUpdate();
            ServiceHomePageVisibleFeedback.FireUpdate();
            ServiceBackPageVisibleFeedback.FireUpdate();
        }

        private void FirePresetFeedbacks()
        {
            foreach (var fb in PresetNameFeedbacks) fb.FireUpdate();
            PresetPageFeedback.FireUpdate();
        }

        #endregion

        #region Overrides of EssentialsBridgeableDevice

        /// <summary>
        /// Links the plugin device to the EISC bridge
        /// </summary>
        /// <param name="trilist">Target trilist</param>
        /// <param name="joinStart">Join offset for this device on the bridge</param>
        /// <param name="joinMapKey">Optional custom join map key</param>
        /// <param name="bridge">The bridge instance</param>
        public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
        {
            var joinMap = new BluesoundApiBridgeJoinMap(joinStart);
            bridge?.AddJoinMap(Key, joinMap);

            var customJoins = JoinMapHelper.TryGetJoinMapAdvancedForDevice(joinMapKey);
            if (customJoins != null)
                joinMap.SetCustomJoinData(customJoins);

            this.LogDebug("Linking to Trilist {id}", trilist.ID.ToString("X"));
            this.LogInformation("Linking to Bridge Type {type}", GetType().Name);

            trilist.SetString(joinMap.DeviceName.JoinNumber, Name);

            // Bool feedback → SIMPL
            OnlineFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline.JoinNumber]);
            IsPlayingFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IsPlaying.JoinNumber]);
            IsPausedFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IsPaused.JoinNumber]);
            ShuffleFeedback.LinkInputSig(trilist.BooleanInput[joinMap.ShuffleState.JoinNumber]);
            ServiceHomePageVisibleFeedback.LinkInputSig(trilist.BooleanInput[joinMap.ServiceHomePageVisible.JoinNumber]);
            ServiceBackPageVisibleFeedback.LinkInputSig(trilist.BooleanInput[joinMap.ServiceBackPageVisible.JoinNumber]);

            // Analog feedback → SIMPL
            StatusFeedback.LinkInputSig(trilist.UShortInput[joinMap.Status.JoinNumber]);
            VolumeLevelFeedback.LinkInputSig(trilist.UShortInput[joinMap.VolumeLevel.JoinNumber]);
            ServicePageFeedback.LinkInputSig(trilist.UShortInput[joinMap.ServicePageNumber.JoinNumber]);
            PresetPageFeedback.LinkInputSig(trilist.UShortInput[joinMap.PresetPageNumber.JoinNumber]);

            // Serial feedback → SIMPL
            CurrentTrackNameFeedback.LinkInputSig(trilist.StringInput[joinMap.CurrentTrackName.JoinNumber]);
            CurrentArtistFeedback.LinkInputSig(trilist.StringInput[joinMap.CurrentArtist.JoinNumber]);
            CurrentAlbumFeedback.LinkInputSig(trilist.StringInput[joinMap.CurrentAlbum.JoinNumber]);
            AlbumArtUrlFeedback.LinkInputSig(trilist.StringInput[joinMap.AlbumArtUrl.JoinNumber]);
            CurrentServicesMenuFeedback.LinkInputSig(trilist.StringInput[joinMap.CurrentServicesMenu.JoinNumber]);

            for (var i = 0; i < pageSize; i++)
            {
                ServiceNameFeedbacks[i].LinkInputSig(trilist.StringInput[joinMap.ServiceNames.JoinNumber + (uint)i]);
                PresetNameFeedbacks[i].LinkInputSig(trilist.StringInput[joinMap.PresetNames.JoinNumber + (uint)i]);
            }

            // SIMPL → Device: transport
            trilist.SetBoolSigAction(joinMap.Play.JoinNumber, b => { if (b) Play(); });
            trilist.SetBoolSigAction(joinMap.Pause.JoinNumber, b => { if (b) Pause(); });
            trilist.SetBoolSigAction(joinMap.NextTrack.JoinNumber, b => { if (b) NextTrack(); });
            trilist.SetBoolSigAction(joinMap.PreviousTrack.JoinNumber, b => { if (b) PreviousTrack(); });
            trilist.SetBoolSigAction(joinMap.ShuffleState.JoinNumber, b => { if (b) ToggleShuffle(); });

            // SIMPL → Device: volume
            trilist.SetBoolSigAction(joinMap.VolumeUp.JoinNumber, b => { if (b) VolumeUp(); });
            trilist.SetBoolSigAction(joinMap.VolumeDown.JoinNumber, b => { if (b) VolumeDown(); });
            trilist.SetUShortSigAction(joinMap.VolumeLevel.JoinNumber, v => SetVolume((int)((v * 100L + 32767) / 65535)));

            // SIMPL → Device: poll lists
            trilist.SetBoolSigAction(joinMap.PollServiceList.JoinNumber, b => { if (b) receiveQueue.Enqueue(new CommandMessage(() => RefreshServices())); });
            trilist.SetBoolSigAction(joinMap.PollPresetList.JoinNumber, b => { if (b) receiveQueue.Enqueue(new CommandMessage(() => RefreshPresets())); });

            // SIMPL → Device: paging
            trilist.SetBoolSigAction(joinMap.ServiceNextPage.JoinNumber, b => { if (b) ServiceNextPage(); });
            trilist.SetBoolSigAction(joinMap.ServicePreviousPage.JoinNumber, b => { if (b) ServicePreviousPage(); });
            trilist.SetBoolSigAction(joinMap.ServiceHomePage.JoinNumber, b => { if (b) ServiceHomePage(); });
            trilist.SetBoolSigAction(joinMap.ServiceBack.JoinNumber, b => { if (b) ServiceBack(); });
            trilist.SetBoolSigAction(joinMap.PresetNextPage.JoinNumber, b => { if (b) PresetNextPage(); });
            trilist.SetBoolSigAction(joinMap.PresetPreviousPage.JoinNumber, b => { if (b) PresetPreviousPage(); });
            trilist.SetBoolSigAction(joinMap.PresetHomePage.JoinNumber, b => { if (b) PresetHomePage(); });
            trilist.SetBoolSigAction(joinMap.PresetBack.JoinNumber, b => { if (b) PresetPreviousPage(); });

            // SIMPL → Device: item selection (captured loop variable avoids closure issue)
            for (var i = 0; i < pageSize; i++)
            {
                var slot = i;
                trilist.SetBoolSigAction(joinMap.SelectServices.JoinNumber + (uint)i, b => { if (b) SelectService(slot); });
                trilist.SetBoolSigAction(joinMap.SelectPresets.JoinNumber + (uint)i, b => { if (b) SelectPreset(slot); });
            }

            UpdateFeedbacks();

            trilist.OnlineStatusChange += (o, a) =>
            {
                if (!a.DeviceOnLine) return;
                trilist.SetString(joinMap.DeviceName.JoinNumber, Name);
                UpdateFeedbacks();
            };
        }

        private void UpdateFeedbacks()
        {
            FireAllFeedbacks();
            FireServiceFeedbacks();
            FirePresetFeedbacks();
        }

        protected void DisposeBehavior()
        {
            if (pollTimer != null)
            {
                pollTimer.Stop();
                pollTimer.Dispose();
                pollTimer = null;
            }
        }

        #endregion
    }
}

