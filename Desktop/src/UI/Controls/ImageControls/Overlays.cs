namespace Dplus_Desktop.UI.Controls.ImageControls
{
    public class PointOverlay
    {
        public PointF Location;
        public float Radius = 4f;
        public Pen? Pen;
        public Brush Brush = Brushes.Lime;

        public void Draw(Graphics g)
        {
            float r = Radius;
            var rect = new RectangleF(Location.X - r, Location.Y - r, r * 2, r * 2);

            if (Brush != null)
                g.FillEllipse(Brush, rect);

            if (Pen != null)
                g.DrawEllipse(Pen, rect);
        }
    }
    public class LineOverlay
    {
        public PointF P1;
        public PointF P2;
        public Pen Pen = Pens.Red;

        public void Draw(Graphics g)
        {
            g.DrawLine(Pen, P1, P2);
        }
    }
    public class PolygonOverlay
    {
        public List<PointF> Points = new();
        public Pen? Pen = Pens.Yellow;
        public Brush? Brush = null;

        public void Draw(Graphics g)
        {
            if (Points.Count < 2)
                return;

            if (Brush != null && Points.Count >= 3)
                g.FillPolygon(Brush, Points.ToArray());

            if (Pen != null)
                g.DrawPolygon(Pen, Points.ToArray());
        }
    }
    public class TextOverlay
    {
        public string Text = string.Empty;
        public PointF Location;
        public Font Font = SystemFonts.DefaultFont;
        public Brush Brush = Brushes.White;
        public bool DrawBackground = false;
        public Brush BackgroundBrush = Brushes.Black;
        public float Padding = 2f;

        public void Draw(Graphics g)
        {
            if (DrawBackground)
            {
                var size = g.MeasureString(Text, Font);
                var bgRect = new RectangleF(
                    Location.X - Padding,
                    Location.Y - Padding,
                    size.Width + Padding * 2,
                    size.Height + Padding * 2);

                g.FillRectangle(BackgroundBrush, bgRect);
            }

            g.DrawString(Text, Font, Brush, Location);
        }
    }
}
