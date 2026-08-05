using System.Diagnostics;
using System.IO;

namespace MeowNetLoader.Core;

internal sealed class Downloader
{
	public long ContentLength { get; private set; }

	public long ProbeContentLength(string url, string header = null)
	{
		ContentLength = 0L;
		try
		{
			ProcessStartInfo processStartInfo = new ProcessStartInfo();
			processStartInfo.FileName = "curl.exe";
			processStartInfo.Arguments = "-sLI" + HeaderArg(header) + " \"" + url + "\"";
			processStartInfo.UseShellExecute = false;
			processStartInfo.RedirectStandardOutput = true;
			processStartInfo.RedirectStandardError = true;
			processStartInfo.CreateNoWindow = true;
			using Process process = Process.Start(processStartInfo);
			string text = process.StandardOutput.ReadToEnd();
			process.StandardError.ReadToEnd();
			process.WaitForExit();
			string[] array = text.Split('\n');
			for (int i = 0; i < array.Length; i++)
			{
				string text2 = array[i].Trim();
				if (text2.Length > 15 && text2.Substring(0, 15).ToLowerInvariant() == "content-length:" && long.TryParse(text2.Substring(15).Trim(), out var result))
				{
					ContentLength = result;
				}
			}
		}
		catch
		{
		}
		return ContentLength;
	}

	public bool Download(string url, string destination, string header = null)
	{
		try
		{
			long num = 0L;
			try
			{
				if (File.Exists(destination))
				{
					num = new FileInfo(destination).Length;
				}
			}
			catch
			{
			}
			if (ContentLength > 0 && num == ContentLength)
			{
				return true;
			}
			ProcessStartInfo processStartInfo = new ProcessStartInfo();
			processStartInfo.FileName = "curl.exe";
			processStartInfo.Arguments = "-L -C - --fail" + HeaderArg(header) + " -o \"" + destination + "\" \"" + url + "\"";
			processStartInfo.UseShellExecute = false;
			processStartInfo.CreateNoWindow = true;
			processStartInfo.RedirectStandardOutput = true;
			processStartInfo.RedirectStandardError = true;
			using (Process process = Process.Start(processStartInfo))
			{
				process.StandardError.ReadToEnd();
				process.StandardOutput.ReadToEnd();
				process.WaitForExit();
			}
			long num2 = 0L;
			try
			{
				num2 = new FileInfo(destination).Length;
			}
			catch
			{
			}
			if (ContentLength > 0 && num2 == ContentLength)
			{
				return true;
			}
			if (ContentLength <= 0 && num2 > 0)
			{
				return true;
			}
			return false;
		}
		catch
		{
			return false;
		}
	}

	private static string HeaderArg(string header)
	{
		if (!string.IsNullOrEmpty(header))
		{
			return " -H \"" + header + "\"";
		}
		return string.Empty;
	}
}
