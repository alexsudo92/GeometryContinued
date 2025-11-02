using System.Numerics;

namespace Geometry.Base
{
    public class Transform
    {
        public Matrix4x4 Matrix4X4 { get; set; }
        public TransformType TransformType { get; set; }
    }
}
