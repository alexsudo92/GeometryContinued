using System.Numerics;

namespace Geometry.Base.Transformation
{
    public class Affine
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
            var rad = angle * MathF.PI / 180f;

            if (axis == Vector4.UnitZ)
                return Matrix4x4.CreateRotationZ(rad);
            else if (axis == Vector4.UnitY)
                return Matrix4x4.CreateRotationY(rad);
            else
                return Matrix4x4.CreateRotationX(rad);
        }

        public static Matrix4x4 GetTranslationMatrix(Vector3 tVector)
        {
            return Matrix4x4.CreateTranslation(tVector);
        }

        public static Matrix4x4 GetShearMatrix(float xy = 0f, float xz = 0f,
                                               float yx = 0f, float yz = 0f,
                                               float zx = 0f, float zy = 0f)
        {
            return new Matrix4x4
            {
                M11 = 1f,
                M12 = xy,
                M13 = xz,
                M14 = 0,
                M21 = yx,
                M22 = 1f,
                M23 = yz,
                M24 = 0,
                M31 = zx,
                M32 = zy,
                M33 = 1f,
                M34 = 0,
                M41 = 0,
                M42 = 0,
                M43 = 0,
                M44 = 1f
            };
        }
    }
}
