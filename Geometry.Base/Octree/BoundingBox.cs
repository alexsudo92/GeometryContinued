using System.Numerics;

namespace Geometry.Base.Octree
{
    // Ограничивающая рамка
    public struct BoundingBox
    {
        public Vector3 Center { get; set; }
        public Vector3 Size { get; set; }

        public Vector3 Min { get; set; }
        public Vector3 Max { get; set; }
        public Vector3 Extents { get; set; }

        public BoundingBox(Vector3 center, float size)
        {
            Center = center;
            Size = new Vector3(size);

            var halfSize = size / 2;
            Min = new Vector3(center.X - halfSize, center.Y - halfSize, center.Z - halfSize);
            Max = new Vector3(center.X + halfSize, center.Y + halfSize, center.Z + halfSize);
            Extents = new Vector3(halfSize, halfSize, halfSize);
        }

        public BoundingBox(Vector3 center, Vector3 size)
        {
            Center = center;
            Size = size;

            var halfX = size.X / 2f;
            var halfY = size.Y / 2f;
            var halfZ = size.Z / 2f;
            Min = new Vector3(center.X - halfX, center.Y - halfY, center.Z - halfZ);
            Max = new Vector3(center.X + halfX, center.Y + halfY, center.Z + halfZ);
            Extents = new Vector3(halfX, halfY, halfZ);
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
