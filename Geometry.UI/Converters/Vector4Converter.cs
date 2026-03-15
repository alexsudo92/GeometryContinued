using System;
using System.Numerics;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Geometry.UI.Converters
{
    public class Vector4Converter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            return type == typeof(Vector4);
        }

        public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
        {
            parser.Consume<SequenceStart>();
            var x = float.Parse(parser.Consume<Scalar>().Value);
            var y = float.Parse(parser.Consume<Scalar>().Value);
            var z = float.Parse(parser.Consume<Scalar>().Value);
            parser.Consume<SequenceEnd>();

            return new Vector4(x, y, z, 1f);
        }

        public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}