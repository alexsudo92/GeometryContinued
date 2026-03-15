using Geometry.Base;
using System.Collections.Generic;
using System.Linq;

namespace Geometry.UI.Models
{
    public class FigureModel : Figure
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public FigureModel Clone()
        {
            return new FigureModel
            {
                Id = Id,
                Name = Name,
                Vertices = [.. Vertices],
                Faces = [.. Faces.Select(face => new List<int>(face))]
            };
        }
    }
}