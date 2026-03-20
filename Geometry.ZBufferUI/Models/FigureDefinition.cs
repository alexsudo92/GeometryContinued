using Geometry.Base;
using System.Collections.Generic;
using System.Numerics;
using System.Windows.Media;

namespace Geometry.ZBufferUI.Models;

public class FigureDefinition
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Vector4> Vertices { get; set; } = [];
    public List<Triangle> Triangles { get; set; } = [];
    public Color Color { get; set; } = Colors.LightGray;
}
