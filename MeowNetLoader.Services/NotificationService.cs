using System;
using System.Windows.Forms;

namespace MeowNetLoader.Services;

internal sealed class NotificationService : IDisposable
{
	private readonly Form owner;

	private NotifyIcon trayIcon;

	private bool disposed;

	public NotificationService(Form owner)
	{
		this.owner = owner;
	}

	public void ShowToast(string title, string text)
	{
		try
		{
			if (trayIcon == null)
			{
				trayIcon = new NotifyIcon
				{
					Icon = owner.Icon,
					Text = "Meow.Net",
					Visible = true
				};
			}
			trayIcon.BalloonTipIcon = ToolTipIcon.Info;
			trayIcon.BalloonTipTitle = title;
			trayIcon.BalloonTipText = text;
			trayIcon.ShowBalloonTip(6000);
		}
		catch
		{
		}
	}

	public void ShowDownloadComplete()
	{
		try
		{
			if (owner.IsHandleCreated)
			{
				owner.BeginInvoke(delegate
				{
					ShowToast("Meow.Net", "Download complete | unpacking now…");
				});
			}
		}
		catch
		{
		}
	}

	public void Dispose()
	{
		if (disposed)
		{
			return;
		}
		disposed = true;
		try
		{
			if (trayIcon != null)
			{
				trayIcon.Visible = false;
				trayIcon.Dispose();
				trayIcon = null;
			}
		}
		catch
		{
		}
	}
}
