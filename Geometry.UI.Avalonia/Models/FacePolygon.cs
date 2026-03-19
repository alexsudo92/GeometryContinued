using Avalonia;
using Avalonia.Media;
using System.Collections.Generic;

namespace Geometry.UI.Avalonia.Models;

public class FacePolygon
{
    public IList<Point> Points { get; set; } = [];
    public Color FillColor { get; set; }
    public float Depth { get; set; }
}
