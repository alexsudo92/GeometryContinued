using System.Numerics;

namespace Geometry.Base.Transformation
{
    public class Perspective
    {
        public static Matrix4x4 ToX(float p)
        {
            var m = Matrix4x4.Identity;
            m.M14 = p;

            return m;
        }

        public static Matrix4x4 ToY(float p)
        {
            var m = Matrix4x4.Identity;
            m.M24 = p;

            return m;
        }

        public static Matrix4x4 ToZ(float p)
        {
            var m = Matrix4x4.Identity;
            m.M34 = p;

            return m;
        }
    }
}
