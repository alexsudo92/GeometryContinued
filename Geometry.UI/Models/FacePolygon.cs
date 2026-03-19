using System.Windows.Media;

namespace Geometry.UI.Models
{
    public class PolygonModel
    {
        public PointCollection Points { get; set; } = [];
        public Color FillColor { get; set; }
        public float Depth { get; set; }
    }
}
