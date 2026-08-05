using System.IO;

namespace MeowNetLoader.Core;

internal sealed class SettingsStore
{
	private readonly string settingsDirectory;

	public SettingsStore(string settingsDirectory)
	{
		this.settingsDirectory = settingsDirectory;
	}

	public AppSettings Load()
	{
		AppSettings appSettings = new AppSettings();
		try
		{
			string path = Path.Combine(settingsDirectory, "settings.txt");
			if (!File.Exists(path))
			{
				return appSettings;
			}
			string[] array = File.ReadAllLines(path);
			foreach (string text in array)
			{
				int num = text.IndexOf('=');
				if (num >= 0)
				{
					string text2 = text.Substring(0, num).Trim();
					string text3 = text.Substring(num + 1).Trim();
					switch (text2)
					{
					case "AutoCloseOnLaunch":
						appSettings.AutoCloseOnLaunch = text3 == "1";
						break;
					case "MinimizeToTray":
						appSettings.MinimizeToTray = text3 == "1";
						break;
					case "DefaultPlayMode":
						appSettings.DefaultPlayMode = text3;
						break;
					}
				}
			}
		}
		catch
		{
		}
		return appSettings;
	}

	public void Save(AppSettings settings)
	{
		try
		{
			Directory.CreateDirectory(settingsDirectory);
			string[] contents = new string[3]
			{
				"AutoCloseOnLaunch=" + (settings.AutoCloseOnLaunch ? "1" : "0"),
				"MinimizeToTray=" + (settings.MinimizeToTray ? "1" : "0"),
				"DefaultPlayMode=" + settings.DefaultPlayMode
			};
			File.WriteAllLines(Path.Combine(settingsDirectory, "settings.txt"), contents);
		}
		catch
		{
		}
	}
}
