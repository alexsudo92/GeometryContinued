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

        public bool CheckCollision(Figure f)
        {
            var (MinX, MinY, MaxX, MaxY) = GetSizeRectangle();
            var rectB = f.GetSizeRectangle();

            return !(MaxX < rectB.MinX ||
                     rectB.MaxX < MinX ||
                     MaxY < rectB.MinY ||
                     rectB.MaxY < MinY);
        }

        public bool RayCast(Figure other, Vector3 direction, float maxDistance)
        {
            var dir = Vector3.Normalize(direction);

            foreach (var vertex in Vertices)
            {
                var (MinX, MinY, MaxX, MaxY) = GetSizeRectangle();
                var originCenter = new Vector3((MaxX + MinX) / 2, (MaxY + MinY) / 2, 0f);
                var originVertex = new Vector3(vertex.Vector4.X, vertex.Vector4.Y, vertex.Vector4.Z);

                var ray = new Ray(originCenter, dir);
                var ray2 = new Ray(originVertex, dir);

                foreach (var edge in other.Edges)
                {
                    var a = other.Vertices[edge[0]].Vector4;
                    var b = other.Vertices[edge[1]].Vector4;

                    var rayCenter = RayIntersectsSegment(ray, a, b, out float distance);
                    var rayVertex = RayIntersectsSegment(ray2, a, b, out float distance2);

                    if ((rayCenter || rayVertex) && (distance <= maxDistance || distance2 < 1f))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool RayIntersectsSegment(Ray ray, Vector4 a, Vector4 b, out float distanceAlongRay)
        {
            distanceAlongRay = float.MaxValue;

            var segDir = b - a;
            var denom = ray.Direction.X * (-segDir.Y) + ray.Direction.Y * segDir.X;

            if (MathF.Abs(denom) < float.Epsilon)
                return false;

            var t = ((a.X - ray.Position.X) * (-segDir.Y) + (a.Y - ray.Position.Y) * segDir.X) / denom;
            var u = ((a.X - ray.Position.X) * (-ray.Direction.Y) + (a.Y - ray.Position.Y) * ray.Direction.X) / denom;

            if (t >= 0 && u >= 0 && u <= 1)
            {
                distanceAlongRay = t;
                return true;
            }

            return false;
        }
    }
}