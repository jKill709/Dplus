namespace Dplus_Desktop.Config
{
    public class HubSettings
    {
        public string name { get; set; } = string.Empty;
        public string role { get; set; } = string.Empty;
        public string clusterID { get; set; } = string.Empty;
        public string hubName { get; set; } = string.Empty;
        public string hubIPaddress { get; set; } = string.Empty;
        public string hubTelemetryTopic { get; set; } = string.Empty;
        public string hubCommandTopic { get; set; } = string.Empty;
        public string nodeTelemetryTopic { get; set; } = string.Empty;
        public string nodeCommandTopic { get; set; } = string.Empty;
        public List<NodeInfoForHubSettings> nodes { get; set; } = new();
        public List<Intrinsics> intrinsics { get; set; } = new();
        public List<Extrinsics> extrinsics { get; set; } = new();
        public string rootDir { get; set; } = string.Empty;
        public string captureDir { get; set; } = string.Empty;
        public string srcDir { get; set; } = string.Empty;
        public string logDir { get; set; } = string.Empty;
        public string modelDir { get; set; } = string.Empty;

        public bool forceExtrinsicsRecalibration { get; set; } = false;
        public float repropuctionErrThreshAtExtrinsicsCalcualtion { get; set; } = 0.0f;
        public int maxMQTTQueueSize { get; set; } = 0;
        public bool saveReconstructions { get; set; } = false;
        public bool broadcastReconstructions { get; set; } = false;

        public int extrinsicsCaptureCount { get; set; } = 0;

        public YoloPoseDetectorProfile yoloPoseDetSettings { get; set; } = new YoloPoseDetectorProfile();
        public YoloObjectDetectorProfile yoloObjectDetSettings { get; set; } = new YoloObjectDetectorProfile();
        public FaceIDDetectorProfile faceIDDetSettings { get; set; } = new FaceIDDetectorProfile();
        public ChArUcoBoardDetectorProfileForHubSetting chArUcoBoardDetSettings { get; set; } = new ChArUcoBoardDetectorProfileForHubSetting();
        public ChessboardDetectorProfileForHubSetting chessboardDetSettings { get; set; } = new ChessboardDetectorProfileForHubSetting();

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
