namespace MapGenerationProject.DOTS
{
    public static class GridData
    {
        public static readonly int Width;
        public static readonly int Height;
        
        static GridData()
        {
            Width = HexGrid.Width;
            Height = HexGrid.Height;
        }
    }
}