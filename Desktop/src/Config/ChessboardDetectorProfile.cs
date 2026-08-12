namespace Dplus_Desktop.Config
{
    public class ChessboardDetectorProfile
    {
        public string name { get; set; } = string.Empty;
        public bool useChessboardDetection { get; set; } = false;
        public bool saveChessboardDetections { get; set; } = false;
        public string chessboardToUse { get; set; } = string.Empty;
    }
}
