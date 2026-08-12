using OpenCvSharp;

namespace Dplus_Desktop.Config
{
    public class Intrinsics
    {
        private const double FLT_EPS = 1e-9;

        public int CameraIDnumber { get; set; } = 0;
        public double Rms { get; set; } = double.MaxValue;
        public int ImageWidth { get; set; } = 640;
        public int ImageHeight { get; set; } = 480;

        public double[][] K { get; set; } =
        {
        new double[3],
        new double[3],
        new double[3]
        };

        public double[] Dist { get; set; } = Array.Empty<double>();
        public static bool operator ==(Intrinsics a, Intrinsics b)
        {
            if (ReferenceEquals(a, b))
                return true;

            if (a is null || b is null)
                return false;

            if (a.CameraIDnumber != b.CameraIDnumber ||
                a.ImageWidth != b.ImageWidth ||
                a.ImageHeight != b.ImageHeight)
                return false;

            if (Math.Abs(a.Rms - b.Rms) > FLT_EPS)
                return false;

            // Compare K
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (Math.Abs(a.K[i][j] - b.K[i][j]) > FLT_EPS)
                        return false;
                }
            }

            // Compare Dist
            if (a.Dist.Length != b.Dist.Length)
                return false;

            for (int i = 0; i < a.Dist.Length; i++)
            {
                if (Math.Abs(a.Dist[i] - b.Dist[i]) > FLT_EPS)
                    return false;
            }

            return true;
        }
        public static bool operator !=(Intrinsics a, Intrinsics b)
        {
            return !(a == b);
        }
        public override bool Equals(object obj)
        {
            if (obj is not Intrinsics other)
                return false;

            return this == other;
        }
        public override int GetHashCode()
        {
            var hash = new HashCode();

            hash.Add(CameraIDnumber);
            hash.Add(Rms);
            hash.Add(ImageWidth);
            hash.Add(ImageHeight);

            foreach (var row in K)
                foreach (var v in row)
                    hash.Add(v);

            foreach (var v in Dist)
                hash.Add(v);

            return hash.ToHashCode();
        }
        // RMS-based ordering
        public static bool operator <(Intrinsics a, Intrinsics b)
        {
            if (a is null || b is null)
                throw new ArgumentNullException();

            return a.Rms < b.Rms;
        }
        public static bool operator >(Intrinsics a, Intrinsics b)
        {
            if (a is null || b is null)
                throw new ArgumentNullException();

            return a.Rms > b.Rms;
        }
        public static bool operator <=(Intrinsics a, Intrinsics b)
        {
            if (a is null || b is null)
                throw new ArgumentNullException();

            return a.Rms <= b.Rms;
        }
        public static bool operator >=(Intrinsics a, Intrinsics b)
        {
            if (a is null || b is null)
                throw new ArgumentNullException();

            return a.Rms >= b.Rms;
        }


        public PointF Project(Point2f normalized)
        {
            double fx = K[0][0];
            double fy = K[1][1];
            double cx = K[0][2];
            double cy = K[1][2];

            float x = (float)(fx * normalized.X + cx);
            float y = (float)(fy * normalized.Y + cy);

            return new PointF(x, y);
        }
        public Rect2f Project(Rect2f normalizedBox)
        {
            var tl = Project(new Point2f(normalizedBox.X, normalizedBox.Y));
            var br = Project(new Point2f(
                normalizedBox.X + normalizedBox.Width,
                normalizedBox.Y + normalizedBox.Height));

            return new Rect2f(
                tl.X,
                tl.Y,
                br.X - tl.X,
                br.Y - tl.Y
            );
        }
    }
}
