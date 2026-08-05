using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace MeowNetLoader.UI;

internal static class Gfx
{
	public static GraphicsPath Round(Rectangle r, int radius)
	{
		int num = radius * 2;
		if (num > r.Width)
		{
			num = r.Width;
		}
		if (num > r.Height)
		{
			num = r.Height;
		}
		GraphicsPath graphicsPath = new GraphicsPath();
		graphicsPath.AddArc(r.X, r.Y, num, num, 180f, 90f);
		graphicsPath.AddArc(r.Right - num, r.Y, num, num, 270f, 90f);
		graphicsPath.AddArc(r.Right - num, r.Bottom - num, num, num, 0f, 90f);
		graphicsPath.AddArc(r.X, r.Bottom - num, num, num, 90f, 90f);
		graphicsPath.CloseFigure();
		return graphicsPath;
	}

	public static GraphicsPath RoundF(RectangleF r, float radius)
	{
		float num = radius * 2f;
		GraphicsPath graphicsPath = new GraphicsPath();
		graphicsPath.AddArc(r.X, r.Y, num, num, 180f, 90f);
		graphicsPath.AddArc(r.Right - num, r.Y, num, num, 270f, 90f);
		graphicsPath.AddArc(r.Right - num, r.Bottom - num, num, num, 0f, 90f);
		graphicsPath.AddArc(r.X, r.Bottom - num, num, num, 90f, 90f);
		graphicsPath.CloseFigure();
		return graphicsPath;
	}

	public static void Monitor(Graphics g, Rectangle b, Pen p)
	{
		Rectangle r = new Rectangle(b.X + 8, b.Y + 11, b.Width - 16, b.Height - 26);
		using (GraphicsPath path = Round(r, 4))
		{
			g.DrawPath(p, path);
		}
		int num = b.X + b.Width / 2;
		g.DrawLine(p, num, r.Bottom, num, r.Bottom + 7);
		g.DrawLine(p, num - 9, r.Bottom + 7, num + 9, r.Bottom + 7);
	}

	public static void DrawFit(Graphics g, Image img, Rectangle box)
	{
		try
		{
			float num = img.Width;
			float num2 = img.Height;
			if (!(num <= 0f) && !(num2 <= 0f))
			{
				float num3 = Math.Min((float)box.Width / num, (float)box.Height / num2);
				float num4 = num * num3;
				float num5 = num2 * num3;
				float x = (float)box.X + ((float)box.Width - num4) / 2f;
				float y = (float)box.Y + ((float)box.Height - num5) / 2f;
				InterpolationMode interpolationMode = g.InterpolationMode;
				g.InterpolationMode = InterpolationMode.HighQualityBicubic;
				g.DrawImage(img, x, y, num4, num5);
				g.InterpolationMode = interpolationMode;
			}
		}
		catch
		{
		}
	}

	public static void Vr(Graphics g, Rectangle b, Pen p)
	{
		float num = b.Width;
		float num2 = b.Height;
		float num3 = num * 0.82f;
		float num4 = num2 * 0.5f;
		float num5 = (float)b.X + (num - num3) / 2f;
		float num6 = (float)b.Y + num2 * 0.26f;
		float num7 = num4 * 0.42f;
		float num8 = num6 + num4;
		float num9 = (float)b.X + num / 2f;
		float num10 = num * 0.24f;
		float num11 = num4 * 0.3f;
		using (GraphicsPath graphicsPath = new GraphicsPath())
		{
			graphicsPath.AddArc(num5, num6, num7, num7, 180f, 90f);
			graphicsPath.AddArc(num5 + num3 - num7, num6, num7, num7, 270f, 90f);
			graphicsPath.AddArc(num5 + num3 - num7, num8 - num7, num7, num7, 0f, 90f);
			graphicsPath.AddBezier(num9 + num10 / 2f, num8, num9 + num10 * 0.3f, num8 - num11, num9 - num10 * 0.3f, num8 - num11, num9 - num10 / 2f, num8);
			graphicsPath.AddArc(num5, num8 - num7, num7, num7, 90f, 90f);
			graphicsPath.CloseFigure();
			g.DrawPath(p, graphicsPath);
		}
		float num12 = num3 * 0.3f;
		float height = num4 * 0.4f;
		float y = num6 + num4 * 0.2f;
		float num13 = num * 0.05f;
		g.DrawEllipse(p, num9 - num13 - num12, y, num12, height);
		g.DrawEllipse(p, num9 + num13, y, num12, height);
	}

	public static void Cat(Graphics g, Rectangle b, Pen p)
	{
		float num = b.Width;
		float num2 = b.Height;
		float num3 = b.X;
		float num4 = b.Y;
		RectangleF r = new RectangleF(num3 + 0.3f * num, num4 + 0.4f * num2, 0.4f * num, 0.34f * num2);
		using (GraphicsPath path = RoundF(r, 0.07f * num))
		{
			g.DrawPath(p, path);
		}
		g.DrawLines(p, new PointF[3]
		{
			new PointF(r.Left + 0.03f * num, r.Top + 0.02f * num2),
			new PointF(r.Left - 0.01f * num, num4 + 0.22f * num2),
			new PointF(r.Left + 0.14f * num, r.Top - 0.01f * num2)
		});
		g.DrawLines(p, new PointF[3]
		{
			new PointF(r.Right - 0.03f * num, r.Top + 0.02f * num2),
			new PointF(r.Right + 0.01f * num, num4 + 0.22f * num2),
			new PointF(r.Right - 0.14f * num, r.Top - 0.01f * num2)
		});
		float num5 = r.Top + 0.13f * num2;
		g.DrawLine(p, r.Left + 0.11f * num, num5, r.Left + 0.11f * num, num5 + 0.07f * num2);
		g.DrawLine(p, r.Right - 0.11f * num, num5, r.Right - 0.11f * num, num5 + 0.07f * num2);
		float y = r.Top + 0.26f * num2;
		g.DrawArc(p, r.Left + 0.3f * num, y, 0.18f * num, 0.11f * num2, 20f, 140f);
		float num6 = r.Top + 0.16f * num2;
		g.DrawLine(p, num3 + 0.14f * num, num6, r.Left + 0.02f * num, num6 + 0.01f * num2);
		g.DrawLine(p, num3 + 0.86f * num, num6, r.Right - 0.02f * num, num6 + 0.01f * num2);
	}
}
