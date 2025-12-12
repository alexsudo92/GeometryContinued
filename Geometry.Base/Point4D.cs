using System.Numerics;

namespace Geometry.Base
{
    public class Point4D
    {
        public Vector4 Vector4 { get; set; }

        public Point4D(float x, float y, float z)
        {
            Vector4 = new Vector4(x, y, z, 1f);
        }

        public void Transform(Matrix4x4 matrix)
        {
            Vector4 = Vector4.Transform(Vector4, matrix);
        }

        public void Normalize()
        {
            var w = Vector4.W;
            Vector4 = new Vector4(Vector4.X / w,
                                  Vector4.Y / w,
                                  Vector4.Z / w,
                                  1f);
        }
    }
}