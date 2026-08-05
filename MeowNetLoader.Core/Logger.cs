using System;
using System.IO;

namespace MeowNetLoader.Core;

internal sealed class Logger
{
	private readonly string logPath;

	private readonly object sync = new object();

	public Logger(string settingsDirectory)
	{
		logPath = Path.Combine(settingsDirectory, "loader.log");
		ClearForNewSession();
	}

	private void ClearForNewSession()
	{
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(logPath));
			File.WriteAllText(logPath, string.Empty);
		}
		catch
		{
		}
	}

	public void Log(string message)
	{
		try
		{
			lock (sync)
			{
				Directory.CreateDirectory(Path.GetDirectoryName(logPath));
				Trim();
				File.AppendAllText(logPath, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "  " + message + Environment.NewLine);
			}
		}
		catch
		{
		}
	}

	public string ReadTail(int maxLines)
	{
		try
		{
			lock (sync)
			{
				if (!File.Exists(logPath))
				{
					return "(no log yet)";
				}
				string[] array = File.ReadAllLines(logPath);
				int num = Math.Max(0, array.Length - maxLines);
				return string.Join(Environment.NewLine, array, num, array.Length - num);
			}
		}
		catch
		{
			return "(unable to read log)";
		}
	}

	private void Trim()
	{
		try
		{
			if (File.Exists(logPath) && new FileInfo(logPath).Length > 524288)
			{
				string[] array = File.ReadAllLines(logPath);
				int num = array.Length / 2;
				if (num < 1)
				{
					num = 1;
				}
				if (num < array.Length)
				{
					string[] array2 = new string[num];
					Array.Copy(array, array.Length - num, array2, 0, num);
					File.WriteAllLines(logPath, array2);
				}
			}
		}
		catch
		{
		}
	}
}
