using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MeowNetLoader.UI;

internal sealed class SettingsPanel : Panel
{
	private const int EdgeMargin = 28;

	private const int PanelWidth = 860;

	private const int CardHeight = 410;

	private const int ContentTop = 105;

	public Action BackClicked;

	public Action ChangeDirectoryClicked;

	public Action OpenInstallDirectoryClicked;

	public Action CheckUpdatesClicked;

	public Action CopyDiagnosticsClicked;

	public Action TestConnectionClicked;

	public Action<bool> AutoCloseChanged;

	public Action<bool> MinimizeToTrayChanged;

	public Action<string> DefaultModeChanged;

	public Action DesktopShortcutClicked;

	private readonly Label gameDirValue;

	private readonly ToggleSwitch autoCloseToggle;

	private readonly ToggleSwitch minimizeTrayToggle;

	private readonly PillButton chipAsk;

	private readonly PillButton chipScreen;

	private readonly PillButton chipVr;

	private readonly PillButton shortcutButton;

	public SettingsPanel()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		PillButton pillButton = MakeButton("←  Back", 90, new Point(28, 12));
		pillButton.Clicked = delegate
		{
			BackClicked?.Invoke();
		};
		base.Controls.Add(pillButton);
		PillButton pillButton2 = MakeButton("Copy Support Info", 190, new Point(128, 12));
		pillButton2.Clicked = delegate
		{
			CopyDiagnosticsClicked?.Invoke();
		};
		base.Controls.Add(pillButton2);
		PillButton pillButton3 = MakeButton("Test Connection", 150, new Point(328, 12));
		pillButton3.Clicked = delegate
		{
			TestConnectionClicked?.Invoke();
		};
		base.Controls.Add(pillButton3);
		PillButton pillButton4 = MakeButton("Check For Updates Now", 200, new Point(632, 12));
		pillButton4.Clicked = delegate
		{
			CheckUpdatesClicked?.Invoke();
		};
		base.Controls.Add(pillButton4);
		AddSectionLabel("GAME DIRECTORY", 60);
		gameDirValue = AddValueLabel(82, 570);
		PillButton pillButton5 = MakeButton("Open Folder", 110, new Point(614, 78));
		pillButton5.Clicked = delegate
		{
			OpenInstallDirectoryClicked?.Invoke();
		};
		base.Controls.Add(pillButton5);
		PillButton pillButton6 = MakeButton("Change", 100, new Point(732, 78));
		pillButton6.Clicked = delegate
		{
			ChangeDirectoryClicked?.Invoke();
		};
		base.Controls.Add(pillButton6);
		AddRowLabels("Auto Close On Launch", "Skip the confirmation screen after the game opens", 140);
		autoCloseToggle = MakeToggle(new Point(780, 143));
		autoCloseToggle.Changed = delegate(bool on)
		{
			AutoCloseChanged?.Invoke(on);
		};
		base.Controls.Add(autoCloseToggle);
		AddRowLabels("Minimize To Tray", "Keep the launcher running in the background instead of closing", 182);
		minimizeTrayToggle = MakeToggle(new Point(780, 185));
		minimizeTrayToggle.Changed = delegate(bool on)
		{
			MinimizeToTrayChanged?.Invoke(on);
		};
		base.Controls.Add(minimizeTrayToggle);
		AddRowLabels("Default Play Mode", "Skip the menu and launch straight into this mode", 224);
		int y = 227;
		int num = 70;
		int num2 = 832 - (num * 3 + 16);
		chipAsk = MakeChip("Ask", new Point(num2, y), num);
		chipScreen = MakeChip("Screen", new Point(num2 + num + 8, y), num);
		chipVr = MakeChip("VR", new Point(num2 + (num + 8) * 2, y), num);
		chipAsk.Clicked = delegate
		{
			DefaultModeChanged?.Invoke("Ask");
		};
		chipScreen.Clicked = delegate
		{
			DefaultModeChanged?.Invoke("Screen");
		};
		chipVr.Clicked = delegate
		{
			DefaultModeChanged?.Invoke("VR");
		};
		base.Controls.Add(chipAsk);
		base.Controls.Add(chipScreen);
		base.Controls.Add(chipVr);
		AddRowLabels("Desktop Shortcut", "Quick access from your desktop", 266);
		shortcutButton = MakeButton("Create", 110, new Point(722, 269));
		shortcutButton.Clicked = delegate
		{
			DesktopShortcutClicked?.Invoke();
		};
		base.Controls.Add(shortcutButton);
	}

	public void SetGameDirectory(string dir)
	{
		gameDirValue.Text = dir ?? "Not installed yet";
	}

	public void SetAutoClose(bool on)
	{
		autoCloseToggle.SetChecked(on);
	}

	public void SetMinimizeToTray(bool on)
	{
		minimizeTrayToggle.SetChecked(on);
	}

	public void SetDefaultMode(string mode)
	{
		chipAsk.Primary = mode == "Ask";
		chipScreen.Primary = mode == "Screen";
		chipVr.Primary = mode == "VR";
		chipAsk.Invalidate();
		chipScreen.Invalidate();
		chipVr.Invalidate();
	}

	public void SetDesktopShortcutExists(bool exists)
	{
		shortcutButton.Text = (exists ? "Remove" : "Create");
		shortcutButton.Invalidate();
	}

	private static Color GradientAt(int localY)
	{
		return Theme.Lerp(Theme.RedTop, Theme.RedBot, (float)(105 + localY) / 410f);
	}

	private static PillButton MakeButton(string text, int width, Point location)
	{
		return new PillButton
		{
			Primary = false,
			Text = text,
			BackColor = GradientAt(location.Y),
			Size = new Size(width, 32),
			Location = location
		};
	}

	private static PillButton MakeChip(string text, Point location, int width)
	{
		return new PillButton
		{
			Primary = false,
			Text = text,
			BackColor = GradientAt(location.Y),
			Size = new Size(width, 30),
			Location = location
		};
	}

	private static ToggleSwitch MakeToggle(Point location)
	{
		return new ToggleSwitch
		{
			Location = location,
			GradientTop = 105 + location.Y,
			GradientSpan = 410
		};
	}

	private void AddSectionLabel(string text, int y)
	{
		Label value = new Label
		{
			Text = text,
			AutoSize = false,
			BackColor = Color.Transparent,
			ForeColor = Color.FromArgb(200, 184, 186),
			Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
			Location = new Point(28, y),
			Size = new Size(400, 20)
		};
		base.Controls.Add(value);
	}

	private Label AddValueLabel(int y, int width)
	{
		Label label = new Label
		{
			AutoSize = false,
			BackColor = Color.Transparent,
			ForeColor = Color.White,
			Font = new Font("Segoe UI", 11f, FontStyle.Regular),
			Location = new Point(28, y),
			Size = new Size(width, 24)
		};
		base.Controls.Add(label);
		return label;
	}

	private void AddRowLabels(string title, string desc, int y)
	{
		Label value = new Label
		{
			Text = title,
			AutoSize = false,
			BackColor = Color.Transparent,
			ForeColor = Color.White,
			Font = new Font("Segoe UI", 11f, FontStyle.Bold),
			Location = new Point(28, y),
			Size = new Size(560, 22)
		};
		Label value2 = new Label
		{
			Text = desc,
			AutoSize = false,
			BackColor = Color.Transparent,
			ForeColor = Color.FromArgb(216, 196, 198),
			Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
			Location = new Point(28, y + 20),
			Size = new Size(560, 18)
		};
		base.Controls.Add(value);
		base.Controls.Add(value2);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		using (LinearGradientBrush brush = new LinearGradientBrush(new Rectangle(0, -105, base.Width, 410), Theme.RedTop, Theme.RedBot, LinearGradientMode.Vertical))
		{
			graphics.FillRectangle(brush, base.ClientRectangle);
		}
		using Pen pen = new Pen(Theme.Alpha(45), 1f);
		graphics.DrawLine(pen, 28, 124, base.Width - 28, 124);
	}
}
