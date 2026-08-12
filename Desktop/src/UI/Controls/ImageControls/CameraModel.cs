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

}
