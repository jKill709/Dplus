using OpenCvSharp;

namespace Dplus_Desktop.UI.ViewerForm
{
    public class ChArUcoReconstruction
    {
        public DateTime Timestamp { get; set; } = DateTime.MinValue;
        public List<int> charucoIds { get; set; } = new();
        public List<Point3f> charucoCorners { get; set; } = new();
        public List<float> cornerReproductionError { get; set; } = new();
    }
    public class ChessboardReconstruction
    {
        public DateTime Timestamp { get; set; } = DateTime.MinValue;
        public List<int> chessboardIds { get; set; } = new();
        public List<Point3f> chessboardCorners { get; set; } = new();
    }
    public class FaceReconstruction
    {
        public DateTime Timestamp { get; set; } = DateTime.MinValue;
        public Point3f BoxCenter { get; set; } = new(0, 0, 0);
        public float BoxSize { get; set; } = 0.0f;
        public float Confidence { get; set; } = 0.0f;
        public int ClassId { get; set; } = -1;

        public FaceReconstruction() { }

        public FaceReconstruction(long TimestampOffset, Point3f boxCenter, float boxSize, float confidence, int classId)
        {
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(TimestampOffset).LocalDateTime;
            BoxCenter = boxCenter;
            BoxSize = boxSize;
            Confidence = confidence;
            ClassId = classId;
        }
    }
    public class YoloObjectReconstruction
    {
        public DateTime Timestamp { get; set; } = DateTime.MinValue;
        public Point3f BoxCenter { get; set; } = new(0, 0, 0);
        public float BoxSize { get; set; } = 0.0f;
        public float Confidence { get; set; } = 0.0f;
        public int ClassId { get; set; } = -1;

        public YoloObjectReconstruction() { }

        public YoloObjectReconstruction(long timestampOffset, Point3f boxCenter, float boxSize, float confidence, int classId)
        {
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(timestampOffset).LocalDateTime;
            BoxCenter = boxCenter;
            BoxSize = boxSize;
            Confidence = confidence;
            ClassId = classId;
        }
    }
    public class YoloPoseReconstruction
    {
        public DateTime Timestamp { get; set; } = DateTime.MinValue;
        public Point3f BoxCenter { get; set; } = new(0, 0, 0);
        public float BoxSize { get; set; } = 0.0f;
        public float Confidence { get; set; } = 0.0f;
        public int ClassId { get; set; } = -1;

        // 17 keypoints for pose, empty for object detection
        public List<Point3f> Keypoints { get; set; } = new();
        public List<float> Kp_Confidences { get; set; } = new();

        public YoloPoseReconstruction() { }

        public YoloPoseReconstruction(long timestampOffset, Point3f boxCenter, float boxSize, float confidence, int classId, List<Point3f> keypoints, List<float> kp_confidences)
        {
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(timestampOffset).LocalDateTime;
            BoxCenter = boxCenter;
            BoxSize = boxSize;
            Confidence = confidence;
            ClassId = classId;
            Keypoints = keypoints ?? new();
            Kp_Confidences = kp_confidences ?? new();
        }
    }
}
