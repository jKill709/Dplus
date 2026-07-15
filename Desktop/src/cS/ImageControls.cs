using OpenCvSharp.Dnn;
using OpenCvSharp.Flann;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Numerics;
using System.Windows.Forms;

namespace Dplus_Desktop
{
    #region Layer Interfaces

    public interface ILayer
    {
        bool Visible { get; set; }
        void Render(Graphics g, Matrix imageTransform);
    }

    public abstract class OverlayLayer : ILayer
    {
        public bool Visible { get; set; } = true;
        public abstract void Render(Graphics g, Matrix imageTransform);
    }

    #endregion

    #region Overlay Primitives

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

    #endregion

    #region Concrete Layer

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

    #endregion

    #region ImageViewer
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
    #endregion

    #region OrthographicViewer
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

            int minorGridLineGreyness = (int)(maxValue - ((maxValue - minValue) *(screenSpacing - settings.MinGridSpacing) / (settings.MaxGridSpacing - settings.MinGridSpacing)));
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
                DrawText(g, screenSpacing.ToString("0.##"), new PointF(0,20), new Font(SystemFonts.DefaultFont.FontFamily, 8f), Color.Green, Color.White);
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
            //MessageBox.Show($"[{Name}] Auto-fitting...");
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

            //MessageBox.Show($"[{Name}] Auto-fitted to bounds: ({minX}, {minY}) - ({maxX}, {maxY})");

            Invalidate();

            //MessageBox.Show($"[{Name}] Auto-fitted.");
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
    #endregion

    #region PerspectiveViewer
    public struct BoundingBox
    {
        public Vector3 Min;
        public Vector3 Max;

        public bool IsValid => Min != Max;
    }

    public sealed class CameraModel
    {
        //-------------------------------------------------------------------------
        // Original calibration
        //-------------------------------------------------------------------------

        public Intrinsics Intrinsics { get; }
        public Extrinsics Extrinsics { get; }

        //-------------------------------------------------------------------------
        // Camera pose (camera -> world)
        //-------------------------------------------------------------------------

        public Matrix4x4 WorldTransform { get; }

        public Matrix4x4 ViewTransform
        {
            get
            {
                Matrix4x4.Invert(WorldTransform, out Matrix4x4 view);
                return view;
            }
        }

        //-------------------------------------------------------------------------
        // Derived orientation
        //-------------------------------------------------------------------------

        public Vector3 Position => new(WorldTransform.M41, WorldTransform.M42, WorldTransform.M43);

        public Vector3 Right => Vector3.Normalize(new Vector3(WorldTransform.M11, WorldTransform.M12, WorldTransform.M13));

        public Vector3 Up => Vector3.Normalize(new Vector3(WorldTransform.M21,WorldTransform.M22, WorldTransform.M23));

        public Vector3 Forward => Vector3.Normalize(new Vector3(WorldTransform.M31, WorldTransform.M32, WorldTransform.M33));

        //-------------------------------------------------------------------------
        // Camera properties
        //-------------------------------------------------------------------------

        public float NearClip { get; }

        public float FarClip { get; }

        public float AspectRatio { get; }

        public float HorizontalFov { get; }

        public float VerticalFov { get; }

        //-------------------------------------------------------------------------
        // Display options
        //-------------------------------------------------------------------------

        public Color Color { get; }

        public bool ShowOrigin { get; }

        public bool ShowAxes { get; }

        public bool ShowFrustum { get; }

        public bool ShowImagePlane { get; }

        public bool ShowCenterRay { get; }

        public bool ShowLabel { get; }

        public string Name => Extrinsics.targetNodeName;

        //-------------------------------------------------------------------------
        // Constructor
        //-------------------------------------------------------------------------

