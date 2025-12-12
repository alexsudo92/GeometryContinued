using DynamicData;
using Geometry.Base;
using Geometry.Base.Transformation;
using Geometry.UI.Models;
using System;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace Geometry.UI.Services
{
    public class FigureService
    {
        private readonly SourceList<Outline> _lines;
        public IObservable<IChangeSet<Outline>> Lines => _lines.Connect();

        private readonly SourceList<RenderFigure> _figures;
        public IObservable<IChangeSet<RenderFigure>> Figures => _figures.Connect();

        public FigureService()
        {
            _figures = new SourceList<RenderFigure>();
            _lines = new SourceList<Outline>();
        }

        public void AddFigure(FigureYaml figureYaml)
        {
            ArgumentNullException.ThrowIfNull(figureYaml, nameof(figureYaml));

            var renderFigure = new RenderFigure() { Id = _figures.Count + 1, Name = figureYaml.Name, Figure = figureYaml.AsFigure(), History = [] };
            _figures.Add(renderFigure);

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

            RenderAllFigures();
        }

        public void ScaleFigure(int id, float scale, bool shouldRender = true)
        {
            if (_figures.Items[id] is not null)
            {
                var figure = _figures.Items[id].Figure;

                if (scale != 0f && scale != 1f)
                {
                    var m = Affine.GetScaleMatrix(scale);

                    if (scale > 1f)
                        AddToHistory(_figures.Items[id], m, TransformType.ScaleUp);
                    if (scale > 0f && scale < 1f)
                        AddToHistory(_figures.Items[id], m, TransformType.ScaleDown);

                    figure.TransformAllVertices(m);

                    if (shouldRender)
                        RenderAllFigures();
                }
            }
        }

        public void RotateFigure(int id, float angle, Vector4 axis, bool shouldRender = true)
        {
            if (_figures.Items[id] is not null)
            {
                var figure = _figures.Items[id].Figure;

                if (angle != 0f)
                {
                    var m = Affine.GetRotationMatrix(angle, axis);
                    AddToHistory(_figures.Items[id], m, TransformType.Rotation);
                    figure.TransformAllVertices(m);

                    if (shouldRender)
                        RenderAllFigures();
                }
            }
        }

        public void TranslateFigure(int id, Vector3 vector, bool shouldRender = true)
        {
            if (_figures.Items[id] is not null)
            {
                var figure = _figures.Items[id].Figure;

                var m = Affine.GetTranslationMatrix(vector);
                AddToHistory(_figures.Items[id], m, TransformType.Translation);
                figure.TransformAllVertices(m);

                if (shouldRender)
                    RenderAllFigures();
            }
        }

        public async Task SmoothTranslateFigure(int id, Vector2 direction, CancellationToken cancellationToken, bool shouldRender = true)
        {
            if (_figures.Items[id] is not null)
            {
                var figure = _figures.Items[id].Figure;

                var distance = direction.Length();
                var stepVector = Vector2.Normalize(direction);

                for (var i = 0f; i < distance; i += stepVector.Length())
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    var translationMatrix = Affine.GetTranslationMatrix(stepVector.AsVector3());
                    figure.TransformAllVertices(translationMatrix);

                    var collision = false;
                    foreach (var otherItem in _figures.Items)
                    {
                        if (otherItem != _figures.Items[id])
                        {
                            var size = Vector4.Distance(figure.Vertices[0].Vector4, figure.Vertices[1].Vector4) / 2f;
                            var ray = figure.RayCast(otherItem.Figure, direction, size);

                            if (ray)
                            {
                                collision = ray;
                                break;
                            }
                        }
                    }

                    if (collision)
                    {
                        break;
                    }

                    if (shouldRender)
                        RenderAllFigures();

                    await Task.Delay(1);
                }
            }
        }

        public void ShearFigure(int id, float yx = 0f, float xy = 0f, bool shouldRender = true)
        {
            if (_figures.Items[id] is not null)
            {
                var figure = _figures.Items[id].Figure;

                if (yx != 0f || xy != 0f)
                {
                    var m = Affine.GetShearMatrix(yx: yx, xy: xy);
                    AddToHistory(_figures.Items[id], m, TransformType.Shear);
                    figure.TransformAllVertices(m);

                    if (shouldRender)
                        RenderAllFigures();
                }
            }
        }

        public void ProjectFigureToX(int id, float p, bool shouldRender = true)
        {
            if (_figures.Items[id] is not null)
            {
                var figure = _figures.Items[id].Figure;

                if (p != 0f)
                {
                    ReverseProjectFigureToX(id, false);

                    var m = Perspective.ToX(1f / p);
                    AddToHistory(_figures.Items[id], m, TransformType.Perspective);
                    figure.TransformAllVertices(m);

                    if (shouldRender)
                        RenderAllFigures();
                }
            }
        }

        public void ReverseProjectFigureToX(int id, bool shouldRender = true)
        {
            if (_figures.Items[id] is not null)
            {
                var figure = _figures.Items[id].Figure;
                var history = _figures.Items[id].History;

                var last = history.FirstOrDefault(x => x.TransformType == TransformType.Perspective);

                if (last != null)
                {
                    var iM = last.Invert();
                    figure.TransformAllVertices(iM);
                    history.Remove(last);

                    if (shouldRender)
                        RenderAllFigures();
                }
            }
        }

        public void MoveToAndScale(int id, float areaX1, float areaY1, float areaX2, float areaY2, bool shouldRender = true)
        {
            if (_figures.Items[id] is not null)
            {
                var figure = _figures.Items[id].Figure;

                if ((areaX1 != 0f && areaY1 != 0f) || (areaX2 != 0f && areaY2 != 0f))
                {
                    var (MinX, MinY, MaxX, MaxY) = figure.GetSizeRectangle();

                    var midX = (MinX + MaxX) / 2f;
                    var midY = (MinY + MaxY) / 2f;

                    var tX = (areaX2 + areaX1) / 2f - midX;
                    var tY = (areaY2 + areaY1) / 2f - midY;

                    var vector = new Vector3(tX, tY, 0f);
                    TranslateFigure(id, vector, false);

                    var areaWidth = areaX2 - areaX1;
                    var areaHeight = areaY2 - areaY1;

                    var fWidth = MaxX - MinX;
                    var fHeight = MaxY - MinY;

                    var k = MathF.Min(areaWidth / fWidth, areaHeight / fHeight);
                    ScaleFigure(id, k, true);
                }
            }
        }

        private void AddToHistory(RenderFigure figure, Matrix4x4 m, TransformType transformType)
        {
            figure.History.Add(new Transform { TransformType = transformType, Matrix4X4 = m });
        }

        private void RenderAllFigures()
        {
            _lines.Edit(list =>
            {
                list.Clear();

                foreach (var fw in _figures.Items)
                {
                    foreach (var e in fw.Figure.Edges)
                    {
                        var startIdx = e[0];
                        var endIdx = e[1];

                        var startVertex = fw.Figure.Vertices[startIdx];
                        var endVertex = fw.Figure.Vertices[endIdx];

                        list.Add(new Outline
                        {
                            X1 = startVertex.Vector4.X,
                            Y1 = -startVertex.Vector4.Y,
                            X2 = endVertex.Vector4.X,
                            Y2 = -endVertex.Vector4.Y,
                        });
                    }
                }
            });
        }
    }
}
