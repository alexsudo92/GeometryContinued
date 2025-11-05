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
                if (v.Vector4.W != 1)
                    v.Normalize();
            }
        }

        public (float MinX, float MinY, float MaxX, float MaxY) GetSizeRectangle()
        {
            var minX = Vertices.Min(x => x.Vector4.X);
            var maxX = Vertices.Max(x => x.Vector4.X);

            var minY = Vertices.Min(x => x.Vector4.Y);
            var maxY = Vertices.Max(x => x.Vector4.Y);

            return (minX, minY, maxX, maxY);
        }
    }
}