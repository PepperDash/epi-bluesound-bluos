using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace PepperDash.Essentials.Plugin
{
    /// <summary>
    /// Handles CRPC (Crestron Remote Protocol for Crestron) message parsing, routing, and response generation.
    /// Supports versions 1.0 and 2.0. Delegates player commands to a provided callback handler.
    /// </summary>
    public class BluesoundCrpcBridge
    {
        private readonly string crpcVersion;
        private readonly string playerInstanceName;
        private readonly IBluesoundCrpcHandler playerHandler;
        private Action<string> logWarning;

        private string crpcHandle = string.Empty;
        private string crpcClientUuid = string.Empty;
        private readonly StringBuilder crpcPartialInput = new StringBuilder();
        private string crpcOutMessage = string.Empty;
        private readonly Dictionary<string, string> crpcEventHandles = new Dictionary<string, string>();

        // Output callback — fired whenever CRPC data is ready to send to SIMPL
        public Action<string> OnCrpcOutput { get; set; }

        // Event subscription callback — fired when a client subscribes to state events
        public Action<string> OnClientSubscribed { get; set; }

        public BluesoundCrpcBridge(string version = "1.0", string instanceName = "BluesoundPlayer1", IBluesoundCrpcHandler handler = null)
        {
            crpcVersion = version ?? "1.0";
            playerInstanceName = instanceName ?? "BluesoundPlayer1";
            playerHandler = handler;
        }

        /// <summary>
        /// Set a logging callback for CRPC warnings
        /// </summary>
        public void SetLogger(Action<string> warn)
        {
            logWarning = warn;
        }

        /// <summary>
        /// Parses an incoming CRPC-framed string from SIMPL.
        /// Handles continuation chunks and reassembles multi-frame messages.
        /// </summary>
        public void ParseAndHandleCrpc(string raw)
        {
            if (string.IsNullOrEmpty(raw) || raw.Length < 8) return;

            // Preamble: 205X00LL  — X='e' (last/only), X='c' (continuation), LL=2-char hex length
            var isLast = raw[3] == 'e';
            var payload = raw.Substring(8);

            if (!isLast)
            {
                crpcPartialInput.Append(payload);
                return;
            }

            crpcPartialInput.Append(payload);
            var fullJson = crpcPartialInput.ToString();
            crpcPartialInput.Clear();

            // Split on {"jsonrpc": in case multiple messages were concatenated
            var parts = fullJson.Split(new[] { "{\"jsonrpc\":" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part)) continue;
                try
                {
                    RouteCrpcMessage(JObject.Parse("{\"jsonrpc\":" + part));
                }
                catch (Exception ex)
                {
                    logWarning?.Invoke(string.Format("CRPC parse error: {0}", ex.Message));
                }
            }
        }

        private void RouteCrpcMessage(JObject msg)
        {
            var id = (int?)msg["id"] ?? 0;
            var method = (string)msg["method"] ?? string.Empty;
            var p = msg["params"] as JObject;

            if (method.StartsWith("Crpc.", StringComparison.OrdinalIgnoreCase))
            {
                HandleCrpcSystemMethod(id, method, p);
                return;
            }

            var dot = method.IndexOf('.');
            if (dot < 0) return;

            HandlePlayerMethod(id, method.Substring(dot + 1), p);
        }

        private void HandleCrpcSystemMethod(int id, string method, JObject p)
        {
            switch (method)
            {
                case "Crpc.Register":
                    crpcHandle = (string)(p != null ? p["handle"] : null) ?? "c5ux";
                    crpcClientUuid = (string)(p != null ? p["uuid"] : null) ?? string.Empty;
                    SendCrpcResponse(id, new JObject { ["ver"] = crpcVersion, ["uuid"] = crpcClientUuid });
                    OnClientSubscribed?.Invoke("Registered");
                    break;

                case "Crpc.GetObjects":
                    var objects = new JArray
                    {
                        new JObject
                        {
                            ["instanceName"] = playerInstanceName,
                            ["interfaces"] = new JArray { "IMediaPlayer" },
                            ["isIMediaPlayer"] = true
                        }
                    };
                    SendCrpcResponse(id, new JObject { ["objects"] = new JObject { ["object"] = objects } });
                    break;

                default:
                    SendCrpcResponse(id, new JObject());
                    break;
            }
        }

        private void HandlePlayerMethod(int id, string method, JObject p)
        {
            if (playerHandler == null) return;

            switch (method)
            {
                case "Play":
                    playerHandler.OnCrpcPlay();
                    SendCrpcResponse(id, new JObject());
                    break;

                case "Pause":
                    playerHandler.OnCrpcPause();
                    SendCrpcResponse(id, new JObject());
                    break;

                case "NextTrack":
                    playerHandler.OnCrpcNextTrack();
                    SendCrpcResponse(id, new JObject());
                    break;

                case "PreviousTrack":
                    playerHandler.OnCrpcPreviousTrack();
                    SendCrpcResponse(id, new JObject());
                    break;

                case "Shuffle":
                case "ToggleShuffle":
                    playerHandler.OnCrpcToggleShuffle();
                    SendCrpcResponse(id, new JObject());
                    break;

                case "GetMenu":
                    SendCrpcResponse(id, new JObject { ["instanceName"] = "BluesoundBrowser1" });
                    break;

                case "RegisterEvent":
                    var ev = (string)(p != null ? p["ev"] : null) ?? string.Empty;
                    var handle = (string)(p != null ? p["handle"] : null) ?? crpcHandle;
                    if (!string.IsNullOrEmpty(ev))
                    {
                        crpcEventHandles[ev] = handle;
                        OnClientSubscribed?.Invoke(ev);
                    }
                    SendCrpcResponse(id, new JObject());
                    break;

                case "GetProperty":
                    HandleCrpcGetProperty(id, p);
                    break;

                default:
                    SendCrpcResponse(id, new JObject());
                    break;
            }
        }

        private void HandleCrpcGetProperty(int id, JObject p)
        {
            if (playerHandler == null) return;

            var propName = (string)(p != null ? p["propName"] : null) ?? string.Empty;
            switch (propName)
            {
                case "TextLines":
                    var lines = playerHandler.GetTextLines();
                    SendCrpcResponse(id, new JObject
                    {
                        ["TextLines"] = new JArray(lines.Cast<object>())
                    });
                    break;

                case "PropertiesSupported":
                    SendCrpcResponse(id, new JObject
                    {
                        ["PropertiesSupported"] = new JArray
                            { "TextLines", "AlbumArtUri", "ElapsedSec", "TrackSec", "PlayerState", "ShuffleState" }
                    });
                    break;

                case "AlbumArtUri":
                    SendCrpcResponse(id, new JObject { ["AlbumArtUri"] = playerHandler.GetAlbumArtUrl() ?? string.Empty });
                    break;

                case "PlayerState":
                    SendCrpcResponse(id, new JObject { ["PlayerState"] = playerHandler.GetPlayerState() });
                    break;

                default:
                    SendCrpcResponse(id, new JObject());
                    break;
            }
        }

        private void SendCrpcResponse(int id, JObject result)
        {
            EmitCrpc(new JObject
            {
                ["jsonrpc"] = "2.0",
                ["id"] = id,
                ["result"] = result
            }.ToString(Newtonsoft.Json.Formatting.None));
        }

        public void SendCrpcEvent(string eventName, JObject parameters)
        {
            string handle;
            if (!crpcEventHandles.TryGetValue(eventName, out handle))
                handle = crpcHandle;
            if (string.IsNullOrEmpty(handle)) return;

            EmitCrpc(new JObject
            {
                ["jsonrpc"] = "2.0",
                ["id"] = 1,
                ["method"] = playerInstanceName + ".Event",
                ["params"] = new JObject
                {
                    ["ev"] = eventName,
                    ["handle"] = handle,
                    ["parameters"] = parameters
                }
            }.ToString(Newtonsoft.Json.Formatting.None));
        }

        public bool HasEventSubscription(string eventName)
        {
            return crpcEventHandles.ContainsKey(eventName);
        }

        private void EmitCrpc(string json)
        {
            const int maxChunk = 247;
            var offset = 0;
            while (offset < json.Length)
            {
                var len = Math.Min(maxChunk, json.Length - offset);
                var isLast = offset + len >= json.Length;
                var flag = isLast ? "e" : "c";
                var chunk = json.Substring(offset, len);
                offset += len;
                crpcOutMessage = string.Format("205{0}00{1:x2}{2}", flag, len, chunk);
                OnCrpcOutput?.Invoke(crpcOutMessage);
            }
        }
    }

    /// <summary>
    /// Interface for the player device to implement CRPC handler callbacks and property accessors
    /// </summary>
    public interface IBluesoundCrpcHandler
    {
        void OnCrpcPlay();
        void OnCrpcPause();
        void OnCrpcNextTrack();
        void OnCrpcPreviousTrack();
        void OnCrpcToggleShuffle();

        string[] GetTextLines();
        string GetAlbumArtUrl();
        string GetPlayerState();
    }
}
