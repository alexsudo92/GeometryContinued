using System.Collections.Generic;
using System.Windows.Media;

namespace Geometry.Base
{
    public class Face
    {
        public List<int> Vertices { get; set; } = [];
        public Color Color { get; set; } = Color.FromArgb(255, 0, 0, 0);
    }
}
