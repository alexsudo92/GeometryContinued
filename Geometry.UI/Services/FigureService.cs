using DynamicData;
using Geometry.Base.Transformation;
using Geometry.UI.Models;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Geometry.UI.Services
{
    public class FigureService
    {
        private readonly SourceList<Line> _lines;
        public IObservable<IChangeSet<Line>> Lines => _lines.Connect();

        private readonly SourceList<FigureModel> _figures;
        public IObservable<IChangeSet<FigureModel>> Figures => _figures.Connect();

        public FigureService()
        {
            _figures = new SourceList<FigureModel>();
            _lines = new SourceList<Line>();
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

            RenderAllFigures();
        }

        public void ScaleFigure(int id, float scale, bool shouldRender = true)
        {
            if (_figures.Items[id] is not null)
            {
                var figure = _figures.Items[id];

                if (scale != 0f && scale != 1f)
                {
                    var m = Affine.GetScaleMatrix(scale);

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
                var figure = _figures.Items[id];

                if (angle != 0f)
                {
                    var m = Affine.GetRotationMatrix(angle, axis);
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
                var figure = _figures.Items[id];

                var m = Affine.GetTranslationMatrix(vector);
                figure.TransformAllVertices(m);

                if (shouldRender)
                    RenderAllFigures();
            }
        }

        public async Task SmoothTranslateFigure(int id, Vector2 direction, CancellationToken cancellationToken, bool shouldRender = true)
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

                    await Task.Delay(1);
                }
            }
        }

        public void ShearFigure(int id, float yx = 0f, float xy = 0f, bool shouldRender = true)
        {
            if (_figures.Items[id] is not null)
            {
                var figure = _figures.Items[id];

                if (yx != 0f || xy != 0f)
                {
                    var m = Affine.GetShearMatrix(yx: yx, xy: xy);
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
                var figure = _figures.Items[id];

                if (p != 0f)
                {
                    var m = Perspective.ToX(1f / p);
                    figure.TransformAllVertices(m);

                    if (shouldRender)
                        RenderAllFigures();
                }
            }
        }

        public void MoveToAndScale(int id, float areaX1, float areaY1, float areaX2, float areaY2, bool shouldRender = true)
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

        private void RenderAllFigures()
        {
            _lines.Edit(list =>
            {
                list.Clear();

                foreach (var fw in _figures.Items)
                {
                    var cameraDirection = new Vector4(0, 0, -1, 1);

                    foreach (var face in fw.Faces)
                    {
                        var v1 = fw.Vertices[face[0]];
                        var v2 = fw.Vertices[face[1]];
                        var v3 = fw.Vertices[face[2]];

                        var a = v2 - v1;
                        var b = v3 - v1;

                        var normal = Vector4.Cross(a, b);

                        if (Vector4.Dot(normal, cameraDirection) > 0)
                        {
                            var linesToAdd = new List<Line>
                            {
                                new Line
                                {
                                    X1 = v1.X,
                                    Y1 = -v1.Y,
                                    X2 = v2.X,
                                    Y2 = -v2.Y
                                },
                                new Line
                                {
                                    X1 = v2.X,
                                    Y1 = -v2.Y,
                                    X2 = v3.X,
                                    Y2 = -v3.Y
                                }
                            };

                            // Для четырехугольников добавляем третью и четвертую стороны
                            if (face.Count >= 4)
                            {
                                var v4 = fw.Vertices[face[3]];

                                linesToAdd.Add(new Line
                                {
                                    X1 = v3.X,
                                    Y1 = -v3.Y,
                                    X2 = v4.X,
                                    Y2 = -v4.Y
                                });
                                linesToAdd.Add(new Line
                                {
                                    X1 = v4.X,
                                    Y1 = -v4.Y,
                                    X2 = v1.X,
                                    Y2 = -v1.Y
                                });
                            }
                            else // Для треугольников замыкаем грань
                            {
                                linesToAdd.Add(new Line
                                {
                                    X1 = v3.X,
                                    Y1 = -v3.Y,
                                    X2 = v1.X,
                                    Y2 = -v1.Y
                                });
                            }

                            list.AddRange(linesToAdd);
                        }
                    }
                }
            });
        }
    }
}
