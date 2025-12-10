using System.Numerics;

namespace Geometry.Base.Octree
{
    public struct BoundingBox(Vector3 center, float size)
    {
        public Vector3 Center { get; set; } = center;
        public float Size { get; set; } = size;

        public readonly Vector3 Max => new(Center.X + Size / 2f, Center.Y + Size / 2f, Center.Z + Size / 2f);
        public readonly Vector3 Min => new(Center.X - Size / 2f, Center.Y - Size / 2f, Center.Z - Size / 2f);

        public readonly bool ContainsPoint(Vector3 point)
        {
            var min = Min;
            var max = Max;
            return point.X >= min.X && point.X <= max.X &&
                   point.Y >= min.Y && point.Y <= max.Y &&
                   point.Z >= min.Z && point.Z <= max.Z;
        }
    }
}
