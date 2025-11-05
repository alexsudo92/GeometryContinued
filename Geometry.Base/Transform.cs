using System.Numerics;

namespace Geometry.Base
{
    public class Transform
    {
        public Matrix4x4 Matrix4X4 { get; set; }
        public TransformType TransformType { get; set; }

        public Matrix4x4 Invert()
        {
            var iM = Matrix4x4.Identity;
            Matrix4x4.Invert(Matrix4X4, out iM);

            return iM;
        }
    }
}
