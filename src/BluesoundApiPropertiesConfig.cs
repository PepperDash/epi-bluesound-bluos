using Newtonsoft.Json;
using PepperDash.Essentials.Core;

namespace PepperDash.Essentials.Plugin
{
	/// <summary>
	/// Plugin device configuration object
	/// </summary>
	/// <remarks>
	public class BluesoundApiPropertiesConfig
	{
		/// <summary>
		/// JSON control object
		/// </summary>
		[JsonProperty("control")]
		public EssentialsControlPropertiesConfig Control { get; set; }

		/// <summary>
		/// Serializes the poll time value
		/// </summary>
		[JsonProperty("pollTimeMs")]
		public long PollTimeMs { get; set; }

		/// <summary>
		/// Serializes the warning timeout value
		/// </summary>
		[JsonProperty("warningTimeoutMs")]
		public long WarningTimeoutMs { get; set; }

		/// <summary>
		/// Serializes the error timeout value
		/// </summary>
		[JsonProperty("errorTimeoutMs")]
		public long ErrorTimeoutMs { get; set; }


		/// <summary>
		/// Constuctor
		/// </summary>
		public BluesoundApiPropertiesConfig()
		{
		}

		/// <summary>
		/// Volume step size used by VolumeUp/VolumeDown commands (percent, 1-10, default 2)
		/// </summary>
		[JsonProperty("volumeStepPercent")]
		public int VolumeStepPercent { get; set; }

		/// <summary>
		/// When true, activates the CRPC serial bridge joins (S51 in, S52 out) so that a
		/// Crestron Media Player Router in SIMPL can exchange CRPC messages with this plugin.
		/// The existing EISC join map remains active alongside the CRPC bridge.
		/// </summary>
		[JsonProperty("useCrpc")]
		public bool UseCrpc { get; set; }

		/// <summary>
		/// CRPC protocol version: "1.0" or "2.0" (default "1.0")
		/// </summary>
		[JsonProperty("crpcVersion")]
		public string CrpcVersion { get; set; }

		/// <summary>
		/// CRPC Media Player instance name exposed to the router (default "BluesoundPlayer1")
		/// </summary>
		[JsonProperty("crpcPlayerInstanceName")]
		public string CrpcPlayerInstanceName { get; set; }
	}
}