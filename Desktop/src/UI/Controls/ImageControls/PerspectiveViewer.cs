using System.Drawing.Drawing2D;
using System.Numerics;

namespace Dplus_Desktop.UI.Controls.ImageControls
{
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
            float size = Math.Max(extent.X, Math.Max(extent.Y, extent.Z)) * 2;

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
            var view = GetCameraMatrix();
            var proj = GetProjectionMatrix();

            // World → View → Clip
            Vector4 p1 = Vector4.Transform(new Vector4(a, 1f), view);
            Vector4 p2 = Vector4.Transform(new Vector4(b, 1f), view);

            p1 = Vector4.Transform(p1, proj);
            p2 = Vector4.Transform(p2, proj);

            // Clip the line against the view frustum.
            if (!ClipLineToFrustum(ref p1, ref p2))
                return;

            // Perspective divide → NDC
            float ndcX1 = p1.X / p1.W;
            float ndcY1 = p1.Y / p1.W;

            float ndcX2 = p2.X / p2.W;
            float ndcY2 = p2.Y / p2.W;

            // NDC → Screen
            float x1 = (ndcX1 * 0.5f + 0.5f) * ClientSize.Width;
            float y1 = (1f - (ndcY1 * 0.5f + 0.5f)) * ClientSize.Height;

            float x2 = (ndcX2 * 0.5f + 0.5f) * ClientSize.Width;
            float y2 = (1f - (ndcY2 * 0.5f + 0.5f)) * ClientSize.Height;

            using var pen = new Pen(color);
            g.DrawLine(pen, x1, y1, x2, y2);
        }
        private void OLD_DrawLine3D(Graphics g, Vector3 a, Vector3 b, Color color)
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
        private bool ClipLineToFrustum(ref Vector4 p1, ref Vector4 p2)
        {
            // The clip-space frustum is defined by:
            //
            //   -W <= X <= W
            //   -W <= Y <= W
            //   -W <= Z <= W
            //
            // Each plane can therefore be represented as:
            //
            //   X + W >= 0     left
            //   W - X >= 0     right
            //   Y + W >= 0     bottom
            //   W - Y >= 0     top
            //   Z + W >= 0     near
            //   W - Z >= 0     far

            if (!ClipLineAgainstPlane(ref p1, ref p2, v => v.X + v.W))
                return false;

            if (!ClipLineAgainstPlane(ref p1, ref p2, v => v.W - v.X))
                return false;

            if (!ClipLineAgainstPlane(ref p1, ref p2, v => v.Y + v.W))
                return false;

            if (!ClipLineAgainstPlane(ref p1, ref p2, v => v.W - v.Y))
                return false;

            if (!ClipLineAgainstPlane(ref p1, ref p2, v => v.Z + v.W))
                return false;

            if (!ClipLineAgainstPlane(ref p1, ref p2, v => v.W - v.Z))
                return false;

            return true;
        }
        private bool ClipLineAgainstPlane(ref Vector4 p1, ref Vector4 p2, Func<Vector4, float> distance)
        {
            float d1 = distance(p1);
            float d2 = distance(p2);

            // Both points are outside the plane.
            if (d1 < 0f && d2 < 0f)
                return false;

            // Both points are inside (or exactly on) the plane.
            if (d1 >= 0f && d2 >= 0f)
                return true;

            // The line crosses the plane.
            //
            // p(t) = p1 + t(p2 - p1)
            //
            // Solve:
            //
            // distance(p(t)) = 0
            //
            // t = d1 / (d1 - d2)

            float t = d1 / (d1 - d2);

            Vector4 intersection = p1 + (p2 - p1) * t;

            if (d1 < 0f)
                p1 = intersection;
            else
                p2 = intersection;

            return true;
        }

        #endregion
    }
}
