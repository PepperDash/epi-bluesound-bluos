using PepperDash.Essentials.Core;

namespace PepperDash.Essentials.Plugin
{
	/// <summary>
	/// Bridge join map for the Bluesound BluOS API plugin.
	/// Digital joins 1-40, Analog joins 1-4, Serial joins 1-40.
	/// </summary>
	public class BluesoundApiBridgeJoinMap : JoinMapBaseAdvanced
	{
		#region Digital

		[JoinName("IsOnline")]
		public JoinDataComplete IsOnline = new JoinDataComplete(
			new JoinData { JoinNumber = 1, JoinSpan = 1 },
			new JoinMetadata { Description = "Device Is Online", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Digital });

		[JoinName("Play")]
		public JoinDataComplete Play = new JoinDataComplete(
			new JoinData { JoinNumber = 2, JoinSpan = 1 },
			new JoinMetadata { Description = "Play (Press)", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Digital });

		[JoinName("IsPlaying")]
		public JoinDataComplete IsPlaying = new JoinDataComplete(
			new JoinData { JoinNumber = 2, JoinSpan = 1 },
			new JoinMetadata { Description = "Is Playing (state = play or stream)", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Digital });

		[JoinName("Pause")]
		public JoinDataComplete Pause = new JoinDataComplete(
			new JoinData { JoinNumber = 3, JoinSpan = 1 },
			new JoinMetadata { Description = "Pause (Press)", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Digital });

		[JoinName("IsPaused")]
		public JoinDataComplete IsPaused = new JoinDataComplete(
			new JoinData { JoinNumber = 3, JoinSpan = 1 },
			new JoinMetadata { Description = "Is Paused", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Digital });

		[JoinName("ShuffleState")]
		public JoinDataComplete ShuffleState = new JoinDataComplete(
			new JoinData { JoinNumber = 4, JoinSpan = 1 },
			new JoinMetadata { Description = "Shuffle State (FB) / Toggle Shuffle (Press)", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Digital });

		[JoinName("ServiceHomePageVisible")]
		public JoinDataComplete ServiceHomePageVisible = new JoinDataComplete(
			new JoinData { JoinNumber = 5, JoinSpan = 1 },
			new JoinMetadata { Description = "Service Home Page Visible (FB)", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Digital });

		[JoinName("ServiceBackPageVisible")]
		public JoinDataComplete ServiceBackPageVisible = new JoinDataComplete(
			new JoinData { JoinNumber = 6, JoinSpan = 1 },
			new JoinMetadata { Description = "Service Back Page Visible (FB)", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Digital });

		[JoinName("NextTrack")]
		public JoinDataComplete NextTrack = new JoinDataComplete(
			new JoinData { JoinNumber = 5, JoinSpan = 1 },
			new JoinMetadata { Description = "Next Track (Press)", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Digital });

		[JoinName("PreviousTrack")]
		public JoinDataComplete PreviousTrack = new JoinDataComplete(
			new JoinData { JoinNumber = 6, JoinSpan = 1 },
			new JoinMetadata { Description = "Previous Track (Press)", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Digital });

		[JoinName("VolumeUp")]
		public JoinDataComplete VolumeUp = new JoinDataComplete(
			new JoinData { JoinNumber = 7, JoinSpan = 1 },
			new JoinMetadata { Description = "Volume Up — local zone (Press)", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Digital });

		[JoinName("VolumeDown")]
		public JoinDataComplete VolumeDown = new JoinDataComplete(
			new JoinData { JoinNumber = 8, JoinSpan = 1 },
			new JoinMetadata { Description = "Volume Down — local zone (Press)", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Digital });

		[JoinName("PollServiceList")]
		public JoinDataComplete PollServiceList = new JoinDataComplete(
			new JoinData { JoinNumber = 9, JoinSpan = 1 },
			new JoinMetadata { Description = "Poll Service List (Press)", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Digital });

