using DynamicData;
using Geometry.Base;
using Geometry.UI.Models;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Geometry.UI.Services
{
    public class FigureService
    {
        private readonly SourceList<Outline> _lines;
        public IObservable<IChangeSet<Outline>> Lines => _lines.Connect();

        private Figure _figureData;
        private List<Transform> _transforms;

        public FigureService()
        {
            _lines = new SourceList<Outline>();
            _transforms = new List<Transform>();
        }

        public void UpdateFigureData(Figure figureData)
        {
            _figureData = figureData;
            _transforms.Clear();
            RenderFigure();
        }

        public void AddScaleMatrix(float scale)
        {
            if (scale != 0)
            {
                var m = AffineTransformation.GetScaleMatrix(scale);

                if (scale > 1)
                    _transforms.Add(new Transform { Matrix4X4 = m, TransformType = TransformType.ScaleUp });

                if (scale > 0 && scale < 1)
                    _transforms.Add(new Transform { Matrix4X4 = m, TransformType = TransformType.ScaleDown });

                RenderFigure(m);
            }
        }

        public void AddRotationMatrix(float angle, Vector4 axis)
        {
            if (angle != 0)
            {
                var m = AffineTransformation.GetRotationMatrix(angle, axis);
                _transforms.Add(new Transform { Matrix4X4 = m, TransformType = TransformType.Rotation });

                RenderFigure(m);
            }
        }

        public void AddTranslationMatrix(Vector3 vector)
        {
            if (vector != Vector3.Zero)
            {
                var m = AffineTransformation.GetTranslationMatrix(vector);
                _transforms.Add(new Transform { Matrix4X4 = m, TransformType = TransformType.Translation });

                RenderFigure(m);
            }
        }

        public void AddShearMatrix(float yx = 0, float xy = 0)
        {
            if (yx != 0 || xy != 0)
            {
                var m = AffineTransformation.GetShearMatrix(yx: yx, xy: xy);
                _transforms.Add(new Transform { Matrix4X4 = m, TransformType = TransformType.Shear });

                RenderFigure(m);
            }
        }

        public void Projection(float p)
        {
            if (p != 0)
            {
                var m = PerspectiveTransformation.ToX(1/p);

                _figureData.TransformAllVertices(m);
                _figureData.Normalize();

                RenderFigure();
            }
        }

        private void Build()
        {
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
                    Y1 = startVertexVector.Y,
                    X2 = endVertexVector.X,
                    Y2 = endVertexVector.Y
                });
            }
        }

        private void Clear() =>
            _lines.Clear();

        private void RenderFigure()
        {
            Clear();
            Build();
        }

        private void RenderFigure(Matrix4x4 m)
        {
            if (_figureData is not null)
            {
                _figureData.TransformAllVertices(m);
                RenderFigure();
            }
        }
    }
}
