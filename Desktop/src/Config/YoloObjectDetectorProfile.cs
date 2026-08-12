namespace Dplus_Desktop.Config
{
    public class YoloObjectDetectorProfile
    {
        public string name { get; set; } = string.Empty;
        // Behavior
        public bool useModel { get; set; } = false;
        public bool saveWholeDetectionImage { get; set; } = false;
        public bool savePartialDetectionImage { get; set; } = false;
        // Constants
        public string modelPath { get; set; } = string.Empty;
        public string classes { get; set; } = string.Empty;
        // Thresholds
        public double objectConfidence { get; set; } = 0.0;
        public double iouThreshold { get; set; } = 0.0;
    }
}
