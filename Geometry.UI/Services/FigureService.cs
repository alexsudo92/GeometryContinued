using DynamicData;
using Geometry.Base;
using Geometry.Base.Transformation;
using Geometry.UI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Geometry.UI.Services
{
    public class FigureService
    {
        private readonly SourceList<Outline> _lines;
        public IObservable<IChangeSet<Outline>> Lines => _lines.Connect();

        private Figure _figureData;
        private List<Transform> _history;

        public FigureService()
        {
            _lines = new SourceList<Outline>();
            _history = new List<Transform>();
        }

        public void SetFigureData(Figure figureData)
        {
            if (figureData != null)
            {
                _figureData = figureData;
                _history.Clear();
                RenderFigure();
            }
        }

        public void ScaleFigure(float scale, bool shouldRender = true)
        {
            if (scale != 0 && scale != 1f)
            {
                var m = Affine.GetScaleMatrix(scale);

                if (scale > 1f)
                    AddToHistrory(m, TransformType.ScaleUp);
                if (scale > 0f && scale < 1f)
                    AddToHistrory(m, TransformType.ScaleDown);

                _figureData.TransformAllVertices(m);

                if (shouldRender)
                    RenderFigure();
            }
        }

        public void RotateFigure(float angle, Vector4 axis, bool shouldRender = true)
        {
            if (angle != 0)
            {
                var m = Affine.GetRotationMatrix(angle, axis);
                AddToHistrory(m, TransformType.Rotation);
                _figureData.TransformAllVertices(m);

                if (shouldRender)
                    RenderFigure();
            }
        }

        public void TranslateFigure(Vector3 vector, bool shouldRender = true)
        {
            if (vector != Vector3.Zero)
            {
                var m = Affine.GetTranslationMatrix(vector);
                AddToHistrory(m, TransformType.Translation);
                _figureData.TransformAllVertices(m);

                if (shouldRender)
                    RenderFigure();
            }
        }

        public void ShearFigure(float yx = 0, float xy = 0, bool shouldRender = true)
        {
            if (yx != 0 || xy != 0)
            {
                var m = Affine.GetShearMatrix(yx: yx, xy: xy);
                AddToHistrory(m, TransformType.Shear);
                _figureData.TransformAllVertices(m);

                if (shouldRender)
                    RenderFigure();
            }
        }

        public void ProjectFigureToX(float p, bool shouldRender = true)
        {
            if (p != 0)
            {
                ReverseProjectFigureToX(false);

                var m = Perspective.ToX(1 / p);
                AddToHistrory(m, TransformType.Perspective);
                _figureData.TransformAllVertices(m);

                if (shouldRender)
                    RenderFigure();
            }
        }

        public void ReverseProjectFigureToX(bool shouldRender = true)
        {
            var last = _history
                .FirstOrDefault(x => x.TransformType == TransformType.Perspective);

            if (last != null)
            {
                var iM = last.Invert();
                _figureData.TransformAllVertices(iM);
                _history.Remove(last);

                if (shouldRender)
                    RenderFigure();
            }
        }

        public void MoveToAndScale(float areaX1, float areaY1, float areaX2, float areaY2, bool shouldRender = true)
        {
            if ((areaX1 != 0 && areaY1 != 0) || (areaX2 != 0 && areaY2 != 0))
            {
                var (MinX, MinY, MaxX, MaxY) = _figureData.GetSizeRectangle();

                var areaWidth = areaX2 - areaX1;
                var areaHeight = areaY2 - areaY1;

                var fWidth = MaxX - MinX;
                var fHeight = MaxY - MinY;

                var k = MathF.Min(areaWidth / fWidth, areaHeight / fHeight);
                ScaleFigure(k, false);


                (MinX, MinY, MaxX, MaxY) = _figureData.GetSizeRectangle();

                var midX = (MinX + MaxX) / 2;
                var midY = (MinY + MaxY) / 2;

                var tX = (areaX2 + areaX1) / 2 - midX;
                var tY = (areaY2 + areaY1) / 2 - midY;

                var vector = new Vector3(tX, tY, 0);
                TranslateFigure(vector, shouldRender);
            }
        }

        private void AddToHistrory(Matrix4x4 m, TransformType transformType)
        {
            _history.Add(new Transform { TransformType = transformType, Matrix4X4 = m });
        }

        private void RenderFigure()
        {
            _lines.Clear();

            foreach (var e in _figureData.Edges)
            {
                var startEdge = e[0];
                var endEdge = e[1];

                var startVertex = _figureData.Vertices[startEdge];
                var endVertex = _figureData.Vertices[endEdge];

                var startVertexVector = startVertex.Vector4;
                var endVertexVector = endVertex.Vector4;

                _lines.Add(new Outline
                {
                    X1 = startVertexVector.X,
                    Y1 = -startVertexVector.Y,
                    X2 = endVertexVector.X,
                    Y2 = -endVertexVector.Y
                });
            }
        }
    }
}
