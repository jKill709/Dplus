namespace Dplus_Desktop.Config
{
    public class ChArUcoBoardDetectorProfile
    {
        public string name { get; set; } = string.Empty;
        public bool useChArUcoBoardDetection { get; set; } = false;
        public bool saveChArUcoBoardDetections { get; set; } = false;
        public string chArUcoBoardToUse { get; set; } = string.Empty;
        public int RepErrThreshAtReconstruction { get; set; } = 0;
    }
}