        public CameraModel(Intrinsics intrinsics, Extrinsics extrinsics, float nearClip = 0.05f, float farClip = 5.0f, Color? color = null, bool showOrigin = true, bool showAxes = true, bool showFrustum = true, bool showImagePlane = true, bool showCenterRay = false, bool showLabel = true)
        {
            Intrinsics = intrinsics ?? throw new ArgumentNullException(nameof(intrinsics));
            Extrinsics = extrinsics ?? throw new ArgumentNullException(nameof(extrinsics));

            NearClip = nearClip;
            FarClip = farClip;

            Color = color ?? Color.Orange;

            ShowOrigin = showOrigin;
            ShowAxes = showAxes;
            ShowFrustum = showFrustum;
            ShowImagePlane = showImagePlane;
            ShowCenterRay = showCenterRay;
            ShowLabel = showLabel;

            //---------------------------------------------------------------------
            // Intrinsics
            //---------------------------------------------------------------------

            double fx = intrinsics.K[0][0];
            double fy = intrinsics.K[1][1];

            AspectRatio = (float)intrinsics.ImageWidth / intrinsics.ImageHeight;

            HorizontalFov = 2f * (float)Math.Atan(intrinsics.ImageWidth / (2.0 * fx));

            VerticalFov =
                2f * (float)Math.Atan(intrinsics.ImageHeight / (2.0 * fy));

            //---------------------------------------------------------------------
            // Extrinsics
            //---------------------------------------------------------------------

            // OpenCV rotation matrix
            Matrix4x4 R = new Matrix4x4(
                (float)extrinsics.R[0][0], (float)extrinsics.R[0][1], (float)extrinsics.R[0][2], 0,
                (float)extrinsics.R[1][0], (float)extrinsics.R[1][1], (float)extrinsics.R[1][2], 0,
                (float)extrinsics.R[2][0], (float)extrinsics.R[2][1], (float)extrinsics.R[2][2], 0,
                0, 0, 0, 1);

            // Camera -> World rotation
            Matrix4x4 rotation = Matrix4x4.Transpose(R);

            Vector3 t = new(
                (float)extrinsics.t[0],
                (float)extrinsics.t[1],
                (float)extrinsics.t[2]);

            // Camera position in world coordinates
            Vector3 position = Vector3.Transform(-t, rotation);

            // Assemble homogeneous transform
            WorldTransform = new Matrix4x4(
                rotation.M11, rotation.M12, rotation.M13, 0,
                rotation.M21, rotation.M22, rotation.M23, 0,
                rotation.M31, rotation.M32, rotation.M33, 0,
                position.X, position.Y, position.Z, 1);
        }

        //-------------------------------------------------------------------------
        // Helpers
        //-------------------------------------------------------------------------

        public Vector3 TransformPoint(Vector3 localPoint)
        {
            return Vector3.Transform(localPoint, WorldTransform);
        }

        public Vector3 TransformDirection(Vector3 localDirection)
        {
            return Vector3.TransformNormal(localDirection, WorldTransform);
        }
    }

    public class PerspectiveViewer : Control
    {
        private bool _panning;
        private bool _rotating;

        private Point _lastMouse;

        // Orbit camera parameters
        private float _fov = 60f * (float)Math.PI / 180f; // radians
        private float _near = 0.1f;
        private float _far = 10000f;

        private float _yaw = 0.25f;
        private float _pitch = 0.3f;
        private float _roll = 0f;

        private float _distance = 10f;
        private Vector3 _target = Vector3.Zero;

        private readonly List<CameraModel> _cameras = new();
        private readonly List<(Vector3, Color)> _points = new();
        private readonly List<(Vector3, Vector3, Color)> _lines = new();

        public bool ShowAxes { get; set; } = true;
        public bool ShowDebug { get; set; } = false;

        public PerspectiveViewer()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;

            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            BackColor = Color.DarkGray;
        }

        #region Data API

        public void Clear()
        {
            _points.Clear();
            _lines.Clear();
            _cameras.Clear();
            Invalidate();
        }
        public void AddPoint(float x, float y, float z, Color color)
        {
            _points.Add((new Vector3(x, y, z), color));
        }
        public void AddLine(Vector3 p1, Vector3 p2, Color color)
        {
            _lines.Add((p1, p2, color));
        }
        public void AddCamera(CameraModel camera)
        {
            _cameras.Add(camera);
        }


