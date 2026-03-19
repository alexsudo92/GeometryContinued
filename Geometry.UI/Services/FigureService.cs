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
        private readonly SourceList<PolygonModel> _polygons;
        public IObservable<IChangeSet<PolygonModel>> Polygons => _polygons.Connect();

        private readonly SourceList<FigureModel> _figures;
        public IObservable<IChangeSet<FigureModel>> Figures => _figures.Connect();

        public FigureService()
        {
            _figures = new SourceList<FigureModel>();
            _polygons = new SourceList<PolygonModel>();
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
            var triangles = new List<ScreenTriangle>();

            foreach (var figure in _figures.Items)
            {
                foreach (var triangle in figure.Triangles)
                {
                    if (triangle.Vertices.Count != 3)
                    {
                        continue;
                    }

                    var faceColor = Color.FromArgb(triangle.Color.A, triangle.Color.R, triangle.Color.G, triangle.Color.B);
                    var screenVertices = triangle.Vertices
                        .Select(index => figure.Vertices[index])
                        .Select(vertex => new Vector3(vertex.X, -vertex.Y, vertex.Z))
                        .ToArray();

                    triangles.Add(new ScreenTriangle(screenVertices[0], screenVertices[1], screenVertices[2], new PolygonModel
                    {
                        Points = [
                            new Point(screenVertices[0].X, screenVertices[0].Y),
                            new Point(screenVertices[1].X, screenVertices[1].Y),
                            new Point(screenVertices[2].X, screenVertices[2].Y)
                        ],
                        FillColor = faceColor,
                        Depth = float.NegativeInfinity
                    }));
                }
            }

            var polygons = ApplyZBuffer(triangles);

            _polygons.Edit(list =>
            {
                list.Clear();
                list.AddRange(polygons);
            });
        }

        private static List<PolygonModel> ApplyZBuffer(List<ScreenTriangle> triangles)
        {
            if (triangles.Count == 0)
            {
                return [];
            }

            var bounds = GetSceneBounds(triangles);
            if (!bounds.HasArea)
            {
                return [.. triangles.Select(triangle => triangle.Polygon)];
            }

            var depthBuffer = CreateDepthBuffer(bounds.Width, bounds.Height);
            var ownerBuffer = CreateOwnerBuffer(bounds.Width, bounds.Height);

            for (var triangleIndex = 0; triangleIndex < triangles.Count; triangleIndex++)
            {
                RasterizeTriangle(triangles[triangleIndex], triangleIndex, bounds, depthBuffer, ownerBuffer);
            }

            return [.. CollectVisiblePolygons(triangles, depthBuffer, ownerBuffer).OrderBy(polygon => polygon.Depth)];
        }

        private static SceneBounds GetSceneBounds(List<ScreenTriangle> triangles)
        {
            var minX = int.MaxValue;
            var minY = int.MaxValue;
            var maxX = int.MinValue;
            var maxY = int.MinValue;

            foreach (var triangle in triangles)
            {
                foreach (var vertex in triangle.Vertices)
                {
                    minX = Math.Min(minX, (int)MathF.Floor(vertex.X));
                    minY = Math.Min(minY, (int)MathF.Floor(vertex.Y));
                    maxX = Math.Max(maxX, (int)MathF.Ceiling(vertex.X));
                    maxY = Math.Max(maxY, (int)MathF.Ceiling(vertex.Y));
                }
            }

            return new SceneBounds(minX, minY, maxX, maxY);
        }

        private static float[,] CreateDepthBuffer(int width, int height)
        {
            var depthBuffer = new float[width, height];

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    depthBuffer[x, y] = float.NegativeInfinity;
                }
            }

            return depthBuffer;
        }

        private static int[,] CreateOwnerBuffer(int width, int height)
        {
            var ownerBuffer = new int[width, height];

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    ownerBuffer[x, y] = -1;
                }
            }

            return ownerBuffer;
        }

        private static IEnumerable<PolygonModel> CollectVisiblePolygons(List<ScreenTriangle> triangles, float[,] depthBuffer, int[,] ownerBuffer)
        {
            var visibleTriangles = new HashSet<int>();

            for (var x = 0; x < depthBuffer.GetLength(0); x++)
            {
                for (var y = 0; y < depthBuffer.GetLength(1); y++)
                {
                    var owner = ownerBuffer[x, y];
                    if (owner < 0)
                    {
                        continue;
                    }

                    visibleTriangles.Add(owner);
                    triangles[owner].Polygon.Depth = MathF.Max(triangles[owner].Polygon.Depth, depthBuffer[x, y]);
                }
            }

            return visibleTriangles.Select(index => triangles[index].Polygon);
        }

        private static void RasterizeTriangle(ScreenTriangle triangle, int triangleIndex, SceneBounds bounds, float[,] depthBuffer, int[,] ownerBuffer)
        {
            var (v1, v2, v3) = triangle;
            var minX = Math.Max((int)MathF.Floor(MathF.Min(v1.X, MathF.Min(v2.X, v3.X))), bounds.MinX);
            var maxX = Math.Min((int)MathF.Ceiling(MathF.Max(v1.X, MathF.Max(v2.X, v3.X))), bounds.MaxX);
            var minY = Math.Max((int)MathF.Floor(MathF.Min(v1.Y, MathF.Min(v2.Y, v3.Y))), bounds.MinY);
            var maxY = Math.Min((int)MathF.Ceiling(MathF.Max(v1.Y, MathF.Max(v2.Y, v3.Y))), bounds.MaxY);
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
                    var bufferX = x - bounds.MinX;
                    var bufferY = y - bounds.MinY;

                    if (depth <= depthBuffer[bufferX, bufferY])
                    {
                        continue;
                    }

                    depthBuffer[bufferX, bufferY] = depth;
                    ownerBuffer[bufferX, bufferY] = triangleIndex;
                }
            }
        }

        private static float EdgeFunction(float ax, float ay, float bx, float by, float px, float py) =>
            (px - ax) * (by - ay) - (py - ay) * (bx - ax);

        private readonly record struct ScreenTriangle(Vector3 A, Vector3 B, Vector3 C, PolygonModel Polygon)
        {
            public Vector3[] Vertices => [A, B, C];

            public void Deconstruct(out Vector3 a, out Vector3 b, out Vector3 c)
            {
                a = A;
                b = B;
                c = C;
            }
        }

        private readonly record struct SceneBounds(int MinX, int MinY, int MaxX, int MaxY)
        {
            public bool HasArea => MaxX >= MinX && MaxY >= MinY;
            public int Width => MaxX - MinX + 1;
            public int Height => MaxY - MinY + 1;
        }
    }
}
