
using System.Drawing.Drawing2D;

namespace Dplus_Desktop.UI.Controls.ImageControls
{
    public class ImageControls : Control
    {
        private Image? _image;
        private float _zoom = 1.0f;
        private PointF _panOffset = new PointF(0, 0);
        private bool _panning;
        private Point _lastMouse;

        private readonly List<ILayer> _layers = new();

        public IReadOnlyList<ILayer> Layers => _layers;

        public ImageControls()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
        }

        #region Public API

        public Image? DisplayedImage
        {
            get => _image;
            set
            {
                _image = value;
                ZoomToFit();
                Invalidate();
            }
        }

        public void AddLayer(ILayer layer)
        {
            _layers.Add(layer);
            Invalidate();
        }

        public void RemoveLayer(ILayer layer)
        {
            _layers.Remove(layer);
            Invalidate();
        }

        public void ClearLayers()
        {
            _layers.Clear();
            Invalidate();
        }

        public void ResetView()
        {
            _zoom = 1.0f;
            _panOffset = new PointF(0, 0);
            Invalidate();
        }

        public void ZoomToFit()
        {
            if (_image == null || ClientSize.Width == 0 || ClientSize.Height == 0)
                return;

            float ratioX = (float)ClientSize.Width / _image.Width;
            float ratioY = (float)ClientSize.Height / _image.Height;
            _zoom = Math.Min(ratioX, ratioY);

            float imageWidth = _image.Width * _zoom;
            float imageHeight = _image.Height * _zoom;

            _panOffset = new PointF(
                (ClientSize.Width - imageWidth) / 2f,
                (ClientSize.Height - imageHeight) / 2f);

            Invalidate();
        }

        public PointF ScreenToImage(PointF screenPoint)
        {
            using var matrix = GetImageTransform();
            matrix.Invert();
            var pts = new[] { screenPoint };
            matrix.TransformPoints(pts);
            return pts[0];
        }

        public PointF ImageToScreen(PointF imagePoint)
        {
            using var matrix = GetImageTransform();
            var pts = new[] { imagePoint };
            matrix.TransformPoints(pts);
            return pts[0];
        }

        #endregion

        #region Rendering

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            if (_image != null)
            {
                using var transform = GetImageTransform();
                e.Graphics.Transform = transform;
                e.Graphics.DrawImage(_image, new RectangleF(0, 0, _image.Width, _image.Height));
                e.Graphics.ResetTransform();
            }

            using var overlayTransform = GetImageTransform();
            foreach (var layer in _layers)
            {
                if (layer.Visible)
                    layer.Render(e.Graphics, overlayTransform);
            }
        }

        private Matrix GetImageTransform()
        {
            var matrix = new Matrix();
            matrix.Translate(_panOffset.X, _panOffset.Y);
            matrix.Scale(_zoom, _zoom);
            return matrix;
        }

        #endregion

        #region Mouse Interaction

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button == MouseButtons.Middle || e.Button == MouseButtons.Right)
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
                float dx = e.X - _lastMouse.X;
                float dy = e.Y - _lastMouse.Y;
                _panOffset = new PointF(_panOffset.X + dx, _panOffset.Y + dy);
                _lastMouse = e.Location;
                Invalidate();
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            if (_image == null)
                return;

            float zoomFactor = e.Delta > 0 ? 1.1f : 0.9f;
            var imageBeforeZoom = ScreenToImage(e.Location);

            _zoom *= zoomFactor;
            _zoom = Math.Max(0.01f, Math.Min(100f, _zoom));

            var screenAfterZoom = ImageToScreen(imageBeforeZoom);

            _panOffset = new PointF(
                _panOffset.X + (e.X - screenAfterZoom.X),
                _panOffset.Y + (e.Y - screenAfterZoom.Y));

            Invalidate();
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            ZoomToFit();
        }

        #endregion
    }
}
