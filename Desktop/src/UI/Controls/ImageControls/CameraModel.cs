using Dplus_Desktop.Config;
using System.Numerics;

namespace Dplus_Desktop.UI.Controls.ImageControls
{
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

        public Vector3 Up => Vector3.Normalize(new Vector3(WorldTransform.M21, WorldTransform.M22, WorldTransform.M23));

        public Vector3 Forward => Vector3.Normalize(new Vector3(WorldTransform.M31, WorldTransform.M32, WorldTransform.M33));

        //-------------------------------------------------------------------------
        // Camera properties
        //-------------------------------------------------------------------------

        public float FrustumNear { get; }

        public float FrustumFar { get; }

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
        public CameraModel(Intrinsics intrinsics, Extrinsics extrinsics, float frustumNear = 0.05f, float frustumFar = 5.0f, Color? color = null, bool showOrigin = true, bool showAxes = true, bool showFrustum = true, bool showImagePlane = true, bool showCenterRay = false, bool showLabel = true)
        {
            if (intrinsics == null)
                throw new ArgumentNullException(nameof(intrinsics));

            if (extrinsics == null)
                throw new ArgumentNullException(nameof(extrinsics));

            if (frustumNear <= 0f)
                throw new ArgumentOutOfRangeException(
                    nameof(frustumNear),
                    "Frustum near distance must be greater than zero.");

            if (frustumFar <= frustumNear)
                throw new ArgumentOutOfRangeException(
                    nameof(frustumFar),
                    "Frustum far distance must be greater than frustum near distance.");

            //-------------------------------------------------------------------------
            // Original calibration
            //-------------------------------------------------------------------------

            Intrinsics = intrinsics;
            Extrinsics = extrinsics;

            //-------------------------------------------------------------------------
            // Frustum visualization parameters
            //-------------------------------------------------------------------------

            FrustumNear = frustumNear;
            FrustumFar = frustumFar;

            //-------------------------------------------------------------------------
            // Display options
            //-------------------------------------------------------------------------

            Color = color ?? Color.Orange;

            ShowOrigin = showOrigin;
            ShowAxes = showAxes;
            ShowFrustum = showFrustum;
            ShowImagePlane = showImagePlane;
            ShowCenterRay = showCenterRay;
            ShowLabel = showLabel;

            //-------------------------------------------------------------------------
            // Intrinsic camera parameters
            //-------------------------------------------------------------------------

            double fx = intrinsics.K[0][0];
            double fy = intrinsics.K[1][1];

            if (fx <= 0.0)
                throw new ArgumentException(
                    "Intrinsic matrix contains an invalid focal length fx.",
                    nameof(intrinsics));

            if (fy <= 0.0)
                throw new ArgumentException(
                    "Intrinsic matrix contains an invalid focal length fy.",
                    nameof(intrinsics));

            if (intrinsics.ImageWidth <= 0)
                throw new ArgumentException(
                    "Image width must be greater than zero.",
                    nameof(intrinsics));

            if (intrinsics.ImageHeight <= 0)
                throw new ArgumentException(
                    "Image height must be greater than zero.",
                    nameof(intrinsics));

            AspectRatio =
                (float)intrinsics.ImageWidth / intrinsics.ImageHeight;

            HorizontalFov =
                2f * MathF.Atan(
                    intrinsics.ImageWidth / (2f * (float)fx));

            VerticalFov =
                2f * MathF.Atan(
                    intrinsics.ImageHeight / (2f * (float)fy));

            //-------------------------------------------------------------------------
            // Extrinsics
            //
            // OpenCV convention:
            //
            //      Pc = R * Pw + t
            //
            // Therefore:
            //
            //      Rworld = R^T
            //      Cworld = -R^T * t
            //-------------------------------------------------------------------------

            Matrix4x4 r = new Matrix4x4(
                (float)extrinsics.R[0][0],
                (float)extrinsics.R[0][1],
                (float)extrinsics.R[0][2],
                0f,

                (float)extrinsics.R[1][0],
                (float)extrinsics.R[1][1],
                (float)extrinsics.R[1][2],
                0f,

                (float)extrinsics.R[2][0],
                (float)extrinsics.R[2][1],
                (float)extrinsics.R[2][2],
                0f,

                0f, 0f, 0f, 1f);

            // Camera -> World rotation
            Matrix4x4 rotation = Matrix4x4.Transpose(r);

            Vector3 t = new Vector3(
                (float)extrinsics.t[0],
                (float)extrinsics.t[1],
                (float)extrinsics.t[2]);

            // Camera position in world coordinates.
            Vector3 position =
                Vector3.Transform(-t, rotation);

            //-------------------------------------------------------------------------
            // Complete camera -> world transform
            //-------------------------------------------------------------------------

            WorldTransform = new Matrix4x4(
                rotation.M11, rotation.M12, rotation.M13, 0f,
                rotation.M21, rotation.M22, rotation.M23, 0f,
                rotation.M31, rotation.M32, rotation.M33, 0f,
                position.X, position.Y, position.Z, 1f);
        }
        //public CameraModel(Intrinsics intrinsics, Extrinsics extrinsics, float frustrumNear = 0.05f, float frustrumFar = 5.0f, Color? color = null, bool showOrigin = true, bool showAxes = true, bool showFrustum = true, bool showImagePlane = true, bool showCenterRay = false, bool showLabel = true)
        //{
        //    Intrinsics = intrinsics ?? throw new ArgumentNullException(nameof(intrinsics));
        //    Extrinsics = extrinsics ?? throw new ArgumentNullException(nameof(extrinsics));

