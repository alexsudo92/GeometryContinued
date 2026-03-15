using DynamicData;
using Geometry.UI.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Geometry.UI.Services
{
    public class FigureLoaderService
    {
        private readonly SourceCache<FigureModel, int> _figures;
        public IObservable<IChangeSet<FigureModel, int>> Figures => _figures.Connect();

        public FigureLoaderService(IEnumerable<string> filenames)
        {
            _figures = new SourceCache<FigureModel, int>(x => x.Id);

            foreach (var filename in filenames)
            {
                _figures.AddOrUpdate(Load(filename));
            }
        }

        private FigureModel Load(string filename)
        {
            var file = File.ReadAllText(filename);

            var deserializer = new DeserializerBuilder()
                                    .WithTypeConverter(new Vector4Converter())
                                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                                    .Build();

            return deserializer.Deserialize<FigureModel>(file);
        }
    }

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

        public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
