using Geometry.Base;
using System.Collections.Generic;
using System.Numerics;
using System.Windows.Media;

namespace Geometry.UI.Models;

public class FigureDefinition
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Vector4> Vertices { get; set; } = [];
    public List<Triangle> Triangles { get; set; } = [];
    public Color Color { get; set; } = Colors.LightGray;
    public BspNode? BspTree { get; set; }
}

public class BspNode
{
    public List<int>? Polygon { get; set; }
    public Color? Color { get; set; }
    public BspNode? Front { get; set; }
    public BspNode? Back { get; set; }
}
