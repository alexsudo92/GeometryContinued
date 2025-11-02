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
    }
}