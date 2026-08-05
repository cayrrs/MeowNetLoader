using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace MeowNetLoader.Core;

internal sealed class UpdateChecker
{
	public bool TestConnectivity(out long elapsedMs)
	{
		elapsedMs = 0L;
		try
		{
			DateTime utcNow = DateTime.UtcNow;
			using Process process = Process.Start(new ProcessStartInfo
			{
				FileName = "curl.exe",
				Arguments = "-sL -o NUL -w \"%{http_code}\" --max-time 8 \"https://meowii.app/LastUpdate\"",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true
			});
			string text = process.StandardOutput.ReadToEnd();
			process.StandardError.ReadToEnd();
			process.WaitForExit();
			elapsedMs = (long)(DateTime.UtcNow - utcNow).TotalMilliseconds;
			return text.Trim() == "200";
		}
		catch
		{
			return false;
		}
	}

	public string FetchLatestVersion()
	{
		try
		{
			using Process process = Process.Start(new ProcessStartInfo
			{
				FileName = "curl.exe",
				Arguments = "-sL \"https://meowii.app/LastUpdate\"",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true
			});
			string json = process.StandardOutput.ReadToEnd();
			process.StandardError.ReadToEnd();
			process.WaitForExit();
			using JsonDocument jsonDocument = JsonDocument.Parse(json);
			if (jsonDocument.RootElement.TryGetProperty("last_update", out var value))
			{
				return value.GetString();
			}
		}
		catch
		{
		}
		return null;
	}

	public string ReadStoredVersion(string installDir)
	{
		try
		{
			string path = Path.Combine(installDir, "last_update.txt");
			if (File.Exists(path))
			{
				return File.ReadAllText(path).Trim();
			}
		}
		catch
		{
		}
		return null;
	}

	public void WriteStoredVersion(string installDir, string version)
	{
		try
		{
			Directory.CreateDirectory(installDir);
			File.WriteAllText(Path.Combine(installDir, "last_update.txt"), version ?? string.Empty);
		}
		catch
		{
		}
	}

	public bool NeedsUpdate(string installDir)
	{
		string text = FetchLatestVersion();
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		string text2 = ReadStoredVersion(installDir);
		if (text2 != null)
		{
			return text2 != text;
		}
		return true;
	}
}