        #endregion

        #region Camera Math
        private Matrix4x4 GetCameraMatrix()
        {
            var rotation = Matrix4x4.CreateFromYawPitchRoll(_yaw, _pitch, _roll);

            var forward = Vector3.Transform(-Vector3.UnitZ, rotation);
            var cameraPos = _target - forward * _distance;

            return Matrix4x4.CreateLookAt(cameraPos, _target, Vector3.UnitY);
        }
        private Matrix4x4 GetProjectionMatrix()
        {
            float aspect = (float)ClientSize.Width / ClientSize.Height;

            return Matrix4x4.CreatePerspectiveFieldOfView(
                _fov,
                aspect,
                _near,
                _far
            );
        }
        private PointF? Project(Vector3 p)
        {
            var view = GetCameraMatrix();
            var proj = GetProjectionMatrix();

            // World → View
            Vector4 v = Vector4.Transform(new Vector4(p, 1f), view);

            // View → Clip
            v = Vector4.Transform(v, proj);

            // Clip rejection
            if (v.W <= 0.0001f)
                return null;

            // Perspective divide → NDC
            float ndcX = v.X / v.W;
            float ndcY = v.Y / v.W;
            float ndcZ = v.Z / v.W;

            // Cull outside screen
            if (ndcX < -1 || ndcX > 1 || ndcY < -1 || ndcY > 1)
                return null;

            // NDC → Screen
            float x = (ndcX * 0.5f + 0.5f) * ClientSize.Width;
            float y = (1f - (ndcY * 0.5f + 0.5f)) * ClientSize.Height;

            return new PointF(x, y);
        }
        public void AutoCenter()
        {
            if (_points.Count == 0)
            {
                _target = Vector3.Zero;
                _distance = 10f;
                return;
            }

            var box = ComputeBoundingBox();
            var (center, distance) = ComputeCenterAndRadius(box);

            _target = center;
            _distance = distance * 1.5f; // padding
        }
        public BoundingBox ComputeBoundingBox()
        {
            if (_points.Count == 0)
            {
                return new BoundingBox
                {
                    Min = Vector3.Zero,
                    Max = Vector3.Zero
                };
            }

            float minX = float.MaxValue, minY = float.MaxValue, minZ = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue, maxZ = float.MinValue;

            foreach (var (pos, _) in _points)
            {
                minX = Math.Min(minX, pos.X);
                minY = Math.Min(minY, pos.Y);
                minZ = Math.Min(minZ, pos.Z);

                maxX = Math.Max(maxX, pos.X);
                maxY = Math.Max(maxY, pos.Y);
                maxZ = Math.Max(maxZ, pos.Z);
            }

            return new BoundingBox
            {
                Min = new Vector3(minX, minY, minZ),
                Max = new Vector3(maxX, maxY, maxZ)
            };
        }
        private (Vector3 center, float distance) ComputeCenterAndRadius(BoundingBox box)
        {
            var center = (box.Min + box.Max) * 0.5f;

            float extentX = box.Max.X - box.Min.X;
            float extentY = box.Max.Y - box.Min.Y;
            float extentZ = box.Max.Z - box.Min.Z;

            float extent = Math.Max(extentX, Math.Max(extentY, extentZ));

            if (extent < 0.001f)
                extent = 1f;

            float distance = extent / (2f * (float)Math.Tan(_fov / 2f));

            return (center, distance);
        }
        #endregion

        #region Rendering
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (ShowAxes)
                DrawAxes(g);

            foreach (var l in _lines)
            {
                var p1 = Project(l.Item1);
                var p2 = Project(l.Item2);

                if (p1.HasValue && p2.HasValue)
                {
                    using var pen = new Pen(l.Item3);
                    g.DrawLine(pen, p1.Value, p2.Value);
                }
            }

