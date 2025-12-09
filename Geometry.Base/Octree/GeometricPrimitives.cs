using System.Numerics;

namespace Geometry.Base.Octree
{
    public static class GeometricPrimitives
    {
        public static Func<Vector3, bool> CreateSphere(Vector3 center, float radius)
        {
            return point =>
            {
                var distance = Vector3.Distance(point, center);
                return distance <= radius;
            };
        }

        public static Func<Vector3, bool> CreateCylinder(Vector3 center, Vector3 axis, float radius, float height)
        {
            return point =>
            {
                var toPoint = point - center;
                var projection = Vector3.Dot(toPoint, axis);

                if (projection < 0 || projection > height)
                    return false;

                var perpendicular = toPoint - projection * axis;

                return perpendicular.LengthSquared() <= radius * radius;
            };
        }
    }
}
