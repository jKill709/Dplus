using System.Drawing.Drawing2D;

namespace Dplus_Desktop.UI.Controls.ImageControls
{
    public class PrimitiveOverlayLayer : OverlayLayer
    {
        public readonly List<PointOverlay> Points = new();
        public readonly List<LineOverlay> Lines = new();
        public readonly List<PolygonOverlay> Polygons = new();
        public readonly List<TextOverlay> Texts = new();

        public override void Render(Graphics g, Matrix imageTransform)
        {
            if (!Visible)
                return;

            var old = g.Transform;
            g.Transform = imageTransform;

            foreach (var p in Points)
                p.Draw(g);

            foreach (var l in Lines)
                l.Draw(g);

            foreach (var poly in Polygons)
                poly.Draw(g);

            foreach (var t in Texts)
                t.Draw(g);

            g.Transform = old;
        }

        // Clears all overlay elements
        public void ClearLayers()
        {
            Points.Clear();
            Lines.Clear();
            Polygons.Clear();
            Texts.Clear();
        }
    }
}
