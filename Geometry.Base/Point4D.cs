using System.Numerics;

namespace Geometry.Base
{
    public class Point4D
    {
        public Vector4 Vector4 { get; set; }

        public Point4D(float x, float y, float z)
        {
            Vector4 = new Vector4(x, y, z, 1);
        }
        
        public void Transform(Matrix4x4 matrix)
        {
            this.Vector4 = Vector4.Transform(this.Vector4, matrix);
        }
    }
}