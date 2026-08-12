namespace Dplus_Desktop.Config
{
    public class ClusterProfile
    {
        public string profileName { get; set; } = string.Empty;
        public List<Extrinsics> extrinsics { get; set; } = new();
        public int intrinsicsCaptureCount { get; set; } = 0;
        public int extrinsicsCaptureCount { get; set; } = 0;

        public string YoloPoseProfileToUse { get; set; } = string.Empty;
        public string YoloObjectProfileToUse { get; set; } = string.Empty;
        public string FaceIDProfileToUse { get; set; } = string.Empty;
        public string ChArUcoDetProfileToUse { get; set; } = string.Empty;
        public string ChessboardDetProfileToUse { get; set; } = string.Empty;

        public bool captureOnStartup { get; set; } = false;
        public bool captureEachFrame { get; set; } = false;
        public bool captureEachDetection { get; set; } = false;
        public bool captureNewDetection { get; set; } = false;
        public double targetFrameRate { get; set; } = 0.0;
        public string indicatorType { get; set; } = string.Empty;
        public int introSequenceIterations { get; set; } = 0;
        public int introSequenceDelay { get; set; } = 0;
    }
}