		[JoinName("PollPresetList")]
		public JoinDataComplete PollPresetList = new JoinDataComplete(
			new JoinData { JoinNumber = 10, JoinSpan = 1 },
			new JoinMetadata { Description = "Poll Preset List (Press)", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Digital });

		[JoinName("ServiceHomePage")]
		public JoinDataComplete ServiceHomePage = new JoinDataComplete(
			new JoinData { JoinNumber = 11, JoinSpan = 1 },
			new JoinMetadata { Description = "Service List — Home Page (Press)", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Digital });

		[JoinName("ServiceNextPage")]
		public JoinDataComplete ServiceNextPage = new JoinDataComplete(
			new JoinData { JoinNumber = 12, JoinSpan = 1 },
			new JoinMetadata { Description = "Service List — Next Page (Press)", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Digital });

		[JoinName("ServiceNextPageVisible")]
		public JoinDataComplete ServiceNextPageVisible = new JoinDataComplete(
			new JoinData { JoinNumber = 12, JoinSpan = 1 },
			new JoinMetadata { Description = "Service List — Next Page Visible (FB)", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Digital });

		[JoinName("ServicePreviousPage")]
		public JoinDataComplete ServicePreviousPage = new JoinDataComplete(
			new JoinData { JoinNumber = 13, JoinSpan = 1 },
			new JoinMetadata { Description = "Service List — Previous Page (Press)", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Digital });

		[JoinName("ServicePreviousPageVisible")]
		public JoinDataComplete ServicePreviousPageVisible = new JoinDataComplete(
			new JoinData { JoinNumber = 13, JoinSpan = 1 },
			new JoinMetadata { Description = "Service List — Previous Page Visible (FB)", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Digital });

		[JoinName("ServiceBack")]
		public JoinDataComplete ServiceBack = new JoinDataComplete(
			new JoinData { JoinNumber = 14, JoinSpan = 1 },
			new JoinMetadata { Description = "Service List — Back (Press)", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Digital });

		[JoinName("PresetHomePage")]
		public JoinDataComplete PresetHomePage = new JoinDataComplete(
			new JoinData { JoinNumber = 16, JoinSpan = 1 },
			new JoinMetadata { Description = "Preset List — Home Page (Press)", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Digital });

		[JoinName("PresetNextPage")]
		public JoinDataComplete PresetNextPage = new JoinDataComplete(
			new JoinData { JoinNumber = 17, JoinSpan = 1 },
			new JoinMetadata { Description = "Preset List — Next Page (Press)", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Digital });

		[JoinName("PresetNextPageVisible")]
		public JoinDataComplete PresetNextPageVisible = new JoinDataComplete(
			new JoinData { JoinNumber = 17, JoinSpan = 1 },
			new JoinMetadata { Description = "Preset List — Next Page Visible (FB)", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Digital });

		[JoinName("PresetPreviousPage")]
		public JoinDataComplete PresetPreviousPage = new JoinDataComplete(
			new JoinData { JoinNumber = 18, JoinSpan = 1 },
			new JoinMetadata { Description = "Preset List — Previous Page (Press)", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Digital });

		[JoinName("PresetPreviousPageVisible")]
		public JoinDataComplete PresetPreviousPageVisible = new JoinDataComplete(
			new JoinData { JoinNumber = 18, JoinSpan = 1 },
			new JoinMetadata { Description = "Preset List — Previous Page Visible (FB)", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Digital });

		[JoinName("PresetBack")]
		public JoinDataComplete PresetBack = new JoinDataComplete(
			new JoinData { JoinNumber = 19, JoinSpan = 1 },
			new JoinMetadata { Description = "Preset List — Back (Press)", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Digital });

		[JoinName("SelectServices")]
		public JoinDataComplete SelectServices = new JoinDataComplete(
			new JoinData { JoinNumber = 21, JoinSpan = 10 },
			new JoinMetadata { Description = "Select Service 1-10 (Press, joins 21-30)", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Digital });

