using Geometry.Base;
using System.Collections.Generic;

namespace Geometry.UI.Services
{
    public class FigureYaml
    {
        public List<List<int>> Vertices { get; set; }
        public List<List<int>> Edges { get; set; }

        public Figure AsFigure()
        {
            var vertices = new List<Point4D>();

            foreach (var v in Vertices)
            {
                var point = new Point4D(v[0], v[1], v[2]);

                vertices.Add(point);
            }

            return new Figure { Edges = Edges, Vertices = vertices };
        }
    }
}
