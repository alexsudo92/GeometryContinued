using System.Numerics;

namespace Geometry.Base
{
    public class Point3D
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public Vector4 Vector4 { get; set; }

        public Point3D(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
            Vector4 = new Vector4(X, Y, Z, 1);
        }

        public Vector4 Scale(float xScale, float yScale, float zScale)
        {
            var matrix = new Matrix4x4
            {
                M11 = xScale, M12 = 0, M13 = 0, M14 = 0,
                M21 = 0, M22 = yScale, M23 = 0, M24 = 0,
                M31 = 0, M32 = 0, M33 = zScale, M34 = 0,
                M41 = 0, M42 = 0, M43 = 0, M44 = 1
            };

            return Vector4.Transform(this.Vector4, matrix);
        }

         public Vector4 Scale(float scale)
        {
            var matrix = new Matrix4x4
            {
                M11 = scale, M12 = 0, M13 = 0, M14 = 0,
                M21 = 0, M22 = scale, M23 = 0, M24 = 0,
                M31 = 0, M32 = 0, M33 = scale, M34 = 0,
                M41 = 0, M42 = 0, M43 = 0, M44 = 1
            };

            return Vector4.Transform(this.Vector4, matrix);
        }

        public Vector4 Rotate(float angle, AxisType type)
        {
            var rad = angle * MathF.PI / 180;

            var matrix = type switch
            {
                AxisType.Z => new Matrix4x4
                {
                    M11 = MathF.Cos(rad), M12 = MathF.Sin(rad), M13 = 0, M14 = 0,
                    M21 = -MathF.Sin(rad), M22 = MathF.Cos(rad), M23 = 0, M24 = 0,
                    M31 = 0, M32 = 0, M33 = 1, M34 = 0,
                    M41 = 0, M42 = 0, M43 = 0, M44 = 1
                },
                AxisType.Y => new Matrix4x4
                {
                    M11 = MathF.Cos(rad), M12 = 0, M13 = -MathF.Sin(rad), M14 = 0,
                    M21 = 0, M22 = 1, M23 = 0, M24 = 0,
                    M31 = MathF.Sin(rad), M32 = 0, M33 = MathF.Cos(rad), M34 = 0,
                    M41 = 0, M42 = 0, M43 = 0, M44 = 1
                },
                AxisType.X => new Matrix4x4
                {
                    M11 = 1, M12 = 0, M13 = 0, M14 = 0,
                    M21 = 0, M22 = MathF.Cos(rad), M23 = MathF.Sin(rad), M24 = 0,
                    M31 = 0, M32 = -MathF.Sin(rad), M33 = MathF.Cos(rad), M34 = 0,
                    M41 = 0, M42 = 0, M43 = 0, M44 = 1
                },
                _ => throw new NotImplementedException()
            };

            return Vector4.Transform(this.Vector4, matrix);
        }

        public Vector4 Translate(Vector3 tVector)
        {
            var matrix = new Matrix4x4
            {
                M11 = 1, M12 = 0, M13 = 0, M14 = 0,
                M21 = 0, M22 = 1, M23 = 0, M24 = 0,
                M31 = 0, M32 = 0, M33 = 1, M34 = 0,
                M41 = tVector.X, M42 = tVector.Y, M43 = tVector.Z, M44 = 1
            };

            return Vector4.Transform(this.Vector4, matrix);
        }

        public Vector4 Shear(float d)
        {
            var matrix = new Matrix4x4
            {
                M11 = 1, M12 = 0, M13 = 0, M14 = d,
                M21 = 0, M22 = 1, M23 = 0, M24 = 0,
                M31 = 0, M32 = 0, M33 = 1, M34 = 0,
                M41 = 0, M42 = 0, M43 = 0, M44 = 1
            };

            return Vector4.Transform(this.Vector4, matrix);
        }
    }
}