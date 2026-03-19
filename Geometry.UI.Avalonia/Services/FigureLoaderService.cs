using DynamicData;
using Geometry.UI.Avalonia.Converters;
using Geometry.UI.Avalonia.Models;
using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Geometry.UI.Avalonia.Services;

public class FigureLoaderService
{
    private readonly SourceCache<FigureModel, int> _figures = new(x => x.Id);

    public IObservable<IChangeSet<FigureModel, int>> Figures => _figures.Connect();

    public FigureLoaderService(IEnumerable<string> filenames)
    {
        foreach (var filename in filenames)
        {
            _figures.AddOrUpdate(Load(filename));
        }
    }

    private static FigureModel Load(string filename)
    {
        var file = File.ReadAllText(filename);
        var deserializer = new DeserializerBuilder()
            .WithTypeConverter(new Vector4Converter())
            .WithTypeConverter(new ColorConverter())
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        return deserializer.Deserialize<FigureModel>(file);
    }
}
