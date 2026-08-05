using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace MeowNetLoader.Core;

internal sealed class DependencyChecker
{
	private List<string> missingNames = new List<string>();

	private List<string> missingUrls = new List<string>();

	public List<string> MissingNames => missingNames;

	public List<string> MissingUrls => missingUrls;

	public bool Check()
	{
		missingNames = new List<string>();
		missingUrls = new List<string>();
		if (!HasVcRedist())
		{
			missingNames.Add("Visual C++ Redistributable");
			missingUrls.Add("https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist?view=msvc-170");
		}
		return missingNames.Count == 0;
	}

	public string FormatList()
	{
		return string.Join(",    ", missingNames.ToArray());
	}

	private static bool HasVcRedist()
	{
		try
		{
			string sys = Environment.GetFolderPath(Environment.SpecialFolder.System);
			if (AppConstants.VcRedistSystemFiles.All((string f) => File.Exists(Path.Combine(sys, f))))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			string[] vcRedistRegistryKeys = AppConstants.VcRedistRegistryKeys;
			foreach (string name in vcRedistRegistryKeys)
			{
				using RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(name);
				if (registryKey != null)
				{
					object value = registryKey.GetValue("Installed");
					if (value != null && Convert.ToInt32(value) == 1)
					{
						return true;
					}
				}
			}
		}
		catch
		{
		}
		return false;
	}
}
