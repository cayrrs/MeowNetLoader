using System;
using System.Reflection;
using System.Windows.Forms;
using MeowNetLoader.Forms;
using MeowNetLoader.Services;
using Microsoft.Win32;

namespace MeowNetLoader;

[Obfuscation(Exclude = true, ApplyToMembers = true)]
internal static class Program
{
	[STAThread]
	private static void Main(string[] args)
	{
		SystemEvents.SessionEnding += OnSessionEnding;
		if (args != null && Array.IndexOf(args, "--uninstall") >= 0)
		{
			try
			{
				new ShortcutService(AppDomain.CurrentDomain.BaseDirectory).Remove();
				return;
			}
			catch
			{
				return;
			}
		}
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(defaultValue: false);
		Application.Run(new MainForm(args != null && Array.IndexOf(args, "--blocked") >= 0));
	}

	private static void OnSessionEnding(object sender, SessionEndingEventArgs e)
	{
		Environment.Exit(0);
	}
}
