using DynamicData;
using Geometry.Base;
using Geometry.Base.Transformation;
using Geometry.UI.Models;
using System;
using System.Diagnostics;
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
                if (scale != 0 && scale != 1f)
                {
                    var m = Affine.GetScaleMatrix(scale);

                    if (scale > 1f)
                        AddToHistory(_figures.Items[id], m, TransformType.ScaleUp);
                    if (scale > 0f && scale < 1f)
                        AddToHistory(_figures.Items[id], m, TransformType.ScaleDown);

                    _figures.Items[id].Figure.TransformAllVertices(m);

                    if (shouldRender)
                        RenderAllFigures();
                }
            }
        }

        public void RotateFigure(int id, float angle, Vector4 axis, bool shouldRender = true)
        {
            if (_figures.Items[id] is not null)
            {
                if (angle != 0)
                {
                    var m = Affine.GetRotationMatrix(angle, axis);
                    AddToHistory(_figures.Items[id], m, TransformType.Rotation);
                    _figures.Items[id].Figure.TransformAllVertices(m);

                    if (shouldRender)
                        RenderAllFigures();
                }
            }
        }

        public void TranslateFigure(int id, Vector3 vector, bool shouldRender = true)
        {
            if (_figures.Items[id] is not null)
            {
                var m = Affine.GetTranslationMatrix(vector);
                AddToHistory(_figures.Items[id], m, TransformType.Translation);
                _figures.Items[id].Figure.TransformAllVertices(m);

                if (shouldRender)
                    RenderAllFigures();
            }
        }

        public async Task SmoothTranslateFigure(int id, Vector3 direction, CancellationToken cancellationToken, bool shouldRender = true)
        {
            if (_figures.Items[id] is not null)
            {
                var distance = direction.Length();
                var stepVector = Vector3.Normalize(direction);

                for (var i = 0; i < distance; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    var translationMatrix = Affine.GetTranslationMatrix(stepVector);
                    _figures.Items[id].Figure.TransformAllVertices(translationMatrix);

                    bool collision = false;
                    foreach (var otherItem in _figures.Items)
                    {
                        if (otherItem != _figures.Items[id])
                        {
                            var size = Vector4.Distance(_figures.Items[id].Figure.Vertices[0].Vector4, _figures.Items[id].Figure.Vertices[1].Vector4) / 2f;
                            var ray = _figures.Items[id].Figure.RayCast(otherItem.Figure, direction, size);
                            Debug.WriteLine(ray);

                            if (ray)
                            {
                                collision = true;
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

        public void ShearFigure(int id, float yx = 0, float xy = 0, bool shouldRender = true)
        {
            if (_figures.Items[id] is not null)
            {
                if (yx != 0 || xy != 0)
                {
                    var m = Affine.GetShearMatrix(yx: yx, xy: xy);
                    AddToHistory(_figures.Items[id], m, TransformType.Shear);
                    _figures.Items[id].Figure.TransformAllVertices(m);

                    if (shouldRender)
                        RenderAllFigures();
                }
            }
        }

        public void ProjectFigureToX(int id, float p, bool shouldRender = true)
        {
            if (_figures.Items[id] is not null)
            {
                if (p != 0)
                {
                    ReverseProjectFigureToX(id, false);

                    var m = Perspective.ToX(1 / p);
                    AddToHistory(_figures.Items[id], m, TransformType.Perspective);
                    _figures.Items[id].Figure.TransformAllVertices(m);

                    if (shouldRender)
                        RenderAllFigures();
                }
            }
        }

        public void ReverseProjectFigureToX(int id, bool shouldRender = true)
        {
            if (_figures.Items[id] is not null)
            {
                var last = _figures.Items[id].History
                    .FirstOrDefault(x => x.TransformType == TransformType.Perspective);

                if (last != null)
                {
                    var iM = last.Invert();
                    _figures.Items[id].Figure.TransformAllVertices(iM);
                    _figures.Items[id].History.Remove(last);

                    if (shouldRender)
                        RenderAllFigures();
                }
            }
        }

        public void MoveToAndScale(int id, float areaX1, float areaY1, float areaX2, float areaY2, bool shouldRender = true)
        {
            if (_figures.Items[id] is not null)
            {
                if ((areaX1 != 0 && areaY1 != 0) || (areaX2 != 0 && areaY2 != 0))
                {
                    var (MinX, MinY, MaxX, MaxY) = _figures.Items[id].Figure.GetSizeRectangle();

                    var midX = (MinX + MaxX) / 2;
                    var midY = (MinY + MaxY) / 2;

                    var tX = (areaX2 + areaX1) / 2 - midX;
                    var tY = (areaY2 + areaY1) / 2 - midY;

                    var vector = new Vector3(tX, tY, 0);
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
