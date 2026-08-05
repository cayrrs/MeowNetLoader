using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MeowNetLoader.UI;

internal sealed class OverlayPanel : Panel
{
	private const int BoxWidth = 470;

	private const int MenuItemHeight = 42;

	private const int MenuItemSpacing = 50;

	private const int MenuItemTop = 104;

	private const int TimerInterval = 33;

	private const int SpinnerAngleStep = 14;

	private const int ScanBarTop = 130;

	private const float ScanBlockFrac = 0.28f;

	private const float ScanPosStep = 0.014f;

	private const float ScanCycleLength = 1.28f;

	private string title = string.Empty;

	private string sub = string.Empty;

	private int kind;

	private float progress;

	private int angle;

	private float scanPos;

	private int boxH = 250;

	private readonly List<PillButton> menu = new List<PillButton>();

	private readonly Timer timer;

	private readonly PillButton b1;

	private readonly PillButton b2;

	public OverlayPanel()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		Dock = DockStyle.Fill;
		base.Visible = false;
		BackColor = Theme.Dim;
		timer = new Timer
		{
			Interval = 33
		};
		timer.Tick += delegate
		{
			if (kind == 4)
			{
				scanPos += 0.014f;
				if (scanPos > 1.28f)
				{
					scanPos -= 1.28f;
				}
				InvalidateScanBar();
			}
			else
			{
				angle = (angle + 14) % 360;
				InvalidateSpinner();
			}
		};
		b1 = new PillButton
		{
			Primary = true,
			Size = new Size(160, 40),
			Visible = false,
			BackColor = Color.FromArgb(166, 45, 54)
		};
		base.Controls.Add(b1);
		b2 = new PillButton
		{
			Primary = false,
			Size = new Size(160, 40),
			Visible = false,
			BackColor = Color.FromArgb(166, 45, 54)
		};
		base.Controls.Add(b2);
		base.SizeChanged += delegate
		{
			Reposition();
			LayoutMenu();
		};
	}

	private Rectangle Box()
	{
		return new Rectangle((base.Width - 470) / 2, (base.Height - boxH) / 2, 470, boxH);
	}

	private static int ComputeChoiceHeight(string s)
	{
		int width = 410;
		int val = 80 + TextRenderer.MeasureText(s ?? string.Empty, Theme.OvSub, new Size(width, int.MaxValue), TextFormatFlags.WordBreak).Height + 20 + 64 + 40;
		return Math.Max(250, val);
	}

	private void InvalidateSpinner()
	{
		Rectangle rectangle = Box();
		Invalidate(new Rectangle(rectangle.X + rectangle.Width / 2 - 30, rectangle.Y + 46, 60, 58));
	}

	private void InvalidateScanBar()
	{
		Rectangle rectangle = Box();
		Invalidate(new Rectangle(rectangle.X + 40, rectangle.Y + 130 - 4, rectangle.Width - 80, 20));
	}

	public void ShowSpinner(string t, string s)
	{
		boxH = 250;
		ClearMenu();
		title = t;
		sub = s;
		kind = 1;
		angle = 0;
		b1.Visible = false;
		b2.Visible = false;
		base.Visible = true;
		BringToFront();
		timer.Start();
		Invalidate();
	}

	public void ShowScanProgress(string t, string s, string cancelLabel, Action onCancel)
	{
		boxH = 300;
		ClearMenu();
		title = t;
		sub = s;
		kind = 4;
		scanPos = 0f;
		b1.Visible = false;
		b2.Text = cancelLabel;
		b2.Clicked = onCancel;
		b2.Visible = true;
		Reposition();
		base.Visible = true;
		BringToFront();
		timer.Start();
		Invalidate();
	}

	public void ShowProgress(string t, string s, float val)
	{
		boxH = 250;
		ClearMenu();
		title = t;
		sub = s;
		kind = 2;
		progress = val;
		b1.Visible = false;
		b2.Visible = false;
		timer.Stop();
		base.Visible = true;
		BringToFront();
		Invalidate();
	}

	public void UpdateProgress(string s, float val)
	{
		sub = s;
		progress = val;
		if (base.Visible && kind == 2)
		{
			Invalidate(Box());
		}
	}

	public void ShowChoice(string t, string s, string p1, Action a1, string p2, Action a2)
	{
		boxH = ComputeChoiceHeight(s);
		ClearMenu();
		title = t;
		sub = s;
		kind = 0;
		timer.Stop();
		if (p1 != null)
		{
			b1.Text = p1;
			b1.Clicked = a1;
			b1.Visible = true;
		}
		else
		{
			b1.Visible = false;
		}
		if (p2 != null)
		{
			b2.Text = p2;
			b2.Clicked = a2;
			b2.Visible = true;
		}
		else
		{
			b2.Visible = false;
		}
		Reposition();
		base.Visible = true;
		BringToFront();
		Invalidate();
	}

	public void ShowMessage(string t, string s, string p1, Action a1)
	{
		ShowChoice(t, s, p1, a1, null, null);
	}

	public void HideOverlay()
	{
		timer.Stop();
		ClearMenu();
		base.Visible = false;
	}

	public void ShowMenu(string t, string s, string[] labels, Action<int> onPick)
	{
		boxH = 130 + labels.Length * 50;
		ClearMenu();
		title = t;
		sub = s;
		kind = 3;
		timer.Stop();
		b1.Visible = false;
		b2.Visible = false;
		for (int i = 0; i < labels.Length; i++)
		{
			int idx = i;
			PillButton pillButton = new PillButton
			{
				Primary = false,
				Text = labels[i],
				Size = new Size(360, 42),
				BackColor = Theme.CardBot
			};
			pillButton.Clicked = delegate
			{
				onPick(idx);
			};
			menu.Add(pillButton);
			base.Controls.Add(pillButton);
		}
		LayoutMenu();
		base.Visible = true;
		BringToFront();
		Invalidate();
	}

	private void LayoutMenu()
	{
		Rectangle rectangle = Box();
		int num = rectangle.Y + 104;
		foreach (PillButton item in menu)
		{
			item.Location = new Point(rectangle.X + 55, num);
			num += 50;
		}
	}

	private void ClearMenu()
	{
		for (int i = 0; i < menu.Count; i++)
		{
			base.Controls.Remove(menu[i]);
			menu[i].Dispose();
		}
		menu.Clear();
	}

	private void Reposition()
	{
		Rectangle rectangle = Box();
		int y = rectangle.Bottom - 64;
		int num = rectangle.X + rectangle.Width / 2;
		if (b1.Visible && b2.Visible)
		{
			b1.Location = new Point(num - 166, y);
			b2.Location = new Point(num + 6, y);
		}
		else if (b1.Visible)
		{
			b1.Location = new Point(num - 80, y);
		}
		else if (b2.Visible)
		{
			b2.Location = new Point(num - 80, y);
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		using (Brush brush = new SolidBrush(Theme.Dim))
		{
			graphics.FillRectangle(brush, base.ClientRectangle);
		}
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		Rectangle rectangle = Box();
		using (GraphicsPath path = Gfx.Round(rectangle, 22))
		{
			if (kind == 3)
			{
				using Brush brush2 = new SolidBrush(Theme.CardBot);
				graphics.FillPath(brush2, path);
			}
			else
			{
				using LinearGradientBrush brush3 = new LinearGradientBrush(rectangle, Theme.RedTop, Theme.RedBot, LinearGradientMode.Vertical);
				graphics.FillPath(brush3, path);
			}
			using Pen pen = new Pen(Theme.Alpha(80), 1.3f);
			graphics.DrawPath(pen, path);
		}
		if (kind == 1)
		{
			int x = rectangle.X + rectangle.Width / 2 - 23;
			int y = rectangle.Y + 50;
			using (Pen pen2 = new Pen(Theme.Alpha(60), 5f))
			{
				graphics.DrawEllipse(pen2, x, y, 46, 46);
			}
			using (Pen pen3 = new Pen(Color.White, 5f))
			{
				pen3.StartCap = LineCap.Round;
				pen3.EndCap = LineCap.Round;
				graphics.DrawArc(pen3, x, y, 46, 46, angle, 90);
			}
			Centered(graphics, title, Theme.OvTitle, rectangle.X + 20, rectangle.Y + 116, rectangle.Width - 40, 30, Color.White, wrap: false);
			Centered(graphics, sub, Theme.OvSub, rectangle.X + 30, rectangle.Y + 148, rectangle.Width - 60, 44, Theme.Alpha(220), wrap: true);
			return;
		}
		if (kind == 2)
		{
			Centered(graphics, title, Theme.OvTitle, rectangle.X + 20, rectangle.Y + 44, rectangle.Width - 40, 30, Color.White, wrap: false);
			Centered(graphics, sub, Theme.OvSub, rectangle.X + 30, rectangle.Y + 80, rectangle.Width - 60, 24, Theme.Alpha(225), wrap: false);
			Rectangle r = new Rectangle(rectangle.X + 46, rectangle.Y + 120, rectangle.Width - 92, 12);
			using (GraphicsPath path2 = Gfx.Round(r, 6))
			{
				using Brush brush4 = new SolidBrush(Theme.Alpha(38));
				graphics.FillPath(brush4, path2);
			}
			float num = progress;
			if (num < 0f)
			{
				num = 0f;
			}
			if (num > 1f)
			{
				num = 1f;
			}
			int num2 = (int)((float)r.Width * num);
			if (num2 >= 4)
			{
				using GraphicsPath path3 = Gfx.Round(new Rectangle(r.X, r.Y, num2, 12), 6);
				using Brush brush5 = new SolidBrush(Color.White);
				graphics.FillPath(brush5, path3);
			}
			Centered(graphics, (int)(num * 100f) + "%", Theme.OvSub, rectangle.X + 30, rectangle.Y + 142, rectangle.Width - 60, 22, Theme.Alpha(185), wrap: false);
			return;
		}
		if (kind == 4)
		{
			Centered(graphics, title, Theme.OvTitle, rectangle.X + 20, rectangle.Y + 34, rectangle.Width - 40, 30, Color.White, wrap: false);
			Centered(graphics, sub, Theme.OvSub, rectangle.X + 30, rectangle.Y + 70, rectangle.Width - 60, 44, Theme.Alpha(220), wrap: true);
			Rectangle r2 = new Rectangle(rectangle.X + 40, rectangle.Y + 130, rectangle.Width - 80, 12);
			using GraphicsPath graphicsPath = Gfx.Round(r2, 6);
			using (Brush brush6 = new SolidBrush(Theme.Alpha(38)))
			{
				graphics.FillPath(brush6, graphicsPath);
			}
			GraphicsState gstate = graphics.Save();
			graphics.SetClip(graphicsPath);
			float width = 0.28f * (float)r2.Width;
			float x2 = (float)r2.X + (scanPos - 0.28f) * (float)r2.Width;
			using (Brush brush7 = new SolidBrush(Color.White))
			{
				graphics.FillRectangle(brush7, x2, r2.Y, width, r2.Height);
			}
			graphics.Restore(gstate);
			return;
		}
		Centered(graphics, title, Theme.OvTitle, rectangle.X + 20, rectangle.Y + 44, rectangle.Width - 40, 30, Color.White, wrap: false);
		int h = Math.Max(46, rectangle.Height - 164);
		Centered(graphics, sub, Theme.OvSub, rectangle.X + 30, rectangle.Y + 80, rectangle.Width - 60, h, Theme.Alpha(222), wrap: true);
	}

	private static void Centered(Graphics g, string text, Font f, int x, int y, int w, int h, Color c, bool wrap)
	{
		TextFormatFlags textFormatFlags = TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding;
		if (wrap)
		{
			textFormatFlags |= TextFormatFlags.WordBreak;
		}
		TextRenderer.DrawText(g, text, f, new Rectangle(x, y, w, h), c, textFormatFlags);
	}
}
