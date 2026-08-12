using System.Numerics;

namespace Dplus_Desktop.UI.Controls.ImageControls
{
    public struct BoundingBox
    {
        public Vector3 Min;
        public Vector3 Max;

        public bool IsValid => Min != Max;
    }
}