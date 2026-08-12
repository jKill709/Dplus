using OpenCvSharp;

namespace Dplus_Desktop.UI.ViewerForm
{
    public class ChArUcoDetection
    {
        public DateTime Timestamp { get; set; } = DateTime.MinValue;
        public bool Valid { get; set; } = false;

        public List<int> CharucoIds { get; set; } = new();
        public List<Point2f> CharucoCorners { get; set; } = new();
    }
    public class ChessboardDetection
    {
        public DateTime Timestamp { get; set; } = DateTime.MinValue;
        public bool Valid { get; set; } = false;

        public List<int> ChessboardIds { get; set; } = new();
        public List<Point2f> ChessboardCorners { get; set; } = new();
    }
    public class FaceDetection
    {
        public DateTime Timestamp { get; set; } = DateTime.MinValue;
        public Rect2f Box { get; set; } = new(0, 0, 0, 0);
        public float Confidence { get; set; } = 0.0f;
        public int ClassId { get; set; } = -1;

        public FaceDetection() { }

        public FaceDetection(long timestampOffset, Rect2f box, float confidence, int classId)
        {
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(timestampOffset).LocalDateTime;
            Box = box;
            Confidence = confidence;
            ClassId = classId;
        }
    }
    public class YoloObjectDetection
    {
        public DateTime Timestamp { get; set; } = DateTime.MinValue;
        public Rect2f Box { get; set; } = new(0, 0, 0, 0);
        public float Confidence { get; set; } = 0.0f;
        public int ClassId { get; set; } = -1;

        public YoloObjectDetection() { }

        public YoloObjectDetection(long timestampOffset, Rect2f box, float confidence, int classId)
        {
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(timestampOffset).LocalDateTime;
            Box = box;
            Confidence = confidence;
            ClassId = classId;
        }
    }
    public class YoloPoseDetection
    {
        public DateTime Timestamp { get; set; } = DateTime.MinValue;
        public Rect2f Box { get; set; } = new(0, 0, 0, 0);
        public float Confidence { get; set; } = 0.0f;
        public int ClassId { get; set; } = -1;

        // 17 keypoints for pose, empty for object detection
        public List<Point2f> Keypoints { get; set; } = new();
        public List<float> Kp_Confidences { get; set; } = new();

        public YoloPoseDetection() { }

        public YoloPoseDetection(long timestampOffset, Rect2f box, float confidence, int classId, List<Point2f> keypoints, List<float> kp_confidences)
        {
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(timestampOffset).LocalDateTime;
            Box = box;
            Confidence = confidence;
            ClassId = classId;
            Keypoints = keypoints;
            Kp_Confidences = kp_confidences;

        }
    }
}
