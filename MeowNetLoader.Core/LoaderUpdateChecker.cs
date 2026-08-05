using System.Diagnostics;
using System.Text.Json;

namespace MeowNetLoader.Core;

internal sealed class LoaderUpdateChecker
{
	public string FetchLatestVersion()
	{
		try
		{
			using Process process = Process.Start(new ProcessStartInfo
			{
				FileName = "curl.exe",
				Arguments = "-sL --max-time 8 \"https://meowii.app/MeowNet/LoaderVersion\"",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true
			});
			string json = process.StandardOutput.ReadToEnd();
			process.StandardError.ReadToEnd();
			process.WaitForExit();
			using JsonDocument jsonDocument = JsonDocument.Parse(json);
			if (jsonDocument.RootElement.TryGetProperty("version", out var value))
			{
				return value.GetString();
			}
		}
		catch
		{
		}
		return null;
	}

	public bool NeedsUpdate()
	{
		string text = FetchLatestVersion();
		if (!string.IsNullOrEmpty(text))
		{
			return text != "9925596864257574618";
		}
		return false;
	}
}
