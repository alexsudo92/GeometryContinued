using System.Numerics;
namespace Geometry.Base
{
    public class Ray(Vector3 position, Vector3 direction)
    {
        public Vector3 Position { get; } = position;
        public Vector3 Direction { get; } = Vector3.Normalize(direction);
    }
}