namespace Dplus_Desktop.UI.ViewerForm
{
    public class RigFrame
    {
        public int commandID { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.MinValue;

        public List<CameraFrame> camFrames { get; set; } = new();

        public List<YoloPoseReconstruction>? poseRecs { get; set; } = new();
        public List<YoloObjectReconstruction>? objectRecs { get; set; } = new();
        public List<FaceReconstruction>? faceRecs { get; set; } = new();
        public ChArUcoReconstruction? charucoRec { get; set; } = new();
        public ChessboardReconstruction? chessboardRec { get; set; } = new();
    }
    public class CameraFrame
    {
        public string sourceName { get; set; } = string.Empty;

        public List<YoloPoseDetection>? poseDets { get; set; } = new();
        public List<YoloObjectDetection>? objectDets { get; set; } = new();
        public List<FaceDetection>? faceDets { get; set; } = new();
        public ChArUcoDetection? charucoDet { get; set; } = new();
        public ChessboardDetection? chessboardDet { get; set; } = new();
    }
}
