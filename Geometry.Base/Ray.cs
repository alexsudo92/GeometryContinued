using System.Numerics;
namespace Geometry.Base
{
    public class Ray(Vector2 position, Vector2 direction)
    {
        public Vector2 Position { get; } = position;
        public Vector2 Direction { get; } = Vector2.Normalize(direction);
    }
}