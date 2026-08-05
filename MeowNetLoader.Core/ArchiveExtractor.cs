using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace MeowNetLoader.Core;

internal sealed class ArchiveExtractor
{
	public bool Extract(string archive, string destination)
	{
		try
		{
			using (ZipArchive zipArchive = ZipFile.OpenRead(archive))
			{
				string fullPath = Path.GetFullPath(destination);
				string text = FindWrapperPrefix(zipArchive);
				foreach (ZipArchiveEntry entry in zipArchive.Entries)
				{
					string text2 = entry.FullName.Replace('\\', '/');
					if (text != null && text2.StartsWith(text, StringComparison.OrdinalIgnoreCase))
					{
						text2 = text2.Substring(text.Length);
					}
					if (text2.Length == 0)
					{
						continue;
					}
					string fullPath2 = Path.GetFullPath(Path.Combine(destination, text2));
					if (!fullPath2.StartsWith(fullPath, StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}
					if (text2.EndsWith("/") || string.IsNullOrEmpty(entry.Name))
					{
						Directory.CreateDirectory(fullPath2);
						continue;
					}
					string directoryName = Path.GetDirectoryName(fullPath2);
					if (!string.IsNullOrEmpty(directoryName))
					{
						Directory.CreateDirectory(directoryName);
					}
					entry.ExtractToFile(fullPath2, overwrite: true);
				}
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static string FindWrapperPrefix(ZipArchive zip)
	{
		string[] array = (from e in zip.Entries
			select e.FullName.Replace('\\', '/') into n
			where n.Length > 0
			select n.Split('/')[0]).Distinct().ToArray();
		if (array.Length != 1)
		{
			return null;
		}
		string root = array[0];
		if (string.IsNullOrEmpty(root))
		{
			return null;
		}
		if (!zip.Entries.Any((ZipArchiveEntry e) => e.FullName.Replace('\\', '/').StartsWith(root + "/", StringComparison.OrdinalIgnoreCase)))
		{
			return null;
		}
		return root + "/";
	}
}
