using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using MeowNetLoader.Core;
using MeowNetLoader.Services;
using MeowNetLoader.UI;

namespace MeowNetLoader.Forms;

internal sealed class MainForm : Form
{
	private const int FormWidth = 1040;

	private const int FormHeight = 600;

	private const int FormCornerRadius = 22;

	private const int CardWidth = 860;

	private const int CardHeight = 410;

	private const int CardCornerRadius = 26;

	private const int CardScreenX = 30;

	private const int CardScreenY = 118;

	private const int CardVrX = 439;

	private const int CardY = 118;

	private const int CardItemWidth = 391;

	private const int CardItemHeight = 250;

	private const int ExitButtonWidth = 76;

	private const int ExitButtonHeight = 36;

	private const int ExitButtonMargin = 30;

	private const int ExitButtonTop = 26;

	private const int SettingsButtonWidth = 44;

	private const int SettingsButtonSpacing = 10;

	private const int FooterButtonHeight = 26;

	private const int FooterButtonSpacing = 10;

	private const int FooterButtonBottom = 374;

	private const int ContentTop = 105;

	private const int SlideDurationMs = 320;

	private const int WM_NCLBUTTONDOWN = 161;

	private const int HTCAPTION = 2;

	private readonly string appDir;

	private readonly InstallPathStore pathStore;

	private readonly NotificationService notifier;

	private readonly GameFinder gameFinder;

	private readonly Downloader downloader;

	private readonly ArchiveExtractor extractor;

	private readonly DependencyChecker dependencyChecker;

	private readonly ShortcutService shortcutService;

	private readonly UpdateChecker updateChecker;

	private readonly IntegrityChecker integrityChecker;

	private readonly LoaderSelfUpdater loaderSelfUpdater;

	private readonly SettingsStore settingsStore;

	private readonly AppSettings settings;

	private readonly Logger logger;

	private readonly Installer installer;

	private bool gameLaunched;

	private MemoryStream gifStream;

	private Image pfpImage;

	private Image desktopImage;

	private Image metaImage;

	private PictureBox bg;

	private DBPanel card;

	private Panel homeRoot;

	private SettingsPanel settingsPanel;

	private RoundedCard cardScreen;

	private RoundedCard cardVR;

	private PillButton btnExit;

	private PillButton btnSettings;

	private PillButton btnRepair;

	private PillButton btnUninstall;

	private OverlayPanel overlay;

	private NotifyIcon trayIcon;

	private string[] destFolders;

	private readonly bool blockedNotice;

	private bool settingsOpen;

	private bool animatingSettings;

	private DateTime lastBytesTime = DateTime.UtcNow;

	private long lastBytesCount;

	private bool progressShown;

	[DllImport("user32.dll")]
	private static extern bool ReleaseCapture();

	private void InitializeComponent()
	{
	}

	[DllImport("user32.dll")]
	private static extern int SendMessage(nint hWnd, int msg, int wParam, int lParam);

	public MainForm(bool blockedNotice = false)
	{
		this.blockedNotice = blockedNotice;
		appDir = Path.GetDirectoryName(Application.ExecutablePath);
		pathStore = new InstallPathStore();
		notifier = new NotificationService(this);
		gameFinder = new GameFinder(appDir, pathStore);
		downloader = new Downloader();
		extractor = new ArchiveExtractor();
		dependencyChecker = new DependencyChecker();
		shortcutService = new ShortcutService(appDir);
		updateChecker = new UpdateChecker();
		integrityChecker = new IntegrityChecker();
		settingsStore = new SettingsStore(pathStore.SettingsDirectory);
		settings = settingsStore.Load();
		logger = new Logger(pathStore.SettingsDirectory);
		installer = new Installer(appDir, gameFinder, downloader, extractor, pathStore, notifier, updateChecker, integrityChecker, logger);
		loaderSelfUpdater = new LoaderSelfUpdater(downloader, logger, pathStore.SettingsDirectory);
		logger.Log("Loader started");
		LaunchGate.WriteLoaderPath(pathStore.SettingsDirectory, Application.ExecutablePath);
		Build();
		SetupTray();
		WireInstaller();
	}

