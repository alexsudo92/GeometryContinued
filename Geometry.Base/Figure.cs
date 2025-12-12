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
                if (v.Vector4.W != 1f)
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

        public bool RayCast(Figure other, Vector2 direction, float maxDistance)
        {
            var normalizedDirection = Vector2.Normalize(direction);

            foreach (var vertex in Vertices)
            {
                var (MinX, MinY, MaxX, MaxY) = GetSizeRectangle();
                var originCenter = new Vector2((MaxX + MinX) / 2f, (MaxY + MinY) / 2f);
                var originVertex = new Vector2(vertex.Vector4.X, vertex.Vector4.Y);

                var ray = new Ray(originCenter, normalizedDirection);
                var ray2 = new Ray(originVertex, normalizedDirection);

                foreach (var edge in other.Edges)
                {
                    var a = other.Vertices[edge[0]].Vector4.AsVector2();
                    var b = other.Vertices[edge[1]].Vector4.AsVector2();

                    var rayCenter = RayIntersectsSegment(ray, a, b);
                    var (Intersects, Distance) = RayIntersectsSegment(ray2, a, b);

                    if ((rayCenter.Intersects && rayCenter.Distance <= maxDistance) || (Intersects && Distance < 1f))
                        return true;
                }
            }

            return false;
        }

        private static (bool Intersects, float Distance) RayIntersectsSegment(Ray ray, Vector2 a, Vector2 b)
        {
            var segmentVector = b - a;
            var denom = Vector2.Cross(segmentVector, ray.Direction);

            if (MathF.Abs(denom) < float.Epsilon)
                return (false, 0f);

            var distance = -Vector2.Cross(a - ray.Position, segmentVector) / denom;
            var intersection = -Vector2.Cross(a - ray.Position, ray.Direction) / denom;

            if (distance >= 0f && intersection >= 0f && intersection <= 1f)
                return (true, distance);

            return (false, 0f);
        }
    }
}