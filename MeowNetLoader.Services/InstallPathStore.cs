using System;
using System.IO;

namespace MeowNetLoader.Services;

internal sealed class InstallPathStore
{
	public string SettingsDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MeowNet");

	public void Save(string folder)
	{
		try
		{
			Directory.CreateDirectory(SettingsDirectory);
			File.WriteAllText(Path.Combine(SettingsDirectory, "install.txt"), folder);
		}
		catch
		{
		}
	}

	public string Load()
	{
		try
		{
			string path = Path.Combine(SettingsDirectory, "install.txt");
			if (File.Exists(path))
			{
				string text = File.ReadAllText(path).Trim();
				if (text.Length > 0 && Directory.Exists(text))
				{
					return text;
				}
			}
		}
		catch
		{
		}
		return null;
	}

	public void Clear()
	{
		try
		{
			File.Delete(Path.Combine(SettingsDirectory, "install.txt"));
		}
		catch
		{
		}
	}
}
