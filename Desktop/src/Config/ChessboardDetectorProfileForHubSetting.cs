namespace Dplus_Desktop.Config
{
    public class ChessboardDetectorProfileForHubSetting
    {
        public string name { get; set; } = string.Empty;
        public bool useChessboardDetection { get; set; } = false;
        public bool saveChessboardDetections { get; set; } = false;
        public ChessboardParameters chessboard { get; set; } = new ChessboardParameters();
    }
}