		[JoinName("SelectPresets")]
		public JoinDataComplete SelectPresets = new JoinDataComplete(
			new JoinData { JoinNumber = 31, JoinSpan = 10 },
			new JoinMetadata { Description = "Select Preset 1-10 (Press, joins 31-40)", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Digital });

		#endregion

		#region Analog

		[JoinName("Status")]
		public JoinDataComplete Status = new JoinDataComplete(
			new JoinData { JoinNumber = 1, JoinSpan = 1 },
			new JoinMetadata { Description = "Device Status (0=Offline, 2=Online)", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Analog });

		[JoinName("VolumeLevel")]
		public JoinDataComplete VolumeLevel = new JoinDataComplete(
			new JoinData { JoinNumber = 2, JoinSpan = 1 },
			new JoinMetadata { Description = "Volume Level 0-100 — local zone (FB/Set)", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Analog });

		[JoinName("ServicePageNumber")]
		public JoinDataComplete ServicePageNumber = new JoinDataComplete(
			new JoinData { JoinNumber = 3, JoinSpan = 1 },
			new JoinMetadata { Description = "Service List Current Page Number (1-based)", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Analog });

		[JoinName("PresetPageNumber")]
		public JoinDataComplete PresetPageNumber = new JoinDataComplete(
			new JoinData { JoinNumber = 4, JoinSpan = 1 },
			new JoinMetadata { Description = "Preset List Current Page Number (1-based)", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Analog });

		#endregion

		#region Serial

		[JoinName("DeviceName")]
		public JoinDataComplete DeviceName = new JoinDataComplete(
			new JoinData { JoinNumber = 1, JoinSpan = 1 },
			new JoinMetadata { Description = "Device Name", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });

		[JoinName("CurrentTrackName")]
		public JoinDataComplete CurrentTrackName = new JoinDataComplete(
			new JoinData { JoinNumber = 2, JoinSpan = 1 },
			new JoinMetadata { Description = "Current Track Name", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });

		[JoinName("CurrentArtist")]
		public JoinDataComplete CurrentArtist = new JoinDataComplete(
			new JoinData { JoinNumber = 3, JoinSpan = 1 },
			new JoinMetadata { Description = "Current Artist", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });

		[JoinName("CurrentAlbum")]
		public JoinDataComplete CurrentAlbum = new JoinDataComplete(
			new JoinData { JoinNumber = 4, JoinSpan = 1 },
			new JoinMetadata { Description = "Current Album", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });

		[JoinName("AlbumArtUrl")]
		public JoinDataComplete AlbumArtUrl = new JoinDataComplete(
			new JoinData { JoinNumber = 5, JoinSpan = 1 },
			new JoinMetadata { Description = "Album Art absolute URL (relative paths resolved to http://ip:port/...)", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });

		[JoinName("CurrentServicesMenu")]
		public JoinDataComplete CurrentServicesMenu = new JoinDataComplete(
			new JoinData { JoinNumber = 6, JoinSpan = 1 },
			new JoinMetadata { Description = "Current Services Menu name", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });

		[JoinName("ServiceNames")]
		public JoinDataComplete ServiceNames = new JoinDataComplete(
			new JoinData { JoinNumber = 21, JoinSpan = 10 },
			new JoinMetadata { Description = "Service Names 1-10 (serials 21-30)", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });

		[JoinName("PresetNames")]
		public JoinDataComplete PresetNames = new JoinDataComplete(
			new JoinData { JoinNumber = 31, JoinSpan = 10 },
			new JoinMetadata { Description = "Preset Names 1-10 (serials 31-40)", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });

		#endregion

		/// <summary>
		/// Constructor — pass the join offset for this device on the EISC bridge
		/// </summary>
		/// <param name="joinStart">Join offset on the EISC bridge</param>
		public BluesoundApiBridgeJoinMap(uint joinStart)
			: base(joinStart, typeof(BluesoundApiBridgeJoinMap))
		{
		}
	}
}