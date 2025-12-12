using Geometry.Base;
using System.Collections.Generic;

namespace Geometry.UI.Models
{
    public class RenderFigure
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Figure Figure { get; set; }
        public List<Transform> History { get; set; } = [];
    }
}
