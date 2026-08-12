namespace Dplus_Desktop.Config
{
    public class Extrinsics
    {
        private const double FLT_EPS = 1e-9;
        public string baseNodeName { get; set; } = string.Empty;
        public string targetNodeName { get; set; } = string.Empty;
        public double[][] R { get; set; }
        public double[] t { get; set; }

        public static Extrinsics Identity(string nodeName)
        {
            return new Extrinsics
            {
                baseNodeName = nodeName,
                targetNodeName = nodeName,
                R = new double[][]
                {
                new double[] { 1, 0, 0 },
                new double[] { 0, 1, 0 },
                new double[] { 0, 0, 1 }
                },
                t = new double[] { 0, 0, 0 }
            };
        }

        public static bool operator ==(Extrinsics a, Extrinsics b)
        {
            if (ReferenceEquals(a, b))
                return true;

            if (a is null || b is null)
                return false;

            if (a.baseNodeName != b.baseNodeName ||
                a.targetNodeName != b.targetNodeName)
                return false;

            // Compare R
            if (a.R.Length != b.R.Length)
                return false;

            for (int i = 0; i < a.R.Length; i++)
            {
                if (a.R[i].Length != b.R[i].Length)
                    return false;

                for (int j = 0; j < a.R[i].Length; j++)
                {
                    if (Math.Abs(a.R[i][j] - b.R[i][j]) > FLT_EPS)
                        return false;
                }
            }

            // Compare t
            if (a.t.Length != b.t.Length)
                return false;

            for (int i = 0; i < a.t.Length; i++)
            {
                if (Math.Abs(a.t[i] - b.t[i]) > FLT_EPS)
                    return false;
            }

            return true;
        }

        public static bool operator !=(Extrinsics a, Extrinsics b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            if (obj is not Extrinsics other)
                return false;

            return this == other;
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();

            hash.Add(baseNodeName);
            hash.Add(targetNodeName);

            foreach (var row in R)
                foreach (var v in row)
                    hash.Add(v);

            foreach (var v in t)
                hash.Add(v);

            return hash.ToHashCode();
        }
    }
}
