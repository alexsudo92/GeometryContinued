using System.Numerics;

namespace Geometry.Base.Octree
{
    // Ограничивающая рамка
    public struct BoundingBox
    {
        public Vector3 Center { get; set; }
        public float Size { get; set; }

        public Vector3 Max => new(Center.X + Size / 2f, Center.Y + Size / 2f, Center.Z + Size / 2f);
        public Vector3 Min => new(Center.X - Size / 2f, Center.Y - Size / 2f, Center.Z - Size / 2f);

        public BoundingBox(Vector3 center, float size)
        {
            Center = center;
            Size = size;
        }

        public bool ContainsPoint(Vector3 point)
        {
            var min = Min;
            var max = Max;
            return point.X >= min.X && point.X <= max.X &&
                   point.Y >= min.Y && point.Y <= max.Y &&
                   point.Z >= min.Z && point.Z <= max.Z;
        }
    }
}