        //    FrustumNear = frustrumNear;
        //    FrustumFar = frustrumFar;

        //    Color = color ?? Color.Orange;

        //    ShowOrigin = showOrigin;
        //    ShowAxes = showAxes;
        //    ShowFrustum = showFrustum;
        //    ShowImagePlane = showImagePlane;
        //    ShowCenterRay = showCenterRay;
        //    ShowLabel = showLabel;

        //    //---------------------------------------------------------------------
        //    // Intrinsics
        //    //---------------------------------------------------------------------

        //    double fx = intrinsics.K[0][0];
        //    double fy = intrinsics.K[1][1];

        //    AspectRatio = (float)intrinsics.ImageWidth / intrinsics.ImageHeight;

        //    HorizontalFov = 2f * (float)Math.Atan(intrinsics.ImageWidth / (2.0 * fx));

        //    VerticalFov =
        //        2f * (float)Math.Atan(intrinsics.ImageHeight / (2.0 * fy));

        //    //---------------------------------------------------------------------
        //    // Extrinsics
        //    //---------------------------------------------------------------------

        //    // OpenCV rotation matrix
        //    Matrix4x4 R = new Matrix4x4(
        //        (float)extrinsics.R[0][0], (float)extrinsics.R[0][1], (float)extrinsics.R[0][2], 0,
        //        (float)extrinsics.R[1][0], (float)extrinsics.R[1][1], (float)extrinsics.R[1][2], 0,
        //        (float)extrinsics.R[2][0], (float)extrinsics.R[2][1], (float)extrinsics.R[2][2], 0,
        //        0, 0, 0, 1);

        //    // Camera -> World rotation
        //    Matrix4x4 rotation = Matrix4x4.Transpose(R);

        //    Vector3 t = new(
        //        (float)extrinsics.t[0],
        //        (float)extrinsics.t[1],
        //        (float)extrinsics.t[2]);

        //    // Camera position in world coordinates
        //    Vector3 position = Vector3.Transform(-t, rotation);

        //    // Assemble homogeneous transform
        //    WorldTransform = new Matrix4x4(
        //        rotation.M11, rotation.M12, rotation.M13, 0,
        //        rotation.M21, rotation.M22, rotation.M23, 0,
        //        rotation.M31, rotation.M32, rotation.M33, 0,
        //        position.X, position.Y, position.Z, 1);
        //}

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

        public Vector3 ImagePointToRay(float x, float y)
        {
            double fx = Intrinsics.K[0][0];
            double fy = Intrinsics.K[1][1];
            double cx = Intrinsics.K[0][2];
            double cy = Intrinsics.K[1][2];

            if (Math.Abs(fx) < 1e-12 || Math.Abs(fy) < 1e-12)
                throw new InvalidOperationException(
                    "Camera intrinsic matrix contains an invalid focal length.");

            // OpenCV camera coordinates:
            //
            //      +X = right
            //      +Y = down
            //      +Z = forward
            //
            // Convert an image pixel into an ideal pinhole-camera ray.
            Vector3 ray = new Vector3(
                (float)((x - cx) / fx),
                (float)((y - cy) / fy),
                1f);

            return Vector3.Normalize(ray);
        }
        public Vector3[] GetFrustumCorners()
        {
            float near = FrustumNear;
            float far = FrustumFar;

            float width = Intrinsics.ImageWidth;
            float height = Intrinsics.ImageHeight;

            // Image corners in pixel coordinates.
            //
            // Order:
            //
            //   0 -------- 1
            //   |          |
            //   |          |
            //   3 -------- 2
            //
            Vector3[] rays =
            {
        ImagePointToRay(0f,     0f),      // top-left
        ImagePointToRay(width,  0f),      // top-right
        ImagePointToRay(width,  height),  // bottom-right
        ImagePointToRay(0f,     height)   // bottom-left
    };

            Vector3[] corners = new Vector3[8];

            for (int i = 0; i < 4; i++)
            {
                // The frustum planes are defined at constant camera-space Z.
                //
                // The ray is normalized, so its Z component may be less than 1.
                // Scale it so that the resulting point has exactly the desired
                // camera-space Z distance.
                float nearScale = near / rays[i].Z;
                float farScale = far / rays[i].Z;

                Vector3 nearPoint = rays[i] * nearScale;
                Vector3 farPoint = rays[i] * farScale;

                corners[i] = TransformPoint(nearPoint);
                corners[i + 4] = TransformPoint(farPoint);
            }

            return corners;
        }
    }

}
