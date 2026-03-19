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
        private readonly SourceList<FacePolygon> _polygons;
        public IObservable<IChangeSet<FacePolygon>> Polygons => _polygons.Connect();

        private readonly SourceList<FigureModel> _figures;
        public IObservable<IChangeSet<FigureModel>> Figures => _figures.Connect();

        public FigureService()
        {
            _figures = new SourceList<FigureModel>();
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

        public async Task SmoothTranslateFigure(int id, Vector3 direction, CancellationToken cancellationToken)
        {
            if (_figures.Items[id] is not null)
            {
                var figure = _figures.Items[id];

                var distance = direction.Length();
                var stepVector = Vector3.Normalize(direction);
                var stepVectorLength = stepVector.Length();

                for (var i = 0f; i < distance; i += stepVectorLength)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    var translationMatrix = Affine.GetTranslationMatrix(stepVector);
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

        private void RenderAllFigures()
        {
            var cameraDirection = new Vector4(0f, 0f, -1f, 1f);
            var polygonCandidates = new List<(FacePolygon Polygon, List<Vector3> Vertices)>();

            foreach (var fw in _figures.Items)
            {
                foreach (var face in fw.Faces)
                {
                    var faceColor = Color.FromArgb(face.Color.A, face.Color.R, face.Color.G, face.Color.B);
                    var faceVertices = face.Vertices;

                    var points = new PointCollection();
                    var projectedVertices = new List<Vector3>(faceVertices.Count);

                    for (var i = 0; i < faceVertices.Count; i++)
                    {
                        var vertex = fw.Vertices[faceVertices[i]];
                        points.Add(new Point(vertex.X, -vertex.Y));
                        projectedVertices.Add(new Vector3(vertex.X, -vertex.Y, vertex.Z));
                    }

                    polygonCandidates.Add((
                        new FacePolygon
                        {
                            Points = points,
                            FillColor = faceColor,
                            Depth = float.NegativeInfinity
                        },
                        projectedVertices));
                }
            }

            var polygons = ApplyZBuffer(polygonCandidates);

            _polygons.Edit(list =>
            {
                list.Clear();
                list.AddRange(polygons);
            });
        }

        private static List<FacePolygon> ApplyZBuffer(List<(FacePolygon Polygon, List<Vector3> Vertices)> candidates)
        {
            if (candidates.Count == 0)
            {
                return [];
            }

            var (minX, minY, maxX, maxY) = GetSceneBounds(candidates);
            if (maxX < minX || maxY < minY)
            {
                return [.. candidates.Select(candidate => candidate.Polygon)];
            }

            var width = maxX - minX + 1;
            var height = maxY - minY + 1;
            var depthBuffer = new float[width, height];
            var ownerBuffer = new int[width, height];

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    depthBuffer[x, y] = float.NegativeInfinity;
                    ownerBuffer[x, y] = -1;
                }
            }

            for (var polygonIndex = 0; polygonIndex < candidates.Count; polygonIndex++)
            {
                var vertices = candidates[polygonIndex].Vertices;

                for (var vertexIndex = 1; vertexIndex < vertices.Count - 1; vertexIndex++)
                {
                    RasterizeTriangle(vertices[0], vertices[vertexIndex], vertices[vertexIndex + 1], polygonIndex, minX, minY, depthBuffer, ownerBuffer);
                }
            }

            var visiblePolygons = new HashSet<int>();
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var owner = ownerBuffer[x, y];
                    if (owner < 0)
                    {
                        continue;
                    }

                    visiblePolygons.Add(owner);
                    candidates[owner].Polygon.Depth = MathF.Max(candidates[owner].Polygon.Depth, depthBuffer[x, y]);
                }
            }

            return [.. visiblePolygons
                .Select(index => candidates[index].Polygon)
                .OrderBy(polygon => polygon.Depth)];
        }

        private static (int minX, int minY, int maxX, int maxY) GetSceneBounds(List<(FacePolygon Polygon, List<Vector3> Vertices)> candidates)
        {
            var minX = int.MaxValue;
            var minY = int.MaxValue;
            var maxX = int.MinValue;
            var maxY = int.MinValue;

            foreach (var (Polygon, Vertices) in candidates)
            {
                foreach (var vertex in Vertices)
                {
                    minX = Math.Min(minX, (int)MathF.Floor(vertex.X));
                    minY = Math.Min(minY, (int)MathF.Floor(vertex.Y));
                    maxX = Math.Max(maxX, (int)MathF.Ceiling(vertex.X));
                    maxY = Math.Max(maxY, (int)MathF.Ceiling(vertex.Y));
                }
            }

            return (minX, minY, maxX, maxY);
        }

        private static void RasterizeTriangle(Vector3 v1, Vector3 v2, Vector3 v3, int polygonIndex, int offsetX, int offsetY, float[,] depthBuffer, int[,] ownerBuffer)
        {
            var minX = Math.Max((int)MathF.Floor(MathF.Min(v1.X, MathF.Min(v2.X, v3.X))), offsetX);
            var maxX = Math.Min((int)MathF.Ceiling(MathF.Max(v1.X, MathF.Max(v2.X, v3.X))), offsetX + depthBuffer.GetLength(0) - 1);
            var minY = Math.Max((int)MathF.Floor(MathF.Min(v1.Y, MathF.Min(v2.Y, v3.Y))), offsetY);
            var maxY = Math.Min((int)MathF.Ceiling(MathF.Max(v1.Y, MathF.Max(v2.Y, v3.Y))), offsetY + depthBuffer.GetLength(1) - 1);
            var denominator = EdgeFunction(v1.X, v1.Y, v2.X, v2.Y, v3.X, v3.Y);

            if (MathF.Abs(denominator) < float.Epsilon)
            {
                return;
            }

            for (var x = minX; x <= maxX; x++)
            {
                for (var y = minY; y <= maxY; y++)
                {
                    var sampleX = x + 0.5f;
                    var sampleY = y + 0.5f;

                    var w0 = EdgeFunction(v2.X, v2.Y, v3.X, v3.Y, sampleX, sampleY) / denominator;
                    var w1 = EdgeFunction(v3.X, v3.Y, v1.X, v1.Y, sampleX, sampleY) / denominator;
                    var w2 = EdgeFunction(v1.X, v1.Y, v2.X, v2.Y, sampleX, sampleY) / denominator;

                    if (w0 < 0f || w1 < 0f || w2 < 0f)
                    {
                        continue;
                    }

                    var depth = v1.Z * w0 + v2.Z * w1 + v3.Z * w2;
                    var bufferX = x - offsetX;
                    var bufferY = y - offsetY;

                    if (depth <= depthBuffer[bufferX, bufferY])
                    {
                        continue;
                    }

                    depthBuffer[bufferX, bufferY] = depth;
                    ownerBuffer[bufferX, bufferY] = polygonIndex;
                }
            }
        }

        private static float EdgeFunction(float ax, float ay, float bx, float by, float px, float py) =>
            (px - ax) * (by - ay) - (py - ay) * (bx - ax);

    }
}
