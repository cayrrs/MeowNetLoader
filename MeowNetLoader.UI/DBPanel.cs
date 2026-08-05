using System.Windows.Forms;

namespace MeowNetLoader.UI;

internal sealed class DBPanel : Panel
{
	public DBPanel()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
	}
}
