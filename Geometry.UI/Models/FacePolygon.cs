using System.Windows.Media;
using System.Windows;

namespace Geometry.UI.Models
{
    public class FacePolygon
    {
        public PointCollection Points { get; set; } = [];
        public Color FillColor { get; set; }
        public float Depth { get; set; }
    }
}
