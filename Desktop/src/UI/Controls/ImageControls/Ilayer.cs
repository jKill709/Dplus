using System.Drawing.Drawing2D;

namespace Dplus_Desktop.UI.Controls.ImageControls
{
    public interface ILayer
    {
        bool Visible { get; set; }
        void Render(Graphics g, Matrix imageTransform);
    }
}
