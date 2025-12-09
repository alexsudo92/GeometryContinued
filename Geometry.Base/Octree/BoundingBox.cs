using System.Numerics;

namespace Geometry.Base.Octree
{
    public struct BoundingBox
    {
        public Vector3 Center { get; set; }
        public Vector3 Size { get; set; }

        public readonly Vector3 Max => new(Center.X + Size.X / 2f, Center.Y + Size.Y / 2f, Center.Z + Size.Z / 2f);
        public readonly Vector3 Min => new(Center.X - Size.X / 2f, Center.Y - Size.Y / 2f, Center.Z - Size.Z / 2f);

        public BoundingBox(Vector3 center, Vector3 size)
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
