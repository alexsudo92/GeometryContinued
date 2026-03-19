using Avalonia.Media;
using System;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Geometry.UI.Avalonia.Converters;

public class ColorConverter : IYamlTypeConverter
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

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
        => throw new NotImplementedException();
}
