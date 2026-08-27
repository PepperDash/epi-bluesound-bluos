using System.Collections.Generic;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace PepperDash.Essentials.Plugin
{
	/// <summary>
	/// Plugin device factory for devices that use IBasicCommunication
	/// </summary>
	public class BluesoundApiDeviceFactory : EssentialsPluginDeviceFactory<BluesoundApiDevice>
	{
		/// <summary>
		/// Plugin device factory constructor
		/// </summary>
		public BluesoundApiDeviceFactory()
		{
			MinimumEssentialsFrameworkVersion = "2.24.0";

			TypeNames = new List<string>() { "bluesoundapi" };
		}

		/// <summary>
		/// Builds and returns an instance of BluesoundApiDevice
		/// </summary>
		public override EssentialsDevice BuildDevice(PepperDash.Essentials.Core.Config.DeviceConfig dc)
		{
			Debug.LogVerbose("[{key}] Factory Attempting to create new device from type: {type}", dc.Key, dc.Type);

			var propertiesConfig = dc.Properties.ToObject<BluesoundApiPropertiesConfig>();
			if (propertiesConfig == null)
			{
				Debug.LogError("[{key}] Factory: failed to read properties config for {name}", dc.Key, dc.Name);
				return null;
			}

			return new BluesoundApiDevice(dc.Key, dc.Name, propertiesConfig);
		}
	}
}

