namespace Dplus_Desktop.Config
{
    public class ChArUcoBoardDetectorProfileForHubSetting
    {
        public string name { get; set; } = string.Empty;
        public bool useChArUcoBoardDetection { get; set; } = false;
        public bool saveChArUcoBoardDetections { get; set; } = false;
        public ChArUcoBoardParameters chArUcoBoard { get; set; } = new ChArUcoBoardParameters();
        public int RepErrThreshAtReconstruction { get; set; } = 10;
    }
}
