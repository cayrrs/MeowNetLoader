using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MeowNetLoader.UI;

internal sealed class RoundedCard : Panel
{
	public int IconKind;

	public Image IconImage;

	public string TitleText = string.Empty;

	public string DescText = string.Empty;

	public string CtaText = string.Empty;

	public Action Clicked;

	public int GradientTop = -1;

	public int GradientSpan = -1;

	private bool hover;

	public RoundedCard()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		Cursor = Cursors.Hand;
		BackColor = Color.FromArgb(166, 45, 54);
		base.MouseEnter += delegate
		{
			if (!hover)
			{
				hover = true;
				base.Top -= 6;
				Invalidate();
			}
		};
		base.MouseLeave += delegate
		{
			if (hover)
			{
				hover = false;
				base.Top += 6;
				Invalidate();
			}
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
		int num = ((GradientSpan > 0) ? GradientSpan : ((base.Parent != null && base.Parent.Height > 0) ? base.Parent.Height : 410));
		int num2 = ((GradientTop >= 0) ? GradientTop : base.Top);
		Color color = Theme.Lerp(Theme.RedTop, Theme.RedBot, (float)num2 / (float)num);
		Color color2 = Theme.Lerp(Theme.RedTop, Theme.RedBot, (float)(num2 + base.Height) / (float)num);
		using (LinearGradientBrush brush = new LinearGradientBrush(new Rectangle(0, -1, base.Width, base.Height + 2), color, color2, LinearGradientMode.Vertical))
		{
			graphics.FillRectangle(brush, base.ClientRectangle);
		}
		using (GraphicsPath path = Gfx.Round(new Rectangle(0, 0, base.Width - 1, base.Height - 1), 16))
		{
			using (LinearGradientBrush brush2 = new LinearGradientBrush(new Rectangle(0, 0, base.Width, base.Height), hover ? Theme.CardTopHot : Theme.CardTop, hover ? Theme.CardBotHot : Theme.CardBot, LinearGradientMode.Vertical))
			{
				graphics.FillPath(brush2, path);
			}
			using Pen pen = new Pen(Theme.Alpha(hover ? 210 : 70), hover ? 1.7f : 1.3f);
			graphics.DrawPath(pen, path);
		}
		Rectangle rectangle = new Rectangle(24, 22, 60, 60);
		using (GraphicsPath path2 = Gfx.Round(rectangle, 14))
		{
			using (Brush brush3 = new SolidBrush(Theme.Alpha(46)))
			{
				graphics.FillPath(brush3, path2);
			}
			using Pen pen2 = new Pen(Theme.Alpha(82), 1.3f);
			graphics.DrawPath(pen2, path2);
		}
		if (IconImage != null)
		{
			Gfx.DrawFit(graphics, IconImage, Rectangle.Inflate(rectangle, -6, -6));
		}
		else
		{
			using Pen pen3 = new Pen(Color.White, 3f);
			pen3.StartCap = LineCap.Round;
			pen3.EndCap = LineCap.Round;
			pen3.LineJoin = LineJoin.Round;
			if (IconKind == 0)
			{
				Gfx.Monitor(graphics, rectangle, pen3);
			}
			else
			{
				Gfx.Vr(graphics, rectangle, pen3);
			}
		}
		TextRenderer.DrawText(graphics, TitleText, Theme.CardTitle, new Point(23, 98), Color.White, TextFormatFlags.NoPadding);
		TextRenderer.DrawText(bounds: new Rectangle(24, 134, base.Width - 48, 54), dc: graphics, text: DescText, font: Theme.Desc, foreColor: Theme.Alpha(210), flags: TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
		Rectangle rectangle2 = new Rectangle(24, 198, TextRenderer.MeasureText(CtaText, Theme.Cta).Width + 26, 32);
		using GraphicsPath path3 = Gfx.Round(rectangle2, 10);
		if (hover)
		{
			using (Brush brush4 = new SolidBrush(Color.White))
			{
				graphics.FillPath(brush4, path3);
			}
			TextRenderer.DrawText(graphics, CtaText, Theme.Cta, rectangle2, Theme.Accent, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
			return;
		}
		using (Brush brush5 = new SolidBrush(Theme.Alpha(40)))
		{
			graphics.FillPath(brush5, path3);
		}
		using (Pen pen4 = new Pen(Theme.Alpha(90), 1.2f))
		{
			graphics.DrawPath(pen4, path3);
		}
		TextRenderer.DrawText(graphics, CtaText, Theme.Cta, rectangle2, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
	}
}
