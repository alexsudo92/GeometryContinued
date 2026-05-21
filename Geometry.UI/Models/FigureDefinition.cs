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
    public BspNodeDefinition? BspTree { get; set; }
    public Color Color { get; set; } = Colors.LightGray;
}

public class BspNodeDefinition
{
    public List<Triangle> Polygons { get; set; } = [];
    public BspNodeDefinition? Front { get; set; }
    public BspNodeDefinition? Back { get; set; }
}
