using System.Numerics;

namespace Geometry.Base.Octree
{
    // Геометрические примитивы
    public static class GeometricPrimitives
    {
        // Сфера с центром и радиусом
        public static Func<Vector3, bool> CreateSphere(Vector3 center, float radius)
        {
            return point =>
            {
                var distance = Vector3.Distance(point, center);
                return distance <= radius;
            };
        }

        // Параллелепипед (прямоугольный бокс) с центром и размерами
        public static Func<Vector3, bool> CreateBox(Vector3 center, Vector3 size)
        {
            return point =>
            {
                var min = center - size * 0.5f;
                var max = center + size * 0.5f;
                return point.X >= min.X && point.X <= max.X &&
                       point.Y >= min.Y && point.Y <= max.Y &&
                       point.Z >= min.Z && point.Z <= max.Z;
            };
        }
    }
}
