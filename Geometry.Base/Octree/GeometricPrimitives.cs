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
                // Вектор от точки к центру основания
                var toPoint = point - center;

                // Проекция на ось
                var projection = Vector3.Dot(toPoint, axis);

                // Проверка высоты
                if (projection < 0 || projection > height)
                    return false;

                // Перпендикулярная составляющая
                var perpendicular = toPoint - projection * axis;

                // Проверка радиуса
                return perpendicular.LengthSquared() <= radius * radius;
            };
        }
    }
}
