using Geometry.UI.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Windows.Media;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Geometry.UI.Services;

public class FigureLoaderService
{
    private readonly IReadOnlyList<FigureDefinition> _figures;

    public FigureLoaderService(IEnumerable<string> filenames)
    {
        _figures = filenames.Select(Load).OrderBy(x => x.Id).ToList();
    }

    public IReadOnlyList<FigureDefinition> GetFigures() => _figures;

    private static FigureDefinition Load(string filename)
    {
        var yaml = File.ReadAllText(filename);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .WithTypeConverter(new Vector4YamlConverter())
            .WithTypeConverter(new ColorYamlConverter())
            .Build();

        return deserializer.Deserialize<FigureDefinition>(yaml);
    }

    private sealed class Vector4YamlConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type) => type == typeof(Vector4);

        public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
        {
            parser.Consume<SequenceStart>();
            var x = float.Parse(parser.Consume<Scalar>().Value);
            var y = float.Parse(parser.Consume<Scalar>().Value);
            var z = float.Parse(parser.Consume<Scalar>().Value);
            parser.Consume<SequenceEnd>();
            return new Vector4(x, y, z, 1f);
        }

        public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer) =>
            throw new NotImplementedException();
    }

    private sealed class ColorYamlConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type) => type == typeof(Color);

        public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
        {
            parser.Consume<MappingStart>();
            byte r = 0, g = 0, b = 0, a = 255;

            while (!parser.Accept<MappingEnd>(out _))
            {
                var key = parser.Consume<Scalar>().Value;
                var value = byte.Parse(parser.Consume<Scalar>().Value);
                switch (key.ToLowerInvariant())
                {
                    case "r": r = value; break;
                    case "g": g = value; break;
                    case "b": b = value; break;
                    case "a": a = value; break;
                }
            }

            parser.Consume<MappingEnd>();
            return Color.FromArgb(a, r, g, b);
        }

        public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer) =>
            throw new NotImplementedException();
    }
}
