using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MeowNetLoader.UI;

internal sealed class PillButton : Panel
{
	public bool Primary;

	public bool ShowBadge;

	public Action Clicked;

	private bool hover;

	public PillButton()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		Cursor = Cursors.Hand;
		BackColor = Theme.RedBot;
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
			Clicked?.Invoke();
		};
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		using (SolidBrush brush = new SolidBrush(BackColor))
		{
			graphics.FillRectangle(brush, base.ClientRectangle);
		}
		using (GraphicsPath path = Gfx.Round(new Rectangle(0, 0, base.Width - 1, base.Height - 1), base.Height / 2))
		{
			if (Primary)
			{
				using (Brush brush2 = new SolidBrush(hover ? Color.White : Theme.Alpha(238)))
				{
					graphics.FillPath(brush2, path);
				}
				TextRenderer.DrawText(graphics, Text, Theme.Btn, base.ClientRectangle, Theme.Accent, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
			}
			else
			{
				using (Brush brush3 = new SolidBrush(Theme.Alpha(hover ? 70 : 34)))
				{
					graphics.FillPath(brush3, path);
				}
				using (Pen pen = new Pen(Theme.Alpha(hover ? 220 : 120), 1.3f))
				{
					graphics.DrawPath(pen, path);
				}
				TextRenderer.DrawText(graphics, Text, Theme.Btn, base.ClientRectangle, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
			}
		}
		if (ShowBadge)
		{
			float num = 10f;
			RectangleF rect = new RectangleF((float)base.Width - num - 2f, 2f, num, num);
			using (Brush brush4 = new SolidBrush(Color.FromArgb(255, 76, 76)))
			{
				graphics.FillEllipse(brush4, rect);
			}
			using Pen pen2 = new Pen(Color.White, 1.2f);
			graphics.DrawEllipse(pen2, rect);
		}
	}
}
