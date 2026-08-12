namespace Dplus_Desktop.Config
{
    public class FaceIDDetectorProfile
    {
        public string name { get; set; } = string.Empty;
        public bool useHaarFaceDetection { get; set; } = false;
        public bool saveHaarDetections { get; set; } = false;
        public string haarFaceModel { get; set; } = string.Empty;
        public bool useLBPHFaceRecognition { get; set; } = false;
        public bool saveLBPHRecognitions { get; set; } = false;
        public string lbphFaceRecognizeModel { get; set; } = string.Empty;
        public string FaceClassNames { get; set; } = string.Empty;
    }
}
