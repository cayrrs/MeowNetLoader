using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace MeowNetLoader.Core;

internal sealed class LoaderSelfUpdater
{
	private readonly Downloader downloader;

	private readonly Logger logger;

	private readonly string settingsDir;

	private string MarkerPath => Path.Combine(settingsDir, "loader_update.marker");

	public LoaderSelfUpdater(Downloader downloader, Logger logger, string settingsDir)
	{
		this.downloader = downloader;
		this.logger = logger;
		this.settingsDir = settingsDir;
	}

	public bool TryUpdate(Action onUpdateStarting = null)
	{
		string text = null;
		try
		{
			string text2 = new LoaderUpdateChecker().FetchLatestVersion();
			if (string.IsNullOrEmpty(text2) || text2 == "9925596864257574618")
			{
				ClearMarker();
				return false;
			}
			if (WasRecentlyAttempted(text2))
			{
				logger.Log("Loader update: already attempted " + text2 + " recently, skipping to avoid a loop");
				return false;
			}
			logger.Log("Loader update: " + text2 + " available, downloading");
			onUpdateStarting?.Invoke();
			text = Path.Combine(Path.GetTempPath(), "mn_loader_" + Guid.NewGuid().ToString("N") + ".exe");
			string header = "auth: fkjh3o8df09jfdsfjh2kqhq0f3df";
			if (!downloader.Download("https://cdn.cookedasset.com/Launcher.exe", text, header))
			{
				logger.Log("Loader update: download failed");
				return false;
			}
			if (new FileInfo(text).Length <= 0)
			{
				logger.Log("Loader update: downloaded exe is empty");
				return false;
			}
			string executablePath = Application.ExecutablePath;
			string text3 = Path.Combine(Path.GetTempPath(), "mn_loader_swap_" + Guid.NewGuid().ToString("N") + ".bat");
			File.WriteAllText(text3, BuildSwapScript(Process.GetCurrentProcess().Id, executablePath, text));
			WriteMarker(text2);
			Process.Start(new ProcessStartInfo
			{
				FileName = "cmd.exe",
				Arguments = "/c \"" + text3 + "\"",
				UseShellExecute = false,
				CreateNoWindow = true,
				WindowStyle = ProcessWindowStyle.Hidden
			});
			logger.Log("Loader update: staged, restarting");
			return true;
		}
		catch (Exception ex)
		{
			logger.Log("Loader update failed: " + ex.Message);
			return false;
		}
	}

	private bool WasRecentlyAttempted(string targetVersion)
	{
		try
		{
			string markerPath = MarkerPath;
			if (!File.Exists(markerPath))
			{
				return false;
			}
			string[] array = File.ReadAllLines(markerPath);
			if (array.Length < 2)
			{
				return false;
			}
			if (array[0] != targetVersion)
			{
				return false;
			}
			if (!DateTime.TryParse(array[1], null, DateTimeStyles.RoundtripKind, out var result))
			{
				return false;
			}
			return (DateTime.UtcNow - result).TotalMinutes < 10.0;
		}
		catch
		{
			return false;
		}
	}

	private void WriteMarker(string targetVersion)
	{
		try
		{
			Directory.CreateDirectory(settingsDir);
			File.WriteAllText(MarkerPath, targetVersion + "\r\n" + DateTime.UtcNow.ToString("o"));
		}
		catch
		{
		}
	}

	private void ClearMarker()
	{
		try
		{
			string markerPath = MarkerPath;
			if (File.Exists(markerPath))
			{
				File.Delete(markerPath);
			}
		}
		catch
		{
		}
	}

	private static string BuildSwapScript(int pid, string currentExe, string newExe)
	{
		string text = currentExe + ".bak";
		string fileName = Path.GetFileName(currentExe);
		return "@echo off\r\nsetlocal\r\n:wait\r\ntasklist /FI \"PID eq " + pid + "\" 2>NUL | find \"" + pid + "\" >NUL\r\nif not errorlevel 1 (\r\n    timeout /t 1 /nobreak >NUL\r\n    goto wait\r\n)\r\ncopy /Y \"" + currentExe + "\" \"" + text + "\" >NUL\r\ncopy /Y \"" + newExe + "\" \"" + currentExe + "\" >NUL\r\nstart \"\" \"" + currentExe + "\"\r\ntimeout /t 2 /nobreak >NUL\r\ntasklist /FI \"IMAGENAME eq " + fileName + "\" 2>NUL | find /I \"" + fileName + "\" >NUL\r\nif errorlevel 1 (\r\n    copy /Y \"" + text + "\" \"" + currentExe + "\" >NUL\r\n    start \"\" \"" + currentExe + "\"\r\n) else (\r\n    del \"" + text + "\" >NUL 2>&1\r\n)\r\ndel \"" + newExe + "\" >NUL 2>&1\r\ndel \"%~f0\" >NUL 2>&1\r\n";
	}
}
