using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace MeowNetLoader.Core;

internal sealed class IntegrityChecker
{
	public bool HasManifest(string settingsDirectory)
	{
		try
		{
			return File.Exists(Path.Combine(settingsDirectory, "client.cache"));
		}
		catch
		{
			return false;
		}
	}

	public void ComputeAndStore(string installDir, string settingsDirectory)
	{
		try
		{
			string text = NormalizeRoot(installDir);
			List<string> list = new List<string>();
			foreach (string item in EnumerateWatchedFiles(text))
			{
				string text2 = HashFile(Path.Combine(text, item));
				if (text2 != null)
				{
					list.Add(item.Replace('\\', '/') + "|" + text2);
				}
			}
			string text3 = string.Join("\n", list);
			string text4 = Sign(text3);
			Directory.CreateDirectory(settingsDirectory);
			File.WriteAllText(Path.Combine(settingsDirectory, "client.cache"), text3 + "\nSIG:" + text4);
		}
		catch
		{
		}
	}

	public bool Verify(string installDir, string settingsDirectory, out string failedFile)
	{
		failedFile = null;
		try
		{
			string path = Path.Combine(settingsDirectory, "client.cache");
			if (!File.Exists(path))
			{
				return false;
			}
			string[] array = File.ReadAllLines(path);
			if (array.Length == 0)
			{
				return false;
			}
			string text = array[^1];
			if (!text.StartsWith("SIG:", StringComparison.Ordinal))
			{
				return false;
			}
			string b = text.Substring(4);
			string[] array2 = array.Take(array.Length - 1).ToArray();
			if (array2.Length == 0)
			{
				return false;
			}
			if (!string.Equals(Sign(string.Join("\n", array2)), b, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			string path2 = NormalizeRoot(installDir);
			string[] array3 = array2;
			foreach (string text2 in array3)
			{
				int num = text2.LastIndexOf('|');
				if (num < 0)
				{
					return false;
				}
				string text3 = text2.Substring(0, num);
				string b2 = text2.Substring(num + 1);
				string text4 = HashFile(Path.Combine(path2, text3.Replace('/', '\\')));
				if (text4 == null || !string.Equals(text4, b2, StringComparison.OrdinalIgnoreCase))
				{
					failedFile = text3.Replace('/', '\\');
					return false;
				}
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static string NormalizeRoot(string installDir)
	{
		return Path.GetFullPath(installDir).TrimEnd(new char[2]
		{
			Path.DirectorySeparatorChar,
			Path.AltDirectorySeparatorChar
		});
	}

	private static IEnumerable<string> EnumerateWatchedFiles(string root)
	{
		string[] integrityWatchedFiles = AppConstants.IntegrityWatchedFiles;
		foreach (string text in integrityWatchedFiles)
		{
			if (File.Exists(Path.Combine(root, text)))
			{
				yield return text;
			}
		}
		integrityWatchedFiles = AppConstants.IntegrityWatchedFolders;
		foreach (string path in integrityWatchedFiles)
		{
			string path2 = Path.Combine(root, path);
			if (Directory.Exists(path2))
			{
				string[] files = Directory.GetFiles(path2, "*", SearchOption.AllDirectories);
				foreach (string text2 in files)
				{
					yield return text2.Substring(root.Length).TrimStart(new char[2] { '\\', '/' });
				}
			}
		}
	}

	private static string HashFile(string path)
	{
		try
		{
			using SHA256 sHA = SHA256.Create();
			using FileStream inputStream = File.OpenRead(path);
			return ToHex(sHA.ComputeHash(inputStream));
		}
		catch
		{
			return null;
		}
	}

	private static string Sign(string data)
	{
		using HMACSHA256 hMACSHA = new HMACSHA256(Encoding.UTF8.GetBytes("b13c6a2e9d4f7081c5b3e6a09f2d84c71a5e"));
		return ToHex(hMACSHA.ComputeHash(Encoding.UTF8.GetBytes(data ?? string.Empty)));
	}

	private static string ToHex(byte[] bytes)
	{
		StringBuilder stringBuilder = new StringBuilder(bytes.Length * 2);
		foreach (byte b in bytes)
		{
			stringBuilder.Append(b.ToString("x2"));
		}
		return stringBuilder.ToString();
	}
}
