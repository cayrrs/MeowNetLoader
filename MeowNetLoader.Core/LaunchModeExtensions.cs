namespace MeowNetLoader.Core;

internal static class LaunchModeExtensions
{
	public static string ToArgs(this LaunchMode mode)
	{
		if (mode != LaunchMode.VR)
		{
			return "+forcemode:screen -noeac";
		}
		return "+forcemode:vr";
	}

	public static string ToLabel(this LaunchMode mode)
	{
		if (mode != LaunchMode.VR)
		{
			return "Screen";
		}
		return "VR";
	}

	public static LaunchMode Parse(string value)
	{
		if (!(value == "vr"))
		{
			return LaunchMode.Screen;
		}
		return LaunchMode.VR;
	}
}
