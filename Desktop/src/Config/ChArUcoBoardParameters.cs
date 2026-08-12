namespace Dplus_Desktop.Config
{
    public class ChArUcoBoardParameters
    {
        public string name { get; set; } = string.Empty;
        public int squaresX { get; set; } = 0;
        public int squaresY { get; set; } = 0;
        public int minDetections { get; set; } = 10;
        public float squareLength { get; set; } = 0.0f;
        public float markerLength { get; set; } = 0.0f;
        public int dictionaryID { get; set; } = 0;
    }
}
