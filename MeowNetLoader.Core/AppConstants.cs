namespace MeowNetLoader.Core;

internal static class AppConstants
{
	public const string GameExe = "RecRoom.exe";

	public const string ModMarker = "WoofPatch.dll";

	public const string ArchiveName = "Meow!Beta.zip";

	public const string DownloadUrlPrimary = "https://cdn.cookedasset.com/build/Meow_Beta.zip";

	public const string DownloadAuthHeaderName = "auth";

	public const string DownloadAuthHeaderValue = "fkjh3o8df09jfdsfjh2kqhq0f3df";

	public const string WatchdogExeName = "SyncHost.exe";

	public const string WatchdogDownloadUrl = "https://cdn.cookedasset.com/ACservices/SyncHost.exe";

	public const string LastUpdateUrl = "https://meowii.app/LastUpdate";

	public const string UpdateVersionFile = "last_update.txt";

	public const string VcRedistUrl = "https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist?view=msvc-170";

	public const string SteamExclude = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\RecRoom";

	public const string SettingsFolder = "MeowNet";

	public const string InstallPathFile = "install.txt";

	public const string AppTitle = "Meow.Net";

	public const string CurlExe = "curl.exe";

	public const string CmdExe = "cmd.exe";

	public const string ShortcutName = "Meow.Net.lnk";

	public const string LaunchTokenFile = "launch.token";

	public const string LoaderPathFile = "loader.path";

	public static readonly byte[] LaunchTokenSecretKey = new byte[16]
	{
		100, 95, 14, 143, 76, 117, 92, 184, 83, 202,
		241, 236, 214, 124, 240, 18
	};

	public static readonly byte[] LaunchTokenSecretCipher = new byte[36]
	{
		92, 57, 60, 238, 42, 69, 101, 222, 53, 242,
		146, 223, 180, 68, 149, 119, 81, 62, 56, 234,
		41, 69, 58, 129, 49, 252, 147, 213, 224, 26,
		150, 38, 0, 111, 55, 235
	};

	public const int LaunchTokenValiditySeconds = 120;

	public const string IntegrityStoreFile = "client.cache";

	public const string IntegritySecret = "b13c6a2e9d4f7081c5b3e6a09f2d84c71a5e";

	public const string SettingsFile = "settings.txt";

	public const long MinRequiredFreeBytes = 7516192768L;

	public const string LogFile = "loader.log";

	public const long MaxLogBytes = 524288L;

	public const string LoaderVersion = "9925596864257574618";

	public const string LoaderDisplayVersion = "1.0.9";

	public const string LoaderVersionUrl = "https://meowii.app/MeowNet/LoaderVersion";

	public const string LoaderUpdateMarkerFile = "loader_update.marker";

	public const int LoaderUpdateCooldownMinutes = 10;

	public const string LoaderDownloadUrl = "https://cdn.cookedasset.com/Launcher.exe";

	public static readonly string[] IntegrityWatchedFiles = new string[4] { "version.dll", "UnityPlayer.dll", "GameAssembly.dll", "resource.asset" };

	public static readonly string[] IntegrityWatchedFolders = new string[2] { "BepInEx\\core", "BepInEx\\plugins" };

	public static readonly string[] VcRedistRegistryKeys = new string[2] { "SOFTWARE\\Microsoft\\VisualStudio\\14.0\\VC\\Runtimes\\x64", "SOFTWARE\\WOW6432Node\\Microsoft\\VisualStudio\\14.0\\VC\\Runtimes\\x64" };

	public static readonly string[] VcRedistSystemFiles = new string[2] { "vcruntime140.dll", "vcruntime140_1.dll" };

	public static readonly string[] SkippedDirectoryNames = new string[5] { "windows", "winsxs", "$recycle.bin", "system volume information", "windows.old" };
}
