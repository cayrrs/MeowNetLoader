using System.Drawing;

namespace MeowNetLoader.UI;

internal static class Theme
{
	public static readonly Color RedTop = Color.FromArgb(194, 58, 67);

	public static readonly Color RedBot = Color.FromArgb(160, 42, 51);

	public static readonly Color CardTop = Color.FromArgb(203, 74, 82);

	public static readonly Color CardBot = Color.FromArgb(178, 55, 64);

	public static readonly Color CardTopHot = Color.FromArgb(228, 110, 116);

	public static readonly Color CardBotHot = Color.FromArgb(198, 80, 88);

	public static readonly Color Dim = Color.FromArgb(51, 9, 15);

	public static readonly Color Accent = Color.FromArgb(179, 36, 43);

	public static readonly Font Title = new Font("Segoe UI", 21f, FontStyle.Bold);

	public static readonly Font Sub = new Font("Segoe UI", 9.5f, FontStyle.Regular);

	public static readonly Font CardTitle = new Font("Segoe UI", 16f, FontStyle.Bold);

	public static readonly Font Desc = new Font("Segoe UI", 9.5f, FontStyle.Regular);

	public static readonly Font Cta = new Font("Segoe UI", 8.5f, FontStyle.Bold);

	public static readonly Font Foot = new Font("Segoe UI", 8.5f, FontStyle.Regular);

	public static readonly Font OvTitle = new Font("Segoe UI", 15f, FontStyle.Bold);

	public static readonly Font OvSub = new Font("Segoe UI", 9.5f, FontStyle.Regular);

	public static readonly Font Btn = new Font("Segoe UI", 9f, FontStyle.Bold);

	public static Color Alpha(int a)
	{
		return Color.FromArgb(a, 255, 255, 255);
	}

	public static Color Lerp(Color a, Color b, float t)
	{
		if (t < 0f)
		{
			t = 0f;
		}
		if (t > 1f)
		{
			t = 1f;
		}
		return Color.FromArgb((int)((float)(int)a.R + (float)(b.R - a.R) * t), (int)((float)(int)a.G + (float)(b.G - a.G) * t), (int)((float)(int)a.B + (float)(b.B - a.B) * t));
	}
}
