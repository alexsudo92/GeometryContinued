using Geometry.Base;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace Geometry.UI.Models
{
    public class FigureModel : Figure
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Color Color { get; set; } = Color.FromArgb(255, 0, 0, 0);

        public FigureModel Clone()
        {
            return new FigureModel
            {
                Id = Id,
                Name = Name,
                Color = Color,
                Vertices = [.. Vertices],
                Faces = [.. Faces.Select(face => new List<int>(face))]
            };
        }
    }
}