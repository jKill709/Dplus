using Dplus_Desktop.Config;
using System.Drawing.Drawing2D;

namespace Dplus_Desktop.UI.Controls.ImageControls
{
    public class OrthographicViewer : Control
    {
        private float _zoom = 1f;
        private PointF _pan = new PointF(0, 0);
        private bool _panning;
        private Point _lastMouse;

        private readonly List<(PointF, Color)> _points = new();
        private readonly List<(PointF, PointF, Color)> _lines = new();
        private readonly List<(string, PointF)> _labels = new();

        public bool ShowGrid { get; set; } = true;
        public bool ShowAxes { get; set; } = true;
        public bool ShowDebug { get; set; } = false;

        public float GridSpacing { get; set; } = 10f;

        OrthographicViewerSettings settings;

        public OrthographicViewer()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            settings = Settings.All.GetOrthographicViewerSettings();
            GridSpacing = settings.GridSpacing;
        }

        #region Data API
        public void Clear()
        {
            _points.Clear();
            _lines.Clear();
            _labels.Clear();
            Invalidate();
        }
        public void AddPoint(float x, float y, Color color)
        {
            _points.Add((new PointF(x, y), color));
        }
        public void AddLine(PointF p1, PointF p2, Color color)
        {
            _lines.Add((p1, p2, color));
        }
        public void AddLabel(string text, PointF location)
        {
            _labels.Add((text, location));
        }
        #endregion

        #region Rendering
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            base.OnPaint(e);

            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (ShowGrid)
                DrawGrid(g);

            if (ShowAxes)
                DrawAxes(g);

            foreach (var p in _points)
            {
                var screen = WorldToScreen(p.Item1);

                float r = 5f; // screen space radius now
                g.FillEllipse(new SolidBrush(p.Item2),
                    screen.X - r, screen.Y - r, r * 2, r * 2);
            }

            foreach (var l in _lines)
            {
                var p1 = WorldToScreen(l.Item1);
                var p2 = WorldToScreen(l.Item2);

                g.DrawLine(new Pen(l.Item3), p1, p2);
            }

