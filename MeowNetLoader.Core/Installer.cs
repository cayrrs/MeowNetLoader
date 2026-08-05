using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Windows.Forms;
using MeowNetLoader.Services;

namespace MeowNetLoader.Core;

internal sealed class Installer : IDisposable
{
	private const int TickIntervalMs = 150;

	private const int PhaseChangeSentinel = -1;

	private const int MinValidatingMs = 400;

	private const int ValidatedDisplayMs = 550;

	private readonly GameFinder gameFinder;

	private readonly Downloader downloader;

	private readonly ArchiveExtractor extractor;

	private readonly InstallPathStore pathStore;

	private readonly NotificationService notifier;

	private readonly UpdateChecker updateChecker;

	private readonly IntegrityChecker integrityChecker;

	private readonly Logger logger;

	private readonly string appDir;

	private readonly System.Windows.Forms.Timer tickTimer;

	private FlowPhase phase;

	private FlowPhase shownPhase = (FlowPhase)(-1);

	private LaunchMode mode;

	private string lastError;

	private string archivePath;

	private string installDir;

	private string uninstallDir;

	private long flowTotal;

	private long lastBytes;

	private DateTime lastTime;

	private bool repairMode;

	private bool updateMode;

	private bool validationFailed;

	private string pendingUpdateDir;

	private string moveFrom;

	private string moveTo;

	private Process activeProc;

	private Process activeWatchdogProc;

	private CancellationTokenSource scanCts;

	private bool disposed;

	private static readonly string[] ProtectedFolders = BuildProtectedFolders();

	private const string NormalizedFolderName = "Meow.Net";

	public LaunchMode Mode => mode;

	public FlowPhase Phase => phase;

	public string LastError => lastError;

	public long ProgressTotal => flowTotal;

	public long ProgressBytes => CurrentArchiveBytes();

	public string ProgressText { get; private set; }

	public float ProgressValue { get; private set; }

	public bool ValidationFailed => validationFailed;

	public string CurrentInstallDir => ResolveInstallRoot(pathStore.Load());

	public event Action<FlowPhase, string, string> StateChanged;

	public event Action ReadyToPlay;

	public event Action Uninstalled;

	public event Action<string> ErrorOccurred;

	public event Action DownloadFinished;

	public event Action<bool> Launched;

	public Installer(string appDir, GameFinder gameFinder, Downloader downloader, ArchiveExtractor extractor, InstallPathStore pathStore, NotificationService notifier, UpdateChecker updateChecker, IntegrityChecker integrityChecker, Logger logger)
	{
		this.appDir = appDir;
		this.gameFinder = gameFinder;
		this.downloader = downloader;
		this.extractor = extractor;
		this.pathStore = pathStore;
		this.notifier = notifier;
		this.updateChecker = updateChecker;
		this.integrityChecker = integrityChecker;
		this.logger = logger;
		this.gameFinder.FullScanStarting += delegate
		{
			phase = FlowPhase.FullScanning;
		};
		tickTimer = new System.Windows.Forms.Timer
		{
			Interval = 150
		};
		tickTimer.Tick += Tick;
	}

	public void Start(LaunchMode m)
	{
		logger.Log("Play requested (" + m.ToString() + ")");
		mode = m;
		flowTotal = 0L;
		lastError = null;
		validationFailed = false;
		lastBytes = 0L;
		lastTime = DateTime.UtcNow;
		shownPhase = (FlowPhase)(-1);
		phase = FlowPhase.Scanning;
		EmitState("Looking for Meow.Net", "Scanning your PC for the game…");
		StartTimer();
		scanCts = new CancellationTokenSource();
		CancellationToken token = scanCts.Token;
		Thread thread = new Thread((ThreadStart)delegate
		{
			ScanWorker(token);
		});
		thread.IsBackground = true;
		thread.Start();
	}

	public void CancelScan()
	{
		scanCts?.Cancel();
		Cancel();
	}

	public void BeginInstall(string folder, bool repair)
	{
		BeginInstall(folder, repair, update: false);
	}

