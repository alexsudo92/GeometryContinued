namespace Geometry.Base
{
    public class Triangle
    {
        public List<int> Vertices { get; set; } = [];
        public FaceColor Color { get; set; } = new();
    }

    public class FaceColor
    {
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public byte A { get; set; } = 255;
    }
}
