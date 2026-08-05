using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using MeowNetLoader.Services;

namespace MeowNetLoader.Core;

internal sealed class GameFinder
{
	private readonly string appDir;

	private readonly InstallPathStore installPathStore;

	private string cachedExe;

	public event Action FullScanStarting;

	public GameFinder(string appDir, InstallPathStore installPathStore)
	{
		this.appDir = appDir;
		this.installPathStore = installPathStore;
	}

	public string Find()
	{
		return Find(CancellationToken.None);
	}

	public string Find(CancellationToken cancellationToken)
	{
		if (!string.IsNullOrEmpty(cachedExe) && File.Exists(cachedExe) && !IsSteamExcluded(cachedExe))
		{
			return cachedExe;
		}
		cachedExe = Locate(cancellationToken);
		return cachedExe;
	}

	public void Invalidate()
	{
		cachedExe = null;
	}

	private string Locate(CancellationToken cancellationToken)
	{
		string text = FindUnder(installPathStore.Load());
		if (text != null)
		{
			return text;
		}
		try
		{
			string text2 = Path.Combine(appDir, "RecRoom.exe");
			if (File.Exists(text2) && !IsSteamExcluded(text2))
			{
				return text2;
			}
			string text3 = Directory.GetFiles(appDir, "RecRoom.exe", SearchOption.AllDirectories).FirstOrDefault((string f) => !IsSteamExcluded(f));
			if (text3 != null)
			{
				return text3;
			}
		}
		catch
		{
		}
		this.FullScanStarting?.Invoke();
		return SearchPc(cancellationToken);
	}

	private string FindUnder(string folder)
	{
		if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
		{
			return null;
		}
		try
		{
			string text = Path.Combine(folder, "RecRoom.exe");
			if (File.Exists(text))
			{
				return text;
			}
			string[] files = Directory.GetFiles(folder, "RecRoom.exe", SearchOption.AllDirectories);
			if (files.Length != 0)
			{
				return files[0];
			}
		}
		catch
		{
		}
		return null;
	}

	private string SearchPc(CancellationToken cancellationToken)
	{
		try
		{
			Stack<string> stack = new Stack<string>();
			DriveInfo[] drives = DriveInfo.GetDrives();
			foreach (DriveInfo driveInfo in drives)
			{
				try
				{
					if (driveInfo.DriveType == DriveType.Fixed && driveInfo.IsReady)
					{
						stack.Push(driveInfo.RootDirectory.FullName);
					}
				}
				catch
				{
				}
			}
			while (stack.Count > 0)
			{
				if (cancellationToken.IsCancellationRequested)
				{
					return null;
				}
				string text = stack.Pop();
				if (SkipDir(text))
				{
					continue;
				}
				try
				{
					string text2 = Path.Combine(text, "RecRoom.exe");
					if (File.Exists(text2) && HasMeowNetMod(text))
					{
						return text2;
					}
				}
				catch
				{
				}
				try
				{
					string[] directories = Directory.GetDirectories(text);
					foreach (string text3 in directories)
					{
						try
						{
							if ((File.GetAttributes(text3) & FileAttributes.ReparsePoint) != FileAttributes.None)
							{
								continue;
							}
						}
						catch
						{
							continue;
						}
						stack.Push(text3);
					}
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static bool SkipDir(string dir)
	{
		try
		{
			string text = dir.ToLowerInvariant().TrimEnd('\\');
			string text2 = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\RecRoom".ToLowerInvariant();
			if (text == text2 || text.StartsWith(text2 + "\\"))
			{
				return true;
			}
			string fileName = Path.GetFileName(text);
			if (Array.IndexOf(AppConstants.SkippedDirectoryNames, fileName) >= 0)
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static bool IsSteamExcluded(string path)
	{
		try
		{
			string text = path.ToLowerInvariant();
			string text2 = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\RecRoom".ToLowerInvariant();
			return text == text2 || text.StartsWith(text2 + "\\");
		}
		catch
		{
			return false;
		}
	}

	private static bool HasMeowNetMod(string gameDir)
	{
		try
		{
			if (File.Exists(Path.Combine(gameDir, "WoofPatch.dll")))
			{
				return true;
			}
			string path = Path.Combine(gameDir, "BepInEx");
			if (Directory.Exists(path) && Directory.GetFiles(path, "WoofPatch.dll", SearchOption.AllDirectories).Length != 0)
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}
}
