using DynamicData;
using Geometry.Base.Transformation;
using Geometry.UI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Geometry.UI.Services
{
    public class FigureService
    {
        private readonly SourceList<Line> _lines;
        public IObservable<IChangeSet<Line>> Lines => _lines.Connect();

        private readonly SourceList<FacePolygon> _polygons;
        public IObservable<IChangeSet<FacePolygon>> Polygons => _polygons.Connect();

        private readonly SourceList<FigureModel> _figures;
        public IObservable<IChangeSet<FigureModel>> Figures => _figures.Connect();

        public FigureService()
        {
            _figures = new SourceList<FigureModel>();
            _lines = new SourceList<Line>();
            _polygons = new SourceList<FacePolygon>();
        }

        public void AddFigure(FigureModel figureModel)
        {
            figureModel.Id = _figures.Count + 1;
            _figures.Add(figureModel);

            RenderAllFigures();
        }

        public void RemoveFigure(int id)
        {
            _figures.RemoveAt(id);

            RenderAllFigures();
        }

        public void ClearFigures()
        {
            _figures.Clear();
            _lines.Clear();
            _polygons.Clear();

            RenderAllFigures();
        }

        public void ScaleFigure(int id, float scale)
        {
            if (_figures.Items[id] is not null)
            {
                var figure = _figures.Items[id];

                if (scale != 0f && scale != 1f)
                {
                    var m = Affine.GetScaleMatrix(scale);

                    figure.TransformAllVertices(m);

                    RenderAllFigures();
                }
            }
        }

        public void RotateFigure(int id, float angle, Vector4 axis)
        {
            if (_figures.Items[id] is not null)
            {
                var figure = _figures.Items[id];

                if (angle != 0f)
                {
                    var m = Affine.GetRotationMatrix(angle, axis);
                    figure.TransformAllVertices(m);

                    RenderAllFigures();
                }
            }
        }

        public void TranslateFigure(int id, Vector3 vector)
        {
            if (_figures.Items[id] is not null)
            {
                var figure = _figures.Items[id];

                var m = Affine.GetTranslationMatrix(vector);
                figure.TransformAllVertices(m);

                RenderAllFigures();
            }
        }

        public async Task SmoothTranslateFigure(int id, Vector2 direction, CancellationToken cancellationToken)
        {
            if (_figures.Items[id] is not null)
            {
                var figure = _figures.Items[id];

                var distance = direction.Length();
                var stepVector = Vector2.Normalize(direction);
                var stepVectorLength = stepVector.Length();
                var stepVector3 = stepVector.AsVector3();

                for (var i = 0f; i < distance; i += stepVectorLength)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    var translationMatrix = Affine.GetTranslationMatrix(stepVector3);
                    figure.TransformAllVertices(translationMatrix);

                    RenderAllFigures();

                    await Task.Delay(5, cancellationToken);
                }
            }
        }

        public void ShearFigure(int id, float yx = 0f, float xy = 0f)
        {
            if (_figures.Items[id] is not null)
            {
                var figure = _figures.Items[id];

                if (yx != 0f || xy != 0f)
                {
                    var m = Affine.GetShearMatrix(yx: yx, xy: xy);
                    figure.TransformAllVertices(m);

                    RenderAllFigures();
                }
            }
        }

        public void ProjectFigureToX(int id, float p)
        {
            if (_figures.Items[id] is not null)
            {
                var figure = _figures.Items[id];

                if (p != 0f)
                {
                    var m = Perspective.ToX(1f / p);
                    figure.TransformAllVertices(m);

                    RenderAllFigures();
                }
            }
        }

        public void MoveToAndScale(int id, float areaX1, float areaY1, float areaX2, float areaY2)
        {
            if (_figures.Items[id] is not null)
            {
                var figure = _figures.Items[id];

                if ((areaX1 != 0f && areaY1 != 0f) || (areaX2 != 0f && areaY2 != 0f))
                {
                    var (MinX, MinY, MaxX, MaxY) = figure.GetSizeRectangle();

                    var midX = (MinX + MaxX) / 2f;
                    var midY = (MinY + MaxY) / 2f;

                    var tX = (areaX2 + areaX1) / 2f - midX;
                    var tY = (areaY2 + areaY1) / 2f - midY;

                    var vector = new Vector3(tX, tY, 0f);
                    TranslateFigure(id, vector);

                    var areaWidth = areaX2 - areaX1;
                    var areaHeight = areaY2 - areaY1;

                    var fWidth = MaxX - MinX;
                    var fHeight = MaxY - MinY;

                    var k = MathF.Min(areaWidth / fWidth, areaHeight / fHeight);
                    ScaleFigure(id, k);
                }
            }
        }

        public void ChangeFigureColor(int id, Color color)
        {
            if (_figures.Items[id] is not null)
            {
                var figure = _figures.Items[id];
                figure.Color = color;

                foreach (var face in figure.Faces)
                {
                    face.Color.R = color.R;
                    face.Color.G = color.G;
                    face.Color.B = color.B;
                    face.Color.A = color.A;
                }

                RenderAllFigures();
            }
        }

        private static bool IsClosedFigure(IReadOnlyCollection<Geometry.Base.Face> faces)
        {
            var edgeUsage = new Dictionary<(int Start, int End), int>();

            foreach (var face in faces)
            {
                if (face.Vertices.Count < 3)
                {
                    continue;
                }

                for (var i = 0; i < face.Vertices.Count; i++)
                {
                    var start = face.Vertices[i];
                    var end = face.Vertices[(i + 1) % face.Vertices.Count];
                    var edge = start < end ? (start, end) : (end, start);

                    edgeUsage.TryGetValue(edge, out var usageCount);
                    edgeUsage[edge] = usageCount + 1;
                }
            }

            return edgeUsage.Count > 0 && edgeUsage.Values.All(count => count == 2);
        }

        private void RenderAllFigures()
        {
            var cameraDirection = new Vector4(0f, 0f, -1f, 0f);
            var polygons = new List<FacePolygon>();
            var lines = new List<Line>();

            foreach (var fw in _figures.Items)
            {
                var isClosedFigure = IsClosedFigure(fw.Faces);

                foreach (var face in fw.Faces)
                {
                    var faceColor = Color.FromArgb(face.Color.A, face.Color.R, face.Color.G, face.Color.B);
                    var faceVertices = face.Vertices;

                    if (faceVertices.Count < 3)
                    {
                        continue;
                    }

                    var v1 = fw.Vertices[faceVertices[0]];
                    var v2 = fw.Vertices[faceVertices[1]];
                    var v3 = fw.Vertices[faceVertices[2]];

                    var a = v2 - v1;
                    var b = v3 - v1;

                    var normal = Vector4.Cross(a, b);
                    var dot = Vector4.Dot(normal, cameraDirection);

                    if (isClosedFigure && dot <= 0f)
                    {
                        continue;
                    }

                    var points = new PointCollection();

                    for (var i = 0; i < faceVertices.Count; i++)
                    {
                        var vertex = fw.Vertices[faceVertices[i]];
                        var point = new Point(vertex.X, -vertex.Y);
                        points.Add(point);

                        var nextVertex = fw.Vertices[faceVertices[(i + 1) % faceVertices.Count]];
                        lines.Add(new Line
                        {
                            X1 = vertex.X,
                            Y1 = -vertex.Y,
                            X2 = nextVertex.X,
                            Y2 = -nextVertex.Y,
                            Color = Colors.Black
                        });
                    }

                    polygons.Add(new FacePolygon
                    {
                        Points = points,
                        FillColor = faceColor
                    });
                }
            }

            _polygons.Edit(list =>
            {
                list.Clear();
                list.AddRange(polygons);
            });

            _lines.Edit(list =>
            {
                list.Clear();
                list.AddRange(lines);
            });
        }
    }
}
