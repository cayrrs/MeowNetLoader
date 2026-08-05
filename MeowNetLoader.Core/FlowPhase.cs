namespace MeowNetLoader.Core;

internal enum FlowPhase
{
	Idle,
	Downloading,
	Extracting,
	Ready,
	Error,
	Scanning,
	Launching,
	AwaitingDestination,
	Preparing,
	Uninstalling,
	Uninstalled,
	UpdateFound,
	Validating,
	FullScanning,
	Validated,
	Moving
}
