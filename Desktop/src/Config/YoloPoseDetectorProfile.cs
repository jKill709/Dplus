namespace Dplus_Desktop.Config
{
    public class YoloPoseDetectorProfile
    {
        public string name { get; set; } = string.Empty;
        // Behavior
        public bool useModel { get; set; } = false;
        public bool saveWholeDetectionImage { get; set; } = false;
        public bool savePartialDetectionImage { get; set; } = false;

        // Constants
        public string modelPath { get; set; } = string.Empty;
        public int cocoPKcount { get; set; } = 17;

        // Thresholds
        public double detectConfThreshold { get; set; } = 0.0;
        public double kpDetectThreshold { get; set; } = 0.0;
        public double nmsThreshold { get; set; } = 0.0;
        public double iouThreshold { get; set; } = 0.0;
        public double ReconstructionThreshold { get; set; } = 0.0;
    }
}
