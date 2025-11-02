using DynamicData;
using Geometry.Base;
using Geometry.UI.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Geometry.UI.Services
{
    public class FigureService
    {
        private readonly SourceList<Outline> _lines;
        public IObservable<IChangeSet<Outline>> Lines => _lines.Connect();

        private readonly Figure _figureData;

        private List<Transform> _transforms;

        public FigureService(string filename)
        {
            _lines = new SourceList<Outline>();
            _transforms = new List<Transform>();
            _figureData = Load(filename);
        }

        public void AddMatrix(float scale)
        {
            if (scale != 0)
            {
                var m = AffineTransformation.GetScaleMatrix(scale);
                _transforms.Add(new Transform { Matrix4X4 = m, TransformType = TransformType.Scale });

                Update(m);
            }
        }

        public void AddMatrix(float angle, Vector4 axis)
        {
            if (angle != 0)
            {
                var m = AffineTransformation.GetRotationMatrix(angle, axis);
                _transforms.Add(new Transform { Matrix4X4 = m, TransformType = TransformType.Rotation });

                Update(m);
            }
        }

        public void AddMatrix(Vector3 vector)
        {
            if (vector != Vector3.Zero)
            {
                var m = AffineTransformation.GetTranslationMatrix(vector);
                _transforms.Add(new Transform { Matrix4X4 = m, TransformType = TransformType.Translation });

                Update(m);
            }
        }

        private void Update(Matrix4x4 m)
        {
            _figureData.TransformAllVertices(m);
            Clear();
            Build();
        }

        public Figure Load(string filename)
        {
            var file = File.ReadAllText(filename);

            var deserializer = new DeserializerBuilder()
                                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                                    .Build();

            var f = deserializer.Deserialize<FigureYaml>(file);

            return f.AsFigure();
        }

        public void Build()
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

        public void Clear()
        {
            _lines.Clear();
        }
    }
}