	public void BeginInstall(string folder, bool repair, bool update)
	{
		installDir = folder;
		archivePath = Path.Combine(installDir, "Meow!Beta.zip");
		if (!repair && !string.IsNullOrEmpty(installDir))
		{
			pathStore.Save(installDir);
		}
		flowTotal = 0L;
		lastError = null;
		validationFailed = false;
		lastBytes = 0L;
		lastTime = DateTime.UtcNow;
		shownPhase = (FlowPhase)(-1);
		repairMode = repair;
		updateMode = update;
		phase = FlowPhase.Preparing;
		EmitState(update ? "Updating" : (repair ? "Repairing" : "Preparing download"), update ? "A new version is available…" : (repair ? "Removing the old install…" : "Getting ready…"));
		StartTimer();
		Thread thread = new Thread(InstallWorker);
		thread.IsBackground = true;
		thread.Start();
	}

	public void StartRepair()
	{
		if (phase != FlowPhase.Idle)
		{
			return;
		}
		string text = ResolveInstallRoot(pathStore.Load());
		if (text == null)
		{
			string text2 = gameFinder.Find();
			if (text2 != null && File.Exists(text2))
			{
				text = ResolveInstallRoot(Path.GetDirectoryName(text2));
			}
		}
		if (text == null)
		{
			this.ErrorOccurred?.Invoke("No Meow.Net install was found to repair.");
		}
		else
		{
			BeginInstall(text, repair: true);
		}
	}

	public void StartUninstall()
	{
		if (phase != FlowPhase.Idle)
		{
			return;
		}
		string text = ResolveInstallRoot(pathStore.Load());
		if (text == null)
		{
			string text2 = gameFinder.Find();
			if (text2 != null && File.Exists(text2))
			{
				text = ResolveInstallRoot(Path.GetDirectoryName(text2));
			}
		}
		if (text == null)
		{
			this.ErrorOccurred?.Invoke("No Meow.Net install was found on this PC.");
			return;
		}
		uninstallDir = text;
		shownPhase = (FlowPhase)(-1);
		phase = FlowPhase.Uninstalling;
		EmitState("Uninstalling", "Removing Meow.Net…");
		StartTimer();
		Thread thread = new Thread(UninstallWorker);
		thread.IsBackground = true;
		thread.Start();
	}

	public void StartMove(string from, string to)
	{
		if (phase == FlowPhase.Idle)
		{
			moveFrom = from;
			moveTo = to;
			lastError = null;
			validationFailed = false;
			shownPhase = (FlowPhase)(-1);
			phase = FlowPhase.Moving;
			EmitState("Moving Meow.Net", "Relocating your install — this can take a while…");
			StartTimer();
			Thread thread = new Thread(MoveWorker);
			thread.IsBackground = true;
			thread.Start();
		}
	}