            foreach (var p in _points)
            {
                var s = Project(p.Item1);

                if (!s.HasValue)
                    continue;

                float r = 4f;
                using var brush = new SolidBrush(p.Item2);

                g.FillEllipse(brush, s.Value.X - r, s.Value.Y - r, r * 2, r * 2);
            }

            foreach (var camera in _cameras)
                DrawCamera(g, camera);

            DrawDebug(g);
        }
        private void DrawAxes(Graphics g)
        {
            var box = ComputeBoundingBox();

            // If no valid data, fall back to origin axes
            if (_points.Count == 0)
            {
                float BackupSize = 1000f;

                DrawLine3D(g, Vector3.Zero, new Vector3(BackupSize, 0, 0), Color.Red);
                DrawLine3D(g, Vector3.Zero, new Vector3(0, BackupSize, 0), Color.Green);
                DrawLine3D(g, Vector3.Zero, new Vector3(0, 0, BackupSize), Color.Blue);
                return;
            }


            // Determine a reasonable axis length based on box size
            var extent = box.Max - box.Min;
            var center = box.Min - (extent * 0.5f);
            float size = Math.Max(extent.X, Math.Max(extent.Y, extent.Z))*2;

            if (size < 0.001f)
                size = 1f;

            // Optional: scale it down so it doesn't dominate the scene
            //size *= 0.5f;

            DrawLine3D(g, center, center + new Vector3(size, 0, 0), Color.Red);
            DrawLine3D(g, center, center + new Vector3(0, size, 0), Color.Green);
            DrawLine3D(g, center, center + new Vector3(0, 0, size), Color.Blue);
        }
        private void DrawLine3D(Graphics g, Vector3 a, Vector3 b, Color color)
        {
            var p1 = Project(a);
            var p2 = Project(b);

            if (!p1.HasValue || !p2.HasValue)
                return;

            using var pen = new Pen(color);
            g.DrawLine(pen, p1.Value, p2.Value);
        }
        private void DrawDebug(Graphics g)
        {
            if (ShowDebug) 
            {
                using var font = new Font(SystemFonts.DefaultFont.FontFamily, 8f);

                float y = 0;
                float step = 18;

                var camPos = GetCameraPosition();
                var forward = GetForward();

                DrawText(g, $"Yaw / Pitch / Roll: {_yaw:0.00}, {_pitch:0.00}, {_roll:0.00}", new PointF(0, y), font); y += step;

                DrawText(g, $"CamPos: {FormatVec(camPos)}", new PointF(0, y), font); y += step;
                DrawText(g, $"Target: {FormatVec(_target)}", new PointF(0, y), font); y += step;

                DrawText(g, $"Distance: {_distance:0.00}", new PointF(0, y), font); y += step;
                DrawText(g, $"Forward: {FormatVec(forward)}", new PointF(0, y), font); y += step;

                DrawText(g, $"FOV: {(_fov * 180f / MathF.PI):0.0}°", new PointF(0, y), font); y += step;
                DrawText(g, $"Near/Far: {_near:0.00} / {_far:0}", new PointF(0, y), font); y += step;

                DrawText(g, $"Points: {_points.Count}  Lines: {_lines.Count}", new PointF(0, y), font); y += step;

                // Screen center → world (very useful sanity check)
                var centerWorld = ScreenToWorld(new Point(ClientSize.Width / 2, ClientSize.Height / 2));
                DrawText(g, $"CenterWorld: {FormatVec(centerWorld)}", new PointF(0, y), font); y += step;
            }
        }
        private void DrawText(Graphics g, string text, PointF location, Font font)
        {
            var size = g.MeasureString(text, font);

            var rect = new RectangleF(location.X - 2, location.Y - 2,
                size.Width + 4, size.Height + 4);

            g.FillRectangle(Brushes.Black, rect);
            g.DrawString(text, font, Brushes.Lime, location);
        }
        private void DrawCamera(Graphics g, CameraModel camera)
        {
            if (camera.ShowOrigin)
                DrawCameraOrigin(g, camera);

            if (camera.ShowAxes)
                DrawCameraAxes(g, camera);

            if (camera.ShowFrustum)
                DrawCameraFrustum(g, camera);

            if (camera.ShowImagePlane)
                DrawCameraImagePlane(g, camera);

            if (camera.ShowCenterRay)
                DrawCameraCenterRay(g, camera);

            if (camera.ShowLabel)
                DrawCameraLabel(g, camera);
        }
        private void DrawCameraOrigin(Graphics g, CameraModel camera)
        {
            var p = Project(camera.Position);

            if (!p.HasValue)
                return;

            const float r = 5f;

            using var brush = new SolidBrush(camera.Color);

            g.FillEllipse(
                brush,
                p.Value.X - r,
                p.Value.Y - r,
                r * 2,
                r * 2);
        }
        private void DrawCameraAxes(Graphics g, CameraModel camera)
        {
            float size = camera.FarClip * 0.2f;

            Vector3 origin = camera.Position;

            DrawLine3D(
                g,
                origin,
                camera.TransformPoint(new Vector3(size, 0, 0)),
                Color.Red);

            DrawLine3D(
                g,
                origin,
                camera.TransformPoint(new Vector3(0, size, 0)),
                Color.Green);

            DrawLine3D(
                g,
                origin,
                camera.TransformPoint(new Vector3(0, 0, size)),
                Color.Blue);
        }
        private void DrawCameraFrustum(Graphics g, CameraModel camera)
        {
            var c = GetFrustumCorners(camera);

            int[,] edges = {{0,1},{1,2},{2,3},{3,0},
                            {4,5},{5,6},{6,7},{7,4},
                            {0,4},{1,5},{2,6},{3,7}};

            for (int i = 0; i < edges.GetLength(0); i++)
            {
                DrawLine3D(
                    g,
                    c[edges[i, 0]],
                    c[edges[i, 1]],
                    camera.Color);
            }
        }
        private void DrawCameraImagePlane(Graphics g, CameraModel camera)
        {
            var c = GetFrustumCorners(camera);

            using var pen = new Pen(Color.Yellow, 2);

            DrawLine3D(g, c[0], c[1], pen.Color);
            DrawLine3D(g, c[1], c[2], pen.Color);
            DrawLine3D(g, c[2], c[3], pen.Color);
            DrawLine3D(g, c[3], c[0], pen.Color);
        }
        private void DrawCameraCenterRay(Graphics g, CameraModel camera)
        {
            DrawLine3D(
                g,
                camera.Position,
                camera.TransformPoint(new Vector3(0, 0, camera.FarClip)),
                Color.White);
        }
        private void DrawCameraLabel(Graphics g, CameraModel camera)
        {
            var p = Project(camera.Position);

            if (!p.HasValue)
                return;

            using var font = new Font(SystemFonts.DefaultFont.FontFamily, 8f);

            var size = g.MeasureString(camera.Name, font);

            RectangleF rect = new RectangleF(
                p.Value.X + 8,
                p.Value.Y - size.Height / 2,
                size.Width + 4,
                size.Height + 4);

            g.FillRectangle(Brushes.Black, rect);

            g.DrawString(
                camera.Name,
                font,
                Brushes.White,
                rect.X + 2,
                rect.Y + 2);
        }
        private Vector3[] GetFrustumCorners(CameraModel camera)
        {
            float near = camera.NearClip;
            float far = camera.FarClip;

            float nearHeight = 2f * MathF.Tan(camera.VerticalFov * 0.5f) * near;
            float nearWidth = nearHeight * camera.AspectRatio;

            float farHeight = 2f * MathF.Tan(camera.VerticalFov * 0.5f) * far;
            float farWidth = farHeight * camera.AspectRatio;

            Vector3[] local =
            {
        // Near
        new(-nearWidth/2,  nearHeight/2, near),
        new( nearWidth/2,  nearHeight/2, near),
        new( nearWidth/2, -nearHeight/2, near),
        new(-nearWidth/2, -nearHeight/2, near),

        // Far
        new(-farWidth/2,  farHeight/2, far),
        new( farWidth/2,  farHeight/2, far),
        new( farWidth/2, -farHeight/2, far),
        new(-farWidth/2, -farHeight/2, far)
    };

            for (int i = 0; i < 8; i++)
                local[i] = camera.TransformPoint(local[i]);

            return local;
        }

        #endregion

        #region Mouse
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            _lastMouse = e.Location;

            if (e.Button == MouseButtons.Right || e.Button == MouseButtons.Middle)
            {
                _panning = true;
                Cursor = Cursors.Hand;
            }

            if (e.Button == MouseButtons.Left) // && ModifierKeys.HasFlag(Keys.Control))
            {
                _rotating = true;
                Cursor = Cursors.SizeAll;
            }
        }
        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);

            AutoCenter();
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            _panning = false;
            _rotating = false;
            Cursor = Cursors.Default;
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            float dx = e.X - _lastMouse.X;
            float dy = e.Y - _lastMouse.Y;

            float sensitivity = 0.01f;

            if (_rotating)
            {
                _yaw += dx * sensitivity;
                _pitch += dy * sensitivity;

                _pitch = Math.Clamp(_pitch, -1.5f, 1.5f);
            }

            if (_panning)
            {
                var right = Vector3.Normalize(Vector3.Cross(GetForward(), Vector3.UnitY));
                var up = Vector3.UnitY;

                float panSpeed = _distance * 0.002f;

                _target += (-right * dx + up * dy) * panSpeed;
            }

            _lastMouse = e.Location;
            Invalidate();
        }
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            float zoomFactor = e.Delta > 0 ? 0.9f : 1.1f;

            // Zoom to cursor
            var before = ScreenToWorld(e.Location);

            _distance *= zoomFactor;
            _distance = Math.Clamp(_distance, .001f, 10000f);

            var after = ScreenToWorld(e.Location);

            _target += before - after;

            Invalidate();
        }
        #endregion

        #region Helpers

        private Vector3 GetForward()
        {
            var rot = Matrix4x4.CreateFromYawPitchRoll(_yaw, _pitch, _roll);
            return Vector3.Transform(-Vector3.UnitZ, rot);
        }

        private Vector3 GetCameraPosition()
        {
            var rotation = Matrix4x4.CreateFromYawPitchRoll(_yaw, _pitch, _roll);
            var forward = Vector3.Transform(-Vector3.UnitZ, rotation);
            return _target - forward * _distance;
        }

        private string FormatVec(Vector3 v)
        {
            return $"({v.X:0.##}, {v.Y:0.##}, {v.Z:0.##})";
        }

        private Vector3 ScreenToWorld(Point p)
        {
            float width = ClientSize.Width;
            float height = ClientSize.Height;

            // 1. Screen → NDC (-1 to 1)
            float ndcX = (2f * p.X / width) - 1f;
            float ndcY = 1f - (2f * p.Y / height);

            // 2. Build inverse ViewProjection
            var view = GetCameraMatrix();
            var proj = GetProjectionMatrix();

            Matrix4x4 viewProj = view * proj;
            Matrix4x4.Invert(viewProj, out var invViewProj);

            // 3. Unproject near + far points
            var nearPoint = Vector4.Transform(new Vector4(ndcX, ndcY, 0f, 1f), invViewProj);
            var farPoint = Vector4.Transform(new Vector4(ndcX, ndcY, 1f, 1f), invViewProj);

            // Perspective divide
            nearPoint /= nearPoint.W;
            farPoint /= farPoint.W;

            Vector3 rayOrigin = new Vector3(nearPoint.X, nearPoint.Y, nearPoint.Z);
            Vector3 rayDir = Vector3.Normalize(
                new Vector3(farPoint.X, farPoint.Y, farPoint.Z) - rayOrigin
            );

            // 4. Intersect with plane at _target (Z plane)
            float t = (_target.Z - rayOrigin.Z) / rayDir.Z;

            return rayOrigin + rayDir * t;
        }

        #endregion
    } 
    #endregion
}