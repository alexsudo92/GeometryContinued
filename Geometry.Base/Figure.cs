namespace Geometry.Base
{
    public class Figure
    {
        public List<Point3D> Vertices { get; set; }
        public Dictionary<Point3D, Point3D> Bounds { get; set; }
    }
}