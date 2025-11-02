using System.Numerics;

namespace Geometry.Base
{
    public class AffineTransformation
    {
        public static Matrix4x4 GetScaleMatrix(float xScale, float yScale, float zScale)
        {
            return Matrix4x4.CreateScale(xScale, yScale, zScale);
        }

        public static Matrix4x4 GetScaleMatrix(float scale)
        {
            return Matrix4x4.CreateScale(scale);
        }

        public static Matrix4x4 GetRotationMatrix(float angle, Vector4 axis)
        {
            var rad = angle * MathF.PI / 180;

            var matrix = Matrix4x4.Identity;

            if (axis == Vector4.UnitZ)
                matrix = Matrix4x4.CreateRotationZ(rad);
            else if (axis == Vector4.UnitY)
                matrix = Matrix4x4.CreateRotationY(rad);
            else if (axis == Vector4.UnitX)
                matrix = Matrix4x4.CreateRotationX(rad);
            else
                throw new ArgumentException("Can't rotate");

            return matrix;
        }

        public static Matrix4x4 GetTranslationMatrix(Vector3 tVector)
        {
            return Matrix4x4.CreateTranslation(tVector);
        }

        public static Matrix4x4 GetShearMatrix(float xy = 0, float xz = 0,
                                               float yx = 0, float yz = 0,
                                               float zx = 0, float zy = 0)
        {
            return new Matrix4x4
            {
                M11 = 1, M12 = xy, M13 = xz, M14 = 0,
                M21 = yx, M22 = 1, M23 = yz, M24 = 0,
                M31 = zx, M32 = zy, M33 = 1, M34 = 0,
                M41 = 0,
                M42 = 0,
                M43 = 0,
                M44 = 1
            };
        }
    }
}
