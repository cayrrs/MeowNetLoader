using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MeowNetLoader.UI;

internal sealed class ToggleSwitch : Panel
{
	public Action<bool> Changed;

	public int GradientTop = -1;

	public int GradientSpan = -1;

	private bool hover;

	private float knobT;

	private readonly Timer animTimer;

	public bool Checked { get; private set; }

	public ToggleSwitch()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		Cursor = Cursors.Hand;
		base.Size = new Size(52, 28);
		base.MouseEnter += delegate
		{
			hover = true;
			Invalidate();
		};
		base.MouseLeave += delegate
		{
			hover = false;
			Invalidate();
		};
		base.MouseClick += delegate
		{
			SetChecked(!Checked, notify: true);
		};
		animTimer = new Timer
		{
			Interval = 15
		};
		animTimer.Tick += AnimTick;
	}

	public void SetChecked(bool value)
	{
		Checked = value;
		knobT = (value ? 1f : 0f);
		Invalidate();
	}

	private void SetChecked(bool value, bool notify)
	{
		Checked = value;
		animTimer.Start();
		if (notify)
		{
			Changed?.Invoke(Checked);
		}
	}

	private void AnimTick(object sender, EventArgs e)
	{
		float num = (Checked ? 1f : 0f);
		knobT += (num - knobT) * 0.35f;
		if (Math.Abs(num - knobT) < 0.01f)
		{
			knobT = num;
			animTimer.Stop();
		}
		Invalidate();
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		int num = ((GradientSpan > 0) ? GradientSpan : ((base.Parent != null && base.Parent.Height > 0) ? base.Parent.Height : 305));
		int num2 = ((GradientTop >= 0) ? GradientTop : base.Top);
		Color color = Theme.Lerp(Theme.RedTop, Theme.RedBot, (float)num2 / (float)num);
		Color color2 = Theme.Lerp(Theme.RedTop, Theme.RedBot, (float)(num2 + base.Height) / (float)num);
		using (LinearGradientBrush brush = new LinearGradientBrush(new Rectangle(0, -1, base.Width, base.Height + 2), color, color2, LinearGradientMode.Vertical))
		{
			graphics.FillRectangle(brush, base.ClientRectangle);
		}
		using (GraphicsPath path = Gfx.Round(new Rectangle(0, 0, base.Width - 1, base.Height - 1), base.Height / 2))
		{
			using (Brush brush2 = new SolidBrush(LerpAlpha(Theme.Alpha(hover ? 55 : 34), Color.White, knobT)))
			{
				graphics.FillPath(brush2, path);
			}
			using Pen pen = new Pen(Theme.Alpha(hover ? 200 : 110), 1.3f);
			graphics.DrawPath(pen, path);
		}
		float num3 = 3f;
		float num4 = (float)base.Height - num3 * 2f;
		float num5 = (float)base.Width - num4 - num3 * 2f;
		float x = num3 + num5 * knobT;
		RectangleF rect = new RectangleF(x, num3, num4, num4);
		using Brush brush3 = new SolidBrush(Theme.Lerp(Color.White, Theme.Accent, knobT));
		graphics.FillEllipse(brush3, rect);
	}

	private static Color LerpAlpha(Color a, Color b, float t)
	{
		if (t < 0f)
		{
			t = 0f;
		}
		if (t > 1f)
		{
			t = 1f;
		}
		return Color.FromArgb((int)((float)(int)a.A + (float)(b.A - a.A) * t), (int)((float)(int)a.R + (float)(b.R - a.R) * t), (int)((float)(int)a.G + (float)(b.G - a.G) * t), (int)((float)(int)a.B + (float)(b.B - a.B) * t));
	}
}