            if (ShowDebug)
                DrawDebug(g);
        }
        private void DrawDebug(Graphics g)
        {
            DrawText(g, _zoom.ToString(), new PointF(0, 0), new Font(SystemFonts.DefaultFont.FontFamily, 8f), Color.Green, Color.White);
            DrawText(g, _pan.ToString(), new PointF(0, 40), new Font(SystemFonts.DefaultFont.FontFamily, 8f), Color.Green, Color.White);
            DrawText(g, ScreenToWorld(new PointF(0, 0)).ToString(), new PointF(0, 60), new Font(SystemFonts.DefaultFont.FontFamily, 8f), Color.Green, Color.White);
        }
        private void DrawText(Graphics g, string text, PointF location, Font font, Color bgColor, Color fgColor)
        {
            //Output Debug Text
            float Padding = 2f;
            var size = g.MeasureString(text, font);
            var bgRect = new RectangleF(
                location.X - Padding,
                location.Y - Padding,
                size.Width + Padding * 2,
                size.Height + Padding * 2);

            g.FillRectangle(new SolidBrush(bgColor), bgRect);

            g.DrawString(text, font, new SolidBrush(fgColor), location);
        }
        private void DrawGrid(Graphics g)
        {
            int majorStep = settings.MaxGridSpacing / settings.MinGridSpacing;

            float worldSpacing = settings.GridSpacing;
            float screenSpacing = worldSpacing * _zoom;

            // Adjust spacing LOCALLY
            while (screenSpacing < settings.MinGridSpacing)
            {
                worldSpacing *= majorStep;
                screenSpacing = worldSpacing * _zoom;
            }

            while (screenSpacing > settings.MaxGridSpacing)
            {
                worldSpacing /= majorStep;
                screenSpacing = worldSpacing * _zoom;
            }

            int minValue = 0;   //Blackest Black (0-255)
            int maxValue = 239; //Lightest Grey (0-255)

            int minorGridLineGreyness = (int)(maxValue - ((maxValue - minValue) * (screenSpacing - settings.MinGridSpacing) / (settings.MaxGridSpacing - settings.MinGridSpacing)));
            Color color = Color.FromArgb(minorGridLineGreyness, minorGridLineGreyness, minorGridLineGreyness);

            // Determine world bounds currently visible
            var topLeft = ScreenToWorld(new PointF(0, 0));
            var bottomRight = ScreenToWorld(new PointF(ClientSize.Width, ClientSize.Height));

            float minX = Math.Min(topLeft.X, bottomRight.X);
            float maxX = Math.Max(topLeft.X, bottomRight.X);
            float minY = Math.Min(topLeft.Y, bottomRight.Y);
            float maxY = Math.Max(topLeft.Y, bottomRight.Y);

            // Snap starting positions to grid
            float startX = (float)Math.Floor(minX / worldSpacing) * worldSpacing;
            float startY = (float)Math.Floor(minY / worldSpacing) * worldSpacing;

            // Vertical grid lines
            float majorSpacing = worldSpacing * majorStep;

            for (float x = startX; x <= maxX; x += worldSpacing)
            {
                bool major = Math.Abs(x % majorSpacing) < 0.0001f;
                var pen = major ? Pens.Black : new Pen(color);

                var p1 = WorldToScreen(new PointF(x, minY));
                var p2 = WorldToScreen(new PointF(x, maxY));
                g.DrawLine(pen, p1, p2);
            }

            // Horizontal grid lines
            for (float y = startY; y <= maxY; y += worldSpacing)
            {
                bool major = Math.Abs(y % majorSpacing) < 0.0001f;
                var pen = major ? Pens.Black : new Pen(color);

                var p1 = WorldToScreen(new PointF(minX, y));
                var p2 = WorldToScreen(new PointF(maxX, y));
                g.DrawLine(pen, p1, p2);
            }

            //Output Debug Text
            if (ShowDebug)
                DrawText(g, screenSpacing.ToString("0.##"), new PointF(0, 20), new Font(SystemFonts.DefaultFont.FontFamily, 8f), Color.Green, Color.White);
        }
        private void DrawAxes(Graphics g)
        {
            var origin = WorldToScreen(new PointF(0, 0));
            //var oppositeCorner = WorldToScreen(new PointF(1, 1));
            using var xPen = new Pen(Color.Red, 3f);     // 3-pixel wide
            using var yPen = new Pen(Color.Green, 3f);

            g.DrawLine(xPen, 0, origin.Y, ClientSize.Width, origin.Y);     //oppositeCorner.X, origin.Y);
            g.DrawLine(yPen, origin.X, 0, origin.X, ClientSize.Height);  //origin.X, oppositeCorner.Y);
        }
        #endregion

        #region Coordinate Helpers
        public void AutoFit()
        {
            //MessageBox.Show($"[{logName}] Auto-fitting...");
            if (_points.Count == 0 && _lines.Count == 0 && _labels.Count == 0)
                return;

            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            foreach (var (point, color) in _points)
            {
                if (point.X < minX) minX = point.X;
                if (point.Y < minY) minY = point.Y;
                if (point.X > maxX) maxX = point.X;
                if (point.Y > maxY) maxY = point.Y;
            }

            float width = maxX - minX;
            float height = maxY - minY;

            //if (width <= 0 || height <= 0)
            //    return;
            const float MinExtent = 0.01f;

            if (width < MinExtent)
            {
                float center = (minX + maxX) * 0.5f;
                minX = center - MinExtent * 0.5f;
                maxX = center + MinExtent * 0.5f;
                width = MinExtent;
            }

            if (height < MinExtent)
            {
                float center = (minY + maxY) * 0.5f;
                minY = center - MinExtent * 0.5f;
                maxY = center + MinExtent * 0.5f;
                height = MinExtent;
            }

            float zoomX = ClientSize.Width / width;
            float zoomY = ClientSize.Height / height;

            _zoom = Math.Min(zoomX, zoomY) * 0.9f;
            _zoom = Math.Clamp(_zoom, 0.001f, 10000f);

            float cx = (minX + maxX) / 2f;
            float cy = (minY + maxY) / 2f;

            _pan = new PointF(
                ClientSize.Width / 2f - cx * _zoom,
                ClientSize.Height / 2f - cy * _zoom);

            //MessageBox.Show($"[{logName}] Auto-fitted to bounds: ({minX}, {minY}) - ({maxX}, {maxY})");

            Invalidate();

            //MessageBox.Show($"[{logName}] Auto-fitted.");
        }
        public PointF WorldToScreen(PointF p)
        {
            float x = p.X * _zoom + _pan.X;
            float y = ClientSize.Height - (p.Y * _zoom + _pan.Y);

            if (float.IsNaN(x) || float.IsInfinity(x))
                x = 0;

            if (float.IsNaN(y) || float.IsInfinity(y))
                y = 0;

            return new PointF(x, y);
        }
        public PointF ScreenToWorld(PointF p)
        {
            float x = (p.X - _pan.X) / _zoom;
            //float y = (p.Y - _pan.Y) / _zoom;
            float y = ((ClientSize.Height - p.Y) - _pan.Y) / _zoom;
            return new PointF(x, y);
        }
        #endregion

        #region Mouse
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button == MouseButtons.Right || e.Button == MouseButtons.Middle)
            {
                _panning = true;
                _lastMouse = e.Location;
                Cursor = Cursors.Hand;
            }
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _panning = false;
            Cursor = Cursors.Default;
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_panning)
            {
                _pan.X += e.X - _lastMouse.X;
                _pan.Y -= e.Y - _lastMouse.Y;

                _lastMouse = e.Location;
                Invalidate();
            }
        }
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            float factor = e.Delta > 0 ? 1.05f : 0.95f;

            // World position under cursor BEFORE zoom
            PointF worldBefore = ScreenToWorld(e.Location);

            // Apply zoom
            _zoom *= factor;
            _zoom = Math.Clamp(_zoom, 0.0001f, 1000f);

            // Recompute pan so that worldBefore stays under cursor
            //_pan = new PointF(e.X - worldBefore.X * _zoom, e.Y - worldBefore.Y * _zoom);
            _pan = new PointF(e.X - worldBefore.X * _zoom, (ClientSize.Height - e.Y) - worldBefore.Y * _zoom
);

            Invalidate();
        }
        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);

            AutoFit();
        }
        #endregion
    }
}
