using DynamicData;
using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Geometry.UI.Services
{
    public class YamlService
    {
        private readonly SourceCache<FigureYaml, int> _figures;
        public IObservable<IChangeSet<FigureYaml, int>> Figures => _figures.Connect();

        public YamlService(IEnumerable<string> filenames)
        {
            _figures = new SourceCache<FigureYaml, int>(x => x.Id);

            foreach (var filename in filenames)
            {
                _figures.AddOrUpdate(Load(filename));
            }
        }

        private FigureYaml Load(string filename)
        {
            var file = File.ReadAllText(filename);

            var deserializer = new DeserializerBuilder()
                                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                                    .Build();

            return deserializer.Deserialize<FigureYaml>(file);
        }
    }
}
