using System.Drawing.Drawing2D;

namespace Dplus_Desktop.UI.Controls.ImageControls
{
    public abstract class OverlayLayer : ILayer
    {
        public bool Visible { get; set; } = true;
        public abstract void Render(Graphics g, Matrix imageTransform);
    }
}
