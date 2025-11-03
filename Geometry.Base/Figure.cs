using System.Numerics;

namespace Geometry.Base
{
    public class Figure
    {
        public List<Point4D> Vertices { get; set; }
        public List<List<int>> Edges { get; set; }

        public void TransformAllVertices(Matrix4x4 matrix)
        {
            foreach (var v in Vertices)
            {
                v.Transform(matrix);
            }
        }

        public (Point4D Min, Point4D Max) GetSizeRectangle()
        {
            var minX = Vertices.Min(x => x.Vector4.X);
            var maxX = Vertices.Max(x => x.Vector4.X);

            var minY = Vertices.Min(x => x.Vector4.Y);
            var maxY = Vertices.Max(x => x.Vector4.Y);

            return (new Point4D(minX, minY,0), new Point4D(maxX, maxY, 0));
        }

        public void Normalize()
        {
            foreach (var v in Vertices)
            {
                var w = v.Vector4.W;
                v.Vector4 = new Vector4(v.Vector4.X / w,
                                        v.Vector4.Y / w,
                                        v.Vector4.Z / w,
                                        1);
            }
        }
    }
}