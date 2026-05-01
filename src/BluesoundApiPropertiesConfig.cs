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
	}
}