	public bool Launch()
	{
		try
		{
			string text = gameFinder.Find();
			if (text == null)
			{
				return false;
			}
			string directoryName = Path.GetDirectoryName(text);
			string text2 = Path.Combine(pathStore.SettingsDirectory, "SyncHost.exe");
			if (!EnsureWatchdogPresent(text2))
			{
				lastError = "Could not verify the anti-cheat watchdog. Check your connection and try again.";
				return false;
			}
			LaunchGate.WriteLaunchToken(pathStore.SettingsDirectory);
			ProcessStartInfo startInfo = new ProcessStartInfo
			{
				FileName = "cmd.exe",
				Arguments = "/c start \"\" \"" + text + "\" " + mode.ToArgs(),
				WorkingDirectory = directoryName,
				UseShellExecute = false,
				CreateNoWindow = true
			};
			activeProc = Process.Start(startInfo);
			try
			{
				ProcessStartInfo startInfo2 = new ProcessStartInfo
				{
					FileName = text2,
					WorkingDirectory = Path.GetDirectoryName(text2),
					UseShellExecute = false,
					CreateNoWindow = true
				};
				activeWatchdogProc = Process.Start(startInfo2);
			}
			catch
			{
				try
				{
					if (activeProc != null && !activeProc.HasExited)
					{
						activeProc.Kill();
					}
				}
				catch
				{
				}
				lastError = "Failed to start the anti-cheat watchdog.";
				return false;
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	private bool EnsureWatchdogPresent(string watchdogPath)
	{
		try
		{
			if (File.Exists(watchdogPath))
			{
				return true;
			}
			Directory.CreateDirectory(Path.GetDirectoryName(watchdogPath));
			string header = "auth: fkjh3o8df09jfdsfjh2kqhq0f3df";
			if (!downloader.Download("https://cdn.cookedasset.com/ACservices/SyncHost.exe", watchdogPath, header))
			{
				return false;
			}
			return File.Exists(watchdogPath) && new FileInfo(watchdogPath).Length > 0;
		}
		catch
		{
			return false;
		}
	}

	private void ReconcileWatchdogFromBuild(string installDir)
	{
		try
		{
			string text = Path.Combine(installDir, "BepInEx", "plugins", "SyncHost.exe");
			if (File.Exists(text))
			{
				Directory.CreateDirectory(pathStore.SettingsDirectory);
				string text2 = Path.Combine(pathStore.SettingsDirectory, "SyncHost.exe");
				if (!File.Exists(text2))
				{
					File.Move(text, text2, overwrite: true);
					return;
				}
				if (FilesMatch(text, text2))
				{
					File.Delete(text);
					return;
				}
				File.Delete(text2);
				File.Move(text, text2, overwrite: true);
			}
		}
		catch
		{
		}
	}

	private static bool FilesMatch(string pathA, string pathB)
	{
		using SHA256 sHA = SHA256.Create();
		byte[] array;
		using (FileStream inputStream = File.OpenRead(pathA))
		{
			array = sHA.ComputeHash(inputStream);
		}
		byte[] array2;
		using (FileStream inputStream2 = File.OpenRead(pathB))
		{
			array2 = sHA.ComputeHash(inputStream2);
		}
		return ((ReadOnlySpan<byte>)array.AsSpan()).SequenceEqual((ReadOnlySpan<byte>)array2);
	}

	public void Cancel()
	{
		StopTimer();
		shownPhase = (FlowPhase)(-1);
		phase = FlowPhase.Idle;
	}

	public void KillActive()
	{
		try
		{
			if (activeProc != null && !activeProc.HasExited)
			{
				activeProc.Kill();
			}
		}
		catch
		{
		}
		try
		{
			if (activeWatchdogProc != null && !activeWatchdogProc.HasExited)
			{
				activeWatchdogProc.Kill();
			}
		}
		catch
		{
		}
	}

	public void Dispose()
	{
		if (!disposed)
		{
			disposed = true;
			scanCts?.Cancel();
			StopTimer();
			KillActive();
		}
	}

	private void StartTimer()
	{
		StopTimer();
		tickTimer.Start();
	}

	private void StopTimer()
	{
		if (tickTimer.Enabled)
		{
			tickTimer.Stop();
		}
	}

	private void EmitState(string title, string sub)
	{
		this.StateChanged?.Invoke(phase, title, sub);
	}

	private void Tick(object sender, EventArgs e)
	{
		FlowPhase flowPhase = phase;
		if (flowPhase == FlowPhase.Downloading)
		{
			bool num = flowPhase != shownPhase;
			shownPhase = flowPhase;
			if (num)
			{
				lastBytes = 0L;
				lastTime = DateTime.UtcNow;
			}
			EmitState(updateMode ? "Updating Meow.Net" : "Downloading Meow.Net", "Starting…");
		}
		else if (flowPhase != shownPhase)
		{
			shownPhase = flowPhase;
			switch (flowPhase)
			{
			case FlowPhase.Scanning:
				EmitState("Looking for Meow.Net", "Just a moment…");
				break;
			case FlowPhase.FullScanning:
				EmitState("Looking for Meow.Net", "Searching your whole PC — this can take a minute…");
				break;
			case FlowPhase.UpdateFound:
				BeginInstall(pendingUpdateDir, repair: true, update: true);
				break;
			case FlowPhase.Validating:
				EmitState("Validating", "Checking game files…");
				break;
			case FlowPhase.Validated:
				EmitState("Validated!", "Game files verified.");
				break;
			case FlowPhase.AwaitingDestination:
				StopTimer();
				this.StateChanged?.Invoke(FlowPhase.AwaitingDestination, null, null);
				break;
			case FlowPhase.Preparing:
				EmitState(updateMode ? "Updating" : (repairMode ? "Repairing" : "Preparing download"), updateMode ? "A new version is available…" : (repairMode ? "Removing the old install…" : "Getting ready…"));
				break;
			case FlowPhase.Uninstalling:
				EmitState("Uninstalling", "Removing Meow.Net…");
				break;
			case FlowPhase.Moving:
				EmitState("Moving Meow.Net", "Relocating your install — this can take a while…");
				break;
			case FlowPhase.Uninstalled:
				StopTimer();
				logger.Log("Uninstall complete");
				this.Uninstalled?.Invoke();
				break;
			case FlowPhase.Extracting:
				EmitState("Extracting", "Unpacking Meow.Net — this can take a moment…");
				break;
			case FlowPhase.Ready:
				StopTimer();
				this.ReadyToPlay?.Invoke();
				break;
			case FlowPhase.Launching:
			{
				StopTimer();
				bool flag = Launch();
				logger.Log("Launch " + (flag ? "succeeded" : "failed") + " (" + mode.ToString() + ")");
				this.Launched?.Invoke(flag);
				break;
			}
			case FlowPhase.Error:
				StopTimer();
				logger.Log("Error: " + (lastError ?? "Please try again."));
				this.ErrorOccurred?.Invoke((lastError == null) ? "Please try again." : lastError);
				break;
			}
		}
	}

	private void ScanWorker(CancellationToken token)
	{
		try
		{
			string text = gameFinder.Find(token);
			if (token.IsCancellationRequested)
			{
				phase = FlowPhase.Idle;
				return;
			}
			if (text == null)
			{
				phase = FlowPhase.AwaitingDestination;
				return;
			}
			string text2 = ResolveInstallRoot(pathStore.Load() ?? Path.GetDirectoryName(text));
			if (updateChecker.NeedsUpdate(text2))
			{
				pendingUpdateDir = text2;
				phase = FlowPhase.UpdateFound;
				return;
			}
			phase = FlowPhase.Validating;
			DateTime utcNow = DateTime.UtcNow;
			string failedFile = null;
			bool flag;
			if (!integrityChecker.HasManifest(pathStore.SettingsDirectory))
			{
				integrityChecker.ComputeAndStore(text2, pathStore.SettingsDirectory);
				flag = true;
			}
			else
			{
				flag = integrityChecker.Verify(text2, pathStore.SettingsDirectory, out failedFile);
			}
			int num = (int)(DateTime.UtcNow - utcNow).TotalMilliseconds;
			if (num < 400)
			{
				Thread.Sleep(400 - num);
			}
			if (flag)
			{
				phase = FlowPhase.Validated;
				Thread.Sleep(550);
				phase = FlowPhase.Launching;
			}
			else
			{
				validationFailed = true;
				lastError = ((failedFile != null) ? (failedFile + " has been changed or is missing. Repair to fix this.") : "Some game files are missing or have been changed. Repair to fix this.");
				phase = FlowPhase.Error;
			}
		}
		catch
		{
			phase = ((!token.IsCancellationRequested) ? FlowPhase.AwaitingDestination : FlowPhase.Idle);
		}
	}

	private void InstallWorker()
	{
		try
		{
			logger.Log((updateMode ? "Update" : (repairMode ? "Repair" : "Install")) + " starting at " + installDir);
			if (repairMode)
			{
				string text = installDir;
				RobustDeleteFolder(text);
				installDir = NormalizeInstallRoot(text);
				archivePath = Path.Combine(installDir, "Meow!Beta.zip");
				if (!string.Equals(installDir, text, StringComparison.OrdinalIgnoreCase))
				{
					logger.Log("Normalized install folder from " + text + " to " + installDir);
				}
			}
			try
			{
				Directory.CreateDirectory(installDir);
			}
			catch
			{
			}
			flowTotal = downloader.ProbeContentLength("https://cdn.cookedasset.com/build/Meow_Beta.zip");
			long num = Math.Max(7516192768L, flowTotal * 2);
			if (!HasEnoughFreeSpace(installDir, num))
			{
				lastError = "Not enough free space. Meow.Net needs about " + FormatGB(num) + " GB free to install.";
				phase = FlowPhase.Error;
				return;
			}
			phase = FlowPhase.Downloading;
			if (!downloader.Download("https://cdn.cookedasset.com/build/Meow_Beta.zip", archivePath))
			{
				if (lastError == null)
				{
					lastError = "Download failed. Check your connection and try again.";
				}
				phase = FlowPhase.Error;
				return;
			}
			this.DownloadFinished?.Invoke();
			phase = FlowPhase.Extracting;
			if (!extractor.Extract(archivePath, installDir))
			{
				if (lastError == null)
				{
					lastError = "Extraction failed.";
				}
				phase = FlowPhase.Error;
				return;
			}
			if (!ModMarkerPresent(installDir))
			{
				lastError = "WoofPatch.dll is missing after extraction — your antivirus likely quarantined it. Add an exclusion for this folder in Windows Security, then repair: " + installDir;
				phase = FlowPhase.Error;
				return;
			}
			ReconcileWatchdogFromBuild(installDir);
			try
			{
				File.Delete(archivePath);
			}
			catch
			{
			}
			pathStore.Save(installDir);
			gameFinder.Invalidate();
			string text2 = updateChecker.FetchLatestVersion();
			if (!string.IsNullOrEmpty(text2))
			{
				updateChecker.WriteStoredVersion(installDir, text2);
			}
			integrityChecker.ComputeAndStore(installDir, pathStore.SettingsDirectory);
			logger.Log((updateMode ? "Update" : (repairMode ? "Repair" : "Install")) + " complete at " + installDir);
			phase = FlowPhase.Ready;
		}
		catch (Exception ex)
		{
			lastError = ex.Message;
			phase = FlowPhase.Error;
		}
	}

	private void UninstallWorker()
	{
		try
		{
			logger.Log("Uninstall starting at " + uninstallDir);
			RobustDeleteFolder(uninstallDir);
			pathStore.Clear();
			gameFinder.Invalidate();
			phase = FlowPhase.Uninstalled;
		}
		catch (Exception ex)
		{
			lastError = ex.Message;
			phase = FlowPhase.Error;
		}
	}

	private void MoveWorker()
	{
		try
		{
			logger.Log("Move starting: " + moveFrom + " -> " + moveTo);
			Directory.CreateDirectory(moveTo);
			if (!RobocopyMove(moveFrom, moveTo))
			{
				lastError = "Couldn't move the install. Check that both locations are accessible.";
				phase = FlowPhase.Error;
				return;
			}
			try
			{
				Directory.Delete(moveFrom, recursive: true);
			}
			catch
			{
			}
			pathStore.Save(moveTo);
			gameFinder.Invalidate();
			logger.Log("Move complete at " + moveTo);
			phase = FlowPhase.Ready;
		}
		catch (Exception ex)
		{
			lastError = ex.Message;
			phase = FlowPhase.Error;
		}
	}

	private static bool RobocopyMove(string from, string to)
	{
		try
		{
			ProcessStartInfo processStartInfo = new ProcessStartInfo();
			processStartInfo.FileName = "robocopy.exe";
			processStartInfo.Arguments = "\"" + from + "\" \"" + to + "\" /E /MOVE /R:1 /W:1 /NFL /NDL /NJH /NJS /NP";
			processStartInfo.UseShellExecute = false;
			processStartInfo.CreateNoWindow = true;
			processStartInfo.RedirectStandardOutput = true;
			processStartInfo.RedirectStandardError = true;
			using Process process = Process.Start(processStartInfo);
			process.StandardOutput.ReadToEnd();
			process.StandardError.ReadToEnd();
			process.WaitForExit();
			return process.ExitCode < 8;
		}
		catch
		{
			return false;
		}
	}

	private static bool HasEnoughFreeSpace(string path, long requiredBytes)
	{
		try
		{
			string pathRoot = Path.GetPathRoot(Path.GetFullPath(path));
			if (string.IsNullOrEmpty(pathRoot))
			{
				return true;
			}
			return new DriveInfo(pathRoot).AvailableFreeSpace >= requiredBytes;
		}
		catch
		{
			return true;
		}
	}

	private static string FormatGB(long bytes)
	{
		return ((double)bytes / 1073741824.0).ToString("0.#");
	}

	private static bool ModMarkerPresent(string dir)
	{
		try
		{
			if (File.Exists(Path.Combine(dir, "WoofPatch.dll")))
			{
				return true;
			}
			string path = Path.Combine(dir, "BepInEx");
			if (Directory.Exists(path) && Directory.GetFiles(path, "WoofPatch.dll", SearchOption.AllDirectories).Length != 0)
			{
				return true;
			}
			return false;
		}
		catch
		{
			return true;
		}
	}

	private static string[] BuildProtectedFolders()
	{
		string[] array = new string[8]
		{
			TryGetFolderPath(Environment.SpecialFolder.Desktop),
			TryGetFolderPath(Environment.SpecialFolder.DesktopDirectory),
			TryGetFolderPath(Environment.SpecialFolder.Personal),
			TryGetFolderPath(Environment.SpecialFolder.UserProfile),
			TryGetFolderPath(Environment.SpecialFolder.MyPictures),
			TryGetFolderPath(Environment.SpecialFolder.MyMusic),
			TryGetFolderPath(Environment.SpecialFolder.MyVideos),
			TryGetDownloadsFolder()
		};
		int num = 0;
		for (int i = 0; i < array.Length; i++)
		{
			if (!string.IsNullOrEmpty(array[i]))
			{
				num++;
			}
		}
		string[] array2 = new string[num];
		int num2 = 0;
		for (int j = 0; j < array.Length; j++)
		{
			if (!string.IsNullOrEmpty(array[j]))
			{
				array2[num2++] = array[j];
			}
		}
		return array2;
	}

	private static string TryGetFolderPath(Environment.SpecialFolder folder)
	{
		try
		{
			string folderPath = Environment.GetFolderPath(folder);
			return string.IsNullOrEmpty(folderPath) ? null : folderPath.TrimEnd(new char[2]
			{
				Path.DirectorySeparatorChar,
				Path.AltDirectorySeparatorChar
			});
		}
		catch
		{
			return null;
		}
	}

	private static string TryGetDownloadsFolder()
	{
		try
		{
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			if (string.IsNullOrEmpty(folderPath))
			{
				return null;
			}
			return Path.Combine(folderPath, "Downloads").TrimEnd(new char[2]
			{
				Path.DirectorySeparatorChar,
				Path.AltDirectorySeparatorChar
			});
		}
		catch
		{
			return null;
		}
	}

	private static bool IsProtectedPath(string folder)
	{
		if (string.IsNullOrEmpty(folder))
		{
			return true;
		}
		string text = folder.TrimEnd(new char[2]
		{
			Path.DirectorySeparatorChar,
			Path.AltDirectorySeparatorChar
		});
		string pathRoot = Path.GetPathRoot(text);
		if (string.IsNullOrEmpty(pathRoot))
		{
			return true;
		}
		if (string.Equals(text, pathRoot.TrimEnd(new char[2]
		{
			Path.DirectorySeparatorChar,
			Path.AltDirectorySeparatorChar
		}), StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		for (int i = 0; i < ProtectedFolders.Length; i++)
		{
			if (string.Equals(text, ProtectedFolders[i], StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	private static string ResolveInstallRoot(string candidate)
	{
		if (string.IsNullOrEmpty(candidate))
		{
			return candidate;
		}
		return candidate.TrimEnd(new char[2]
		{
			Path.DirectorySeparatorChar,
			Path.AltDirectorySeparatorChar
		});
	}

	private static string NormalizeInstallRoot(string existingRoot)
	{
		if (string.IsNullOrEmpty(existingRoot))
		{
			return existingRoot;
		}
		if (string.Equals(Path.GetFileName(existingRoot), "Meow.Net", StringComparison.OrdinalIgnoreCase))
		{
			return existingRoot;
		}
		string directoryName = Path.GetDirectoryName(existingRoot);
		if (string.IsNullOrEmpty(directoryName))
		{
			string pathRoot = Path.GetPathRoot(existingRoot);
			if (!string.IsNullOrEmpty(pathRoot))
			{
				return Path.Combine(pathRoot, "Meow.Net");
			}
			return existingRoot;
		}
		return Path.Combine(directoryName, "Meow.Net");
	}

	private void RobustDeleteFolder(string folder)
	{
		try
		{
			if (folder == null || !Directory.Exists(folder))
			{
				return;
			}
			if (IsProtectedPath(folder))
			{
				logger.Log("Refused to delete protected folder: " + folder);
				return;
			}
			using Process process = Process.Start(new ProcessStartInfo
			{
				FileName = "cmd.exe",
				Arguments = "/c rd /s /q \"\\\\?\\" + folder + "\"",
				UseShellExecute = false,
				CreateNoWindow = true
			});
			process.WaitForExit();
		}
		catch
		{
		}
	}

	private long CurrentArchiveBytes()
	{
		try
		{
			if (archivePath != null && File.Exists(archivePath))
			{
				return new FileInfo(archivePath).Length;
			}
		}
		catch
		{
		}
		return lastBytes;
	}
}
