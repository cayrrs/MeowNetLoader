using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MeowNetLoader.Services;

internal sealed class ShortcutService
{
	[ComImport]
	[Guid("00021401-0000-0000-C000-000000000046")]
	[ClassInterface(ClassInterfaceType.None)]
	private class ShellLink
	{
	}

	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("000214F9-0000-0000-C000-000000000046")]
	private interface IShellLinkW
	{
		void GetPath([Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out nint pfd, int fFlags);

		void GetIDList(out nint ppidl);

		void SetIDList(nint pidl);

		void GetDescription([Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);

		void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);

		void GetWorkingDirectory([Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);

		void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);

		void GetArguments([Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);

		void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);

		void GetHotkey(out short pwHotkey);

		void SetHotkey(short wHotkey);

		void GetShowCmd(out int piShowCmd);

		void SetShowCmd(int iShowCmd);

		void GetIconLocation([Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);

		void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);

		void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);

		void Resolve(nint hwnd, int fFlags);

		void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
	}

	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99")]
	private interface IPropertyStore
	{
		void GetCount(out uint cProps);

		void GetAt(uint iProp, out PropertyKey pkey);

		void GetValue(ref PropertyKey pkey, out PropVariant pv);

		void SetValue(ref PropertyKey pkey, ref PropVariant pv);

		void Commit();
	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	private struct PropertyKey
	{
		private Guid fmtid;

		private uint pid;

		public static PropertyKey AppUserModel_ID = new PropertyKey
		{
			fmtid = new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"),
			pid = 5u
		};
	}

	[StructLayout(LayoutKind.Explicit)]
	private struct PropVariant
	{
		[FieldOffset(0)]
		private ushort vt;

		[FieldOffset(8)]
		private nint ptr;

		public void SetString(string value)
		{
			vt = 31;
			ptr = Marshal.StringToCoTaskMemUni(value);
		}
	}

	private readonly string appDir;

	private const string RegistryPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\MeowNetLoader";

	private const string AppUserModelId = "MeowNet.MeowNetLoader.Launcher";

	public ShortcutService(string appDir)
	{
		this.appDir = appDir;
	}

	public bool DesktopShortcutExists()
	{
		try
		{
			return File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Meow.Net.lnk"));
		}
		catch
		{
			return false;
		}
	}

	public void Create()
	{
		try
		{
			string shortcutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Meow.Net.lnk");
			CreateShellLink(shortcutPath);
			string shortcutPath2 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Meow.Net.lnk");
			CreateShellLink(shortcutPath2);
			RegisterInAddRemovePrograms();
		}
		catch (Exception)
		{
		}
	}

	public void Remove()
	{
		try
		{
			string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Meow.Net.lnk");
			if (File.Exists(path))
			{
				File.Delete(path);
			}
			string path2 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Meow.Net.lnk");
			if (File.Exists(path2))
			{
				File.Delete(path2);
			}
			UnregisterFromAddRemovePrograms();
		}
		catch
		{
		}
	}

	private void CreateShellLink(string shortcutPath)
	{
		IShellLinkW obj = (IShellLinkW)new ShellLink();
		obj.SetPath(Application.ExecutablePath);
		obj.SetWorkingDirectory(appDir);
		obj.SetDescription("Meow.Net Launcher");
		obj.SetIconLocation(Application.ExecutablePath, 0);
		IPropertyStore obj2 = (IPropertyStore)obj;
		PropVariant pv = default(PropVariant);
		pv.SetString("MeowNet.MeowNetLoader.Launcher");
		PropertyKey pkey = PropertyKey.AppUserModel_ID;
		obj2.SetValue(ref pkey, ref pv);
		obj2.Commit();
		((IPersistFile)obj).Save(shortcutPath, fRemember: true);
	}

	private void RegisterInAddRemovePrograms()
	{
		using RegistryKey registryKey = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\MeowNetLoader");
		if (registryKey != null)
		{
			registryKey.SetValue("DisplayName", "MeowNetLoader");
			registryKey.SetValue("DisplayIcon", "\"" + Application.ExecutablePath + "\",0");
			registryKey.SetValue("DisplayVersion", "1.0.0");
			registryKey.SetValue("Publisher", "MeowNet");
			registryKey.SetValue("UninstallString", "\"" + Application.ExecutablePath + "\" --uninstall");
			long num = 0L;
			if (File.Exists(Application.ExecutablePath))
			{
				num = new FileInfo(Application.ExecutablePath).Length / 1024;
			}
			registryKey.SetValue("EstimatedSize", (int)num, RegistryValueKind.DWord);
			registryKey.SetValue("NoModify", 1, RegistryValueKind.DWord);
			registryKey.SetValue("NoRepair", 1, RegistryValueKind.DWord);
		}
	}

	private void UnregisterFromAddRemovePrograms()
	{
		Registry.CurrentUser.DeleteSubKeyTree("Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\MeowNetLoader", throwOnMissingSubKey: false);
	}
}
