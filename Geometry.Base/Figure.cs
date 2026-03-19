using System.Numerics;

namespace Geometry.Base
{
    public class Figure
    {
        public List<Vector4> Vertices { get; set; } = [];
        public List<Face> Faces { get; set; } = [];

        public void TransformAllVertices(Matrix4x4 matrix)
        {
            for (var i = 0; i < Vertices.Count; i++)
            {
                Vertices[i] = Vector4.Transform(Vertices[i], matrix);
                if (Vertices[i].W != 1f)
                    Vertices[i] = new Vector4(
                        Vertices[i].X / Vertices[i].W,
                        Vertices[i].Y / Vertices[i].W,
                        Vertices[i].Z / Vertices[i].W,
                        1f);
            }
        }

        public (float MinX, float MinY, float MaxX, float MaxY) GetSizeRectangle()
        {
            var minX = Vertices.Min(x => x.X);
            var maxX = Vertices.Max(x => x.X);

            var minY = Vertices.Min(x => x.Y);
            var maxY = Vertices.Max(x => x.Y);

            return (minX, minY, maxX, maxY);
        }
    }
}