	private void Build()
	{
		base.FormBorderStyle = FormBorderStyle.None;
		base.StartPosition = FormStartPosition.CenterScreen;
		base.ClientSize = new Size(1040, 600);
		using (GraphicsPath path = Gfx.Round(new Rectangle(0, 0, 1040, 600), 22))
		{
			base.Region = new Region(path);
		}
		BackColor = Theme.RedBot;
		Text = "Meow.Net";
		base.KeyPreview = true;
		DoubleBuffered = true;
		SetFormIcon();
		bg = new PictureBox
		{
			Dock = DockStyle.Fill,
			SizeMode = PictureBoxSizeMode.StretchImage,
			BackColor = Theme.RedBot
		};
		LoadBackground();
		LoadPfp();
		desktopImage = LoadEmbeddedImage("MeowNetLoader.Desktop");
		metaImage = LoadEmbeddedImage("MeowNetLoader.Meta");
		base.Controls.Add(bg);
		EnableDrag(bg);
		card = new DBPanel
		{
			Bounds = new Rectangle(90, 95, 860, 410),
			BackColor = Theme.RedBot
		};
		card.Paint += PaintCard;
		using (GraphicsPath path2 = Gfx.Round(new Rectangle(0, 0, 860, 410), 26))
		{
			card.Region = new Region(path2);
		}
		base.Controls.Add(card);
		EnableDrag(card);
		homeRoot = new DBPanel
		{
			Bounds = new Rectangle(0, 105, 860, 305)
		};
		homeRoot.Paint += PaintContentBackground;
		card.Controls.Add(homeRoot);
		cardScreen = new RoundedCard
		{
			IconKind = 0,
			IconImage = desktopImage,
			TitleText = "Screen",
			DescText = "Play In Screen Mode",
			CtaText = "▶  LAUNCH SCREEN",
			Bounds = new Rectangle(30, 13, 391, 250),
			GradientTop = 118,
			GradientSpan = 410
		};
		cardScreen.Clicked = delegate
		{
			installer.Start(LaunchMode.Screen);
		};
		homeRoot.Controls.Add(cardScreen);
		cardVR = new RoundedCard
		{
			IconKind = 1,
			IconImage = metaImage,
			TitleText = "VR",
			DescText = "Play In VR",
			CtaText = "▶  LAUNCH VR",
			Bounds = new Rectangle(439, 13, 391, 250),
			GradientTop = 118,
			GradientSpan = 410
		};
		cardVR.Clicked = delegate
		{
			installer.Start(LaunchMode.VR);
		};
		homeRoot.Controls.Add(cardVR);
		btnExit = new PillButton
		{
			Primary = false,
			Text = "Exit",
			BackColor = Color.FromArgb(190, 56, 65),
			Size = new Size(76, 36),
			Location = new Point(754, 26)
		};
		btnExit.Clicked = base.Close;
		card.Controls.Add(btnExit);
		btnSettings = new PillButton
		{
			Primary = false,
			Text = "⚙",
			BackColor = Color.FromArgb(190, 56, 65),
			Size = new Size(44, 36),
			Location = new Point(700, 26)
		};
		btnSettings.Clicked = OpenSettingsPanel;
		card.Controls.Add(btnSettings);
		btnUninstall = new PillButton
		{
			Primary = false,
			Text = "Uninstall",
			BackColor = Color.FromArgb(163, 43, 52),
			Size = new Size(100, 26)
		};
		btnRepair = new PillButton
		{
			Primary = false,
			Text = "Repair",
			BackColor = Color.FromArgb(163, 43, 52),
			Size = new Size(92, 26)
		};
		int x = 628;
		btnUninstall.Location = new Point(x, 269);
		btnRepair.Location = new Point(738, 269);
		btnUninstall.Clicked = AskUninstall;
		btnRepair.Clicked = AskRepair;
		homeRoot.Controls.Add(btnUninstall);
		homeRoot.Controls.Add(btnRepair);
		Label value = new Label
		{
			Text = "v1.0.9",
			AutoSize = false,
			BackColor = Color.Transparent,
			ForeColor = Theme.Alpha(120),
			Font = Theme.Foot,
			TextAlign = ContentAlignment.MiddleLeft,
			Location = new Point(30, 269),
			Size = new Size(100, 26)
		};
		homeRoot.Controls.Add(value);
		settingsPanel = new SettingsPanel
		{
			Bounds = new Rectangle(860, 105, 860, 305)
		};
		settingsPanel.BackClicked = CloseSettingsPanel;
		settingsPanel.ChangeDirectoryClicked = ChangeGameDirectory;
		settingsPanel.OpenInstallDirectoryClicked = OpenInstallDirectory;
		settingsPanel.CheckUpdatesClicked = CheckForUpdatesNow;
		settingsPanel.CopyDiagnosticsClicked = CopyDiagnosticInfo;
		settingsPanel.TestConnectionClicked = TestConnection;
		settingsPanel.AutoCloseChanged = delegate(bool on)
		{
			settings.AutoCloseOnLaunch = on;
			settingsStore.Save(settings);
		};
		settingsPanel.MinimizeToTrayChanged = delegate(bool on)
		{
			settings.MinimizeToTray = on;
			settingsStore.Save(settings);
		};
		settingsPanel.DefaultModeChanged = delegate(string mode)
		{
			settings.DefaultPlayMode = mode;
			settingsStore.Save(settings);
			settingsPanel.SetDefaultMode(mode);
		};
		settingsPanel.DesktopShortcutClicked = delegate
		{
			if (shortcutService.DesktopShortcutExists())
			{
				shortcutService.Remove();
			}
			else
			{
				shortcutService.Create();
			}
			settingsPanel.SetDesktopShortcutExists(shortcutService.DesktopShortcutExists());
		};
		card.Controls.Add(settingsPanel);
		overlay = new OverlayPanel();
		base.Controls.Add(overlay);
		bg.SendToBack();
		card.BringToFront();
		base.Shown += delegate
		{
			StartupChecks();
		};
		base.KeyDown += delegate(object? s, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Escape)
			{
				if (overlay.Visible)
				{
					overlay.HideOverlay();
				}
				else
				{
					Close();
				}
			}
		};
	}

	private void WireInstaller()
	{
		installer.StateChanged += OnInstallerState;
		installer.ReadyToPlay += OnReadyToPlay;
		installer.Uninstalled += OnUninstalled;
		installer.ErrorOccurred += OnInstallerError;
		installer.DownloadFinished += OnDownloadFinished;
		installer.Launched += OnLaunched;
	}

	private void SetFormIcon()
	{
		try
		{
			Stream manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MeowNetLoader.AppIcon");
			if (manifestResourceStream != null)
			{
				base.Icon = new Icon(manifestResourceStream);
				return;
			}
		}
		catch
		{
		}
		try
		{
			base.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
		}
		catch
		{
		}
	}

	private void LoadBackground()
	{
		try
		{
			Stream manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MeowNetLoader.Background");
			if (manifestResourceStream != null)
			{
				gifStream = new MemoryStream();
				manifestResourceStream.CopyTo(gifStream);
				manifestResourceStream.Dispose();
				gifStream.Position = 0L;
				bg.Image = Image.FromStream(gifStream);
				return;
			}
		}
		catch
		{
		}
		try
		{
			string path = Path.Combine(appDir, "bg.gif");
			if (File.Exists(path))
			{
				gifStream = new MemoryStream(File.ReadAllBytes(path));
				bg.Image = Image.FromStream(gifStream);
			}
		}
		catch
		{
		}
	}

	private void LoadPfp()
	{
		try
		{
			using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MeowNetLoader.Pfp");
			if (stream != null)
			{
				using (Image original = Image.FromStream(stream))
				{
					pfpImage = new Bitmap(original);
					return;
				}
			}
		}
		catch
		{
		}
	}

	private Image LoadEmbeddedImage(string name)
	{
		try
		{
			using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
			if (stream != null)
			{
				using (Image original = Image.FromStream(stream))
				{
					return new Bitmap(original);
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private void EnableDrag(Control c)
	{
		c.MouseDown += delegate(object s, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				ReleaseCapture();
				SendMessage(base.Handle, 161, 2, 0);
			}
		};
	}

	private void PaintCard(object sender, PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		Rectangle rect = new Rectangle(0, 0, card.Width, card.Height);
		using (GraphicsPath path = Gfx.Round(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 26))
		{
			using (LinearGradientBrush brush = new LinearGradientBrush(rect, Theme.RedTop, Theme.RedBot, LinearGradientMode.Vertical))
			{
				graphics.FillPath(brush, path);
			}
			using Pen pen = new Pen(Theme.Alpha(75), 1.4f);
			graphics.DrawPath(pen, path);
		}
		Rectangle rectangle = new Rectangle(30, 26, 54, 54);
		using (GraphicsPath graphicsPath = Gfx.Round(rectangle, 15))
		{
			if (pfpImage != null)
			{
				GraphicsState gstate = graphics.Save();
				graphics.SetClip(graphicsPath);
				InterpolationMode interpolationMode = graphics.InterpolationMode;
				graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
				graphics.DrawImage(pfpImage, rectangle);
				graphics.InterpolationMode = interpolationMode;
				graphics.Restore(gstate);
				using Pen pen2 = new Pen(Theme.Alpha(90), 1.3f);
				graphics.DrawPath(pen2, graphicsPath);
			}
			else
			{
				using (Brush brush2 = new SolidBrush(Theme.Alpha(46)))
				{
					graphics.FillPath(brush2, graphicsPath);
				}
				using (Pen pen3 = new Pen(Theme.Alpha(90), 1.3f))
				{
					graphics.DrawPath(pen3, graphicsPath);
				}
				using Pen pen4 = new Pen(Color.White, 2.4f);
				pen4.StartCap = LineCap.Round;
				pen4.EndCap = LineCap.Round;
				pen4.LineJoin = LineJoin.Round;
				Gfx.Cat(graphics, rectangle, pen4);
			}
		}
		TextRenderer.DrawText(graphics, settingsOpen ? "Settings" : "Meow.Net Launcher", Theme.Title, new Point(96, 28), Color.White, TextFormatFlags.NoPadding);
		TextRenderer.DrawText(graphics, settingsOpen ? "Configure how the Launcher behaves" : "Select your play mode", Theme.Sub, new Point(98, 64), Theme.Alpha(205), TextFormatFlags.NoPadding);
		TextRenderer.DrawText(graphics, "Meow.Net", Theme.Foot, new Point(30, card.Height - 32), Theme.Alpha(175), TextFormatFlags.NoPadding);
	}

	private void PaintContentBackground(object sender, PaintEventArgs e)
	{
		Panel panel = (Panel)sender;
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		using LinearGradientBrush brush = new LinearGradientBrush(new Rectangle(0, -panel.Top, panel.Width, 410), Theme.RedTop, Theme.RedBot, LinearGradientMode.Vertical);
		graphics.FillRectangle(brush, panel.ClientRectangle);
	}

	private void OnInstallerState(FlowPhase phase, string title, string sub)
	{
		if (phase == FlowPhase.AwaitingDestination)
		{
			AskDestination();
		}
		else
		{
			if (title == null && sub == null)
			{
				return;
			}
			switch (phase)
			{
			case FlowPhase.Downloading:
			{
				long progressTotal = installer.ProgressTotal;
				long progressBytes = installer.ProgressBytes;
				DateTime utcNow = DateTime.UtcNow;
				double num = 0.0;
				if (progressShown)
				{
					double totalSeconds = (utcNow - lastBytesTime).TotalSeconds;
					num = ((totalSeconds > 0.0) ? ((double)(progressBytes - lastBytesCount) * 8.0 / (totalSeconds * 1000000.0)) : 0.0);
					if (num < 0.0)
					{
						num = 0.0;
					}
				}
				lastBytesCount = progressBytes;
				lastBytesTime = utcNow;
				float val = ((progressTotal > 0) ? ((float)((double)progressBytes / (double)progressTotal)) : 0f);
				string s = ((progressTotal > 0) ? (num.ToString("0.0") + " Mbps      " + Mb(progressBytes) + " / " + Mb(progressTotal) + " MB") : (num.ToString("0.0") + " Mbps      " + Mb(progressBytes) + " MB"));
				if (!progressShown)
				{
					progressShown = true;
					overlay.ShowProgress(title ?? string.Empty, s, val);
				}
				else
				{
					overlay.UpdateProgress(s, val);
				}
				break;
			}
			case FlowPhase.Scanning:
			case FlowPhase.FullScanning:
				progressShown = false;
				overlay.ShowScanProgress(title ?? string.Empty, sub ?? string.Empty, "Cancel", CancelScan);
				break;
			default:
				progressShown = false;
				overlay.ShowSpinner(title ?? string.Empty, sub ?? string.Empty);
				break;
			}
		}
	}

	private void CancelScan()
	{
		installer.CancelScan();
		overlay.HideOverlay();
	}

	private void OnReadyToPlay()
	{
		overlay.ShowChoice("Ready to play", "Meow.Net is installed and up to date.", "▶  Play " + installer.Mode.ToLabel(), PlayNow, "Not now", CancelOverlay);
	}

	private void OnUninstalled()
	{
		overlay.ShowChoice("Uninstalled", "Meow.Net was removed from your PC.", "Done", CancelOverlay, null, null);
	}

	private void OnInstallerError(string message)
	{
		btnSettings.ShowBadge = true;
		btnSettings.Invalidate();
		if (installer.ValidationFailed)
		{
			overlay.ShowChoice("Files modified", string.IsNullOrEmpty(message) ? "Some game files are missing or have been changed. Repair to fix this." : message, "Repair now", delegate
			{
				installer.Cancel();
				installer.StartRepair();
			}, "Back", CancelOverlay);
		}
		else
		{
			overlay.ShowChoice("Something went wrong", string.IsNullOrEmpty(message) ? "Please try again." : message, "Back", CancelOverlay, null, null);
		}
	}

	private void OnDownloadFinished()
	{
		notifier.ShowDownloadComplete();
	}

	private static void OpenUrl(string url)
	{
		try
		{
			Process.Start(new ProcessStartInfo(url)
			{
				UseShellExecute = true
			});
		}
		catch
		{
		}
	}

	private void PlayNow()
	{
		bool num = installer.Launch();
		string text = installer.Mode.ToLabel();
		if (num)
		{
			gameLaunched = true;
			if (settings.AutoCloseOnLaunch)
			{
				CloseOrMinimize();
			}
			else
			{
				overlay.ShowMessage(text + " mode launched", "Meow.Net is starting, you can close the loader", "Close", CloseOrMinimize);
			}
		}
		else
		{
			overlay.ShowChoice("Couldn't launch", "The game wasn't found after install.", "Back", CancelOverlay, null, null);
		}
	}

	private void OnLaunched(bool ok)
	{
		if (ok)
		{
			gameLaunched = true;
			if (settings.AutoCloseOnLaunch)
			{
				CloseOrMinimize();
			}
			else
			{
				overlay.ShowMessage("Game launched!", "Meow.Net is starting, you can close the loader", "Close", CloseOrMinimize);
			}
		}
		else
		{
			overlay.ShowChoice("Couldn't launch", "The game wasn't found. Try repairing your install.", "Back", CancelOverlay, null, null);
		}
	}

	private void CancelOverlay()
	{
		installer.Cancel();
		overlay.HideOverlay();
	}

	private void AskDestination()
	{
		GetDriveOptions(out var labels, out destFolders);
		overlay.ShowMenu("Where should Meow.Net install?", "Pick a drive or a custom folder - it needs about 7 GB free.", labels, OnDestPicked);
	}

	private void GetDriveOptions(out string[] labels, out string[] folders)
	{
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		try
		{
			DriveInfo[] drives = DriveInfo.GetDrives();
			foreach (DriveInfo driveInfo in drives)
			{
				try
				{
					if (driveInfo.DriveType == DriveType.Fixed && driveInfo.IsReady)
					{
						long num = driveInfo.AvailableFreeSpace / 1073741824;
						list.Add(driveInfo.Name.TrimEnd('\\') + "  drive        " + num + " GB free");
						list2.Add(Path.Combine(driveInfo.RootDirectory.FullName, "Meow.Net"));
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
		list.Add("Custom location...");
		list2.Add(null);
		list.Add("Cancel");
		list2.Add("");
		labels = list.ToArray();
		folders = list2.ToArray();
	}

	private void OnDestPicked(int idx)
	{
		string text = destFolders[idx];
		if (text == "")
		{
			CancelOverlay();
			return;
		}
		if (text == null)
		{
			string text2 = PickCustomFolder();
			if (text2 == null)
			{
				AskDestination();
				return;
			}
			text = Path.Combine(text2, "Meow.Net");
		}
		installer.BeginInstall(text, repair: false);
	}

	private string PickCustomFolder()
	{
		try
		{
			using FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
			folderBrowserDialog.Description = "Choose where to install Meow.Net";
			folderBrowserDialog.ShowNewFolderButton = true;
			if (folderBrowserDialog.ShowDialog(this) == DialogResult.OK && folderBrowserDialog.SelectedPath.Length > 0)
			{
				return folderBrowserDialog.SelectedPath;
			}
		}
		catch
		{
		}
		return null;
	}

	private void OpenSettingsPanel()
	{
		if (!animatingSettings && !settingsOpen)
		{
			settingsPanel.SetGameDirectory(installer.CurrentInstallDir);
			settingsPanel.SetAutoClose(settings.AutoCloseOnLaunch);
			settingsPanel.SetMinimizeToTray(settings.MinimizeToTray);
			settingsPanel.SetDefaultMode(settings.DefaultPlayMode);
			settingsPanel.SetDesktopShortcutExists(shortcutService.DesktopShortcutExists());
			btnSettings.ShowBadge = false;
			btnSettings.Visible = false;
			AnimateSettings(opening: true);
		}
	}

	private void CloseSettingsPanel()
	{
		if (!animatingSettings && settingsOpen)
		{
			AnimateSettings(opening: false);
		}
	}

	private void AnimateSettings(bool opening)
	{
		animatingSettings = true;
		settingsOpen = opening;
		card.Invalidate();
		int startHomeX = ((!opening) ? (-860) : 0);
		int endHomeX = (opening ? (-860) : 0);
		int startSettingsX = (opening ? 860 : 0);
		int endSettingsX = ((!opening) ? 860 : 0);
		DateTime start = DateTime.UtcNow;
		System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer
		{
			Interval = 15
		};
		timer.Tick += delegate
		{
			double num = (DateTime.UtcNow - start).TotalMilliseconds / 320.0;
			if (num >= 1.0)
			{
				num = 1.0;
			}
			double num2 = 1.0 - Math.Pow(1.0 - num, 3.0);
			homeRoot.Left = startHomeX + (int)((double)(endHomeX - startHomeX) * num2);
			settingsPanel.Left = startSettingsX + (int)((double)(endSettingsX - startSettingsX) * num2);
			if (num >= 1.0)
			{
				timer.Stop();
				timer.Dispose();
				animatingSettings = false;
				if (!opening)
				{
					btnSettings.Visible = true;
				}
			}
		};
		timer.Start();
	}

	private void ChangeGameDirectory()
	{
		string currentInstallDir = installer.CurrentInstallDir;
		if (currentInstallDir == null)
		{
			overlay.ShowChoice("No install found", "Install the game first from the main menu, then you can change its location.", "Back", overlay.HideOverlay, null, null);
			return;
		}
		string text = PickCustomFolder();
		if (text != null)
		{
			string text2 = Path.Combine(text, "Meow.Net");
			if (!string.Equals(Path.GetFullPath(text2), Path.GetFullPath(currentInstallDir), StringComparison.OrdinalIgnoreCase))
			{
				CloseSettingsPanel();
				installer.StartMove(currentInstallDir, text2);
			}
		}
	}

	private void OpenInstallDirectory()
	{
		string currentInstallDir = installer.CurrentInstallDir;
		if (currentInstallDir == null || !Directory.Exists(currentInstallDir))
		{
			overlay.ShowChoice("No install found", "Install the game first from the main menu.", "Back", overlay.HideOverlay, null, null);
			return;
		}
		Process.Start(new ProcessStartInfo("explorer.exe", "\"" + currentInstallDir + "\"")
		{
			UseShellExecute = true
		});
	}

	private void CheckForUpdatesNow()
	{
		string dir = installer.CurrentInstallDir;
		if (dir == null)
		{
			overlay.ShowChoice("No install found", "Install the game first from the main menu.", "Back", overlay.HideOverlay, null, null);
			return;
		}
		overlay.ShowSpinner("Checking for updates", "Just a moment…");
		Thread thread = new Thread((ThreadStart)delegate
		{
			bool needsUpdate = updateChecker.NeedsUpdate(dir);
			BeginInvoke(delegate
			{
				if (needsUpdate)
				{
					overlay.ShowChoice("Update available", "A new version is ready. Repair now to install it.", "Repair now", delegate
					{
						CloseSettingsPanel();
						installer.Cancel();
						installer.StartRepair();
					}, "Back", overlay.HideOverlay);
				}
				else
				{
					overlay.ShowChoice("Up to date", "You're already on the latest version.", "Back", overlay.HideOverlay, null, null);
				}
			});
		});
		thread.IsBackground = true;
		thread.Start();
	}

	private void TestConnection()
	{
		overlay.ShowSpinner("Testing connection", "Just a moment…");
		Thread thread = new Thread((ThreadStart)delegate
		{
			long elapsedMs;
			bool reachable = updateChecker.TestConnectivity(out elapsedMs);
			BeginInvoke(delegate
			{
				if (reachable)
				{
					overlay.ShowChoice("Connected", "Reached the server in " + elapsedMs + " ms.", "Back", overlay.HideOverlay, null, null);
				}
				else
				{
					overlay.ShowChoice("Can't reach server", "Couldn't connect to meowii.app. Check your internet connection.", "Back", overlay.HideOverlay, null, null);
				}
			});
		});
		thread.IsBackground = true;
		thread.Start();
	}

	private void CopyDiagnosticInfo()
	{
		try
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("Meow.Net Launcher Diagnostic Info");
			stringBuilder.AppendLine("Generated: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC");
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("Install directory: " + (installer.CurrentInstallDir ?? "Not installed"));
			stringBuilder.AppendLine("Last error: " + (installer.LastError ?? "None"));
			stringBuilder.AppendLine("Validation failed: " + installer.ValidationFailed);
			stringBuilder.AppendLine("Auto Close On Launch: " + (settings.AutoCloseOnLaunch ? "On" : "Off"));
			stringBuilder.AppendLine("Minimize To Tray: " + (settings.MinimizeToTray ? "On" : "Off"));
			stringBuilder.AppendLine("Default Play Mode: " + settings.DefaultPlayMode);
			stringBuilder.AppendLine("OS: " + Environment.OSVersion);
			stringBuilder.AppendLine(".NET: " + Environment.Version);
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("Recent log:");
			stringBuilder.AppendLine(logger.ReadTail(40));
			Clipboard.SetText(stringBuilder.ToString());
			overlay.ShowMessage("Copied!", "Support info copied to your clipboard — paste it in Discord.", "OK", overlay.HideOverlay);
		}
		catch
		{
			overlay.ShowMessage("Couldn't copy", "Something went wrong grabbing support info.", "OK", overlay.HideOverlay);
		}
	}

	private void SetupTray()
	{
		trayIcon = new NotifyIcon
		{
			Text = "Meow.Net",
			Visible = false
		};
		trayIcon.DoubleClick += delegate
		{
			RestoreFromTray();
		};
		ContextMenuStrip contextMenuStrip = new ContextMenuStrip();
		contextMenuStrip.Items.Add("Launch Screen", null, delegate
		{
			RestoreFromTray();
			installer.Start(LaunchMode.Screen);
		});
		contextMenuStrip.Items.Add("Launch VR", null, delegate
		{
			RestoreFromTray();
			installer.Start(LaunchMode.VR);
		});
		contextMenuStrip.Items.Add(new ToolStripSeparator());
		contextMenuStrip.Items.Add("Open", null, delegate
		{
			RestoreFromTray();
		});
		contextMenuStrip.Items.Add("Exit", null, delegate
		{
			trayIcon.Visible = false;
			Application.Exit();
		});
		trayIcon.ContextMenuStrip = contextMenuStrip;
	}

	private void MinimizeToTray()
	{
		if (trayIcon == null)
		{
			Close();
			return;
		}
		trayIcon.Icon = base.Icon;
		trayIcon.Visible = true;
		Hide();
	}

	private void RestoreFromTray()
	{
		Show();
		base.WindowState = FormWindowState.Normal;
		Activate();
		if (trayIcon != null)
		{
			trayIcon.Visible = false;
		}
	}

	private void CloseOrMinimize()
	{
		if (settings.MinimizeToTray)
		{
			MinimizeToTray();
		}
		else
		{
			Close();
		}
	}

	private void AskRepair()
	{
		overlay.ShowChoice("Repair Meow.Net?", "This deletes your current install and downloads a fresh copy.", "Repair", installer.StartRepair, "Cancel", overlay.HideOverlay);
	}

	private void AskUninstall()
	{
		overlay.ShowChoice("Uninstall Meow.Net?", "This permanently deletes the installed game from your PC.", "Uninstall", installer.StartUninstall, "Cancel", overlay.HideOverlay);
	}

	private void StartupChecks()
	{
		if (blockedNotice)
		{
			overlay.ShowChoice("Direct launch blocked", "Meow.Net needs to be started through the Meow.Net launcher.", "Got it", ContinueStartupChecks, null, null);
		}
		else
		{
			ContinueStartupChecks();
		}
	}

	private void ContinueStartupChecks()
	{
		CheckLoaderUpdate(RunDependencyChecks);
	}

	private void RunDependencyChecks()
	{
		if (!dependencyChecker.Check())
		{
			ShowDepOverlay(dependencyChecker.MissingNames, dependencyChecker.MissingUrls, MaybeAskShortcut);
		}
		else
		{
			MaybeAskShortcut();
		}
	}

	private void CheckLoaderUpdate(Action continuation)
	{
		Thread thread = new Thread((ThreadStart)delegate
		{
			bool updated;
			try
			{
				updated = loaderSelfUpdater.TryUpdate(delegate
				{
					if (!base.IsDisposed)
					{
						BeginInvoke(delegate
						{
							overlay.ShowSpinner("Updating Meow.Net Launcher", "Downloading the latest version…");
						});
					}
				});
			}
			catch
			{
				updated = false;
			}
			if (!base.IsDisposed)
			{
				BeginInvoke(delegate
				{
					if (updated)
					{
						Close();
					}
					else
					{
						overlay.HideOverlay();
						continuation();
					}
				});
			}
		});
		thread.IsBackground = true;
		thread.Start();
	}

	private void ShowDepOverlay(List<string> names, List<string> urls, Action onDone)
	{
		string text = string.Join(",    ", names.ToArray());
		overlay.ShowChoice("Install these first", "Meow.Net needs " + text + " installed on your PC. Open the download pages, install them, then relaunch.", "Open download pages", delegate
		{
			OpenDepsAndContinue(urls, onDone);
		}, "Skip for now", delegate
		{
			overlay.HideOverlay();
			if (onDone != null)
			{
				onDone();
			}
		});
	}

	private void OpenDepsAndContinue(List<string> urls, Action onDone)
	{
		if (urls != null)
		{
			foreach (string url in urls)
			{
				OpenUrl(url);
			}
		}
		overlay.HideOverlay();
		onDone?.Invoke();
	}

	private void MaybeAskShortcut()
	{
		if (shortcutService.DesktopShortcutExists())
		{
			MaybeAutoLaunchDefault();
			return;
		}
		overlay.ShowChoice("Create a desktop shortcut?", "Launch Meow.Net any time, straight from your desktop.", "Yes, create", delegate
		{
			shortcutService.Create();
			overlay.HideOverlay();
			MaybeAutoLaunchDefault();
		}, "Not now", delegate
		{
			overlay.HideOverlay();
			MaybeAutoLaunchDefault();
		});
	}

	private void MaybeAutoLaunchDefault()
	{
		if (settings.DefaultPlayMode == "Screen")
		{
			installer.Start(LaunchMode.Screen);
		}
		else if (settings.DefaultPlayMode == "VR")
		{
			installer.Start(LaunchMode.VR);
		}
	}

	protected override void OnFormClosing(FormClosingEventArgs e)
	{
		if (e.CloseReason == CloseReason.WindowsShutDown || e.CloseReason == CloseReason.ApplicationExitCall)
		{
			e.Cancel = false;
			Environment.Exit(0);
			return;
		}
		try
		{
			if (!gameLaunched)
			{
				installer.KillActive();
			}
			notifier.Dispose();
			trayIcon?.Dispose();
		}
		catch
		{
		}
		base.OnFormClosing(e);
	}

	private static string Mb(long bytes)
	{
		return ((double)bytes / 1048576.0).ToString("0.0");
	}
}
