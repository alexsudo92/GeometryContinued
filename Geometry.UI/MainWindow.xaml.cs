using Geometry.Base;
using Geometry.Base.Transformation;
using Geometry.UI.Models;
using Geometry.UI.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Geometry.UI;

public partial class MainWindow : Window
{
    private readonly FigureLoaderService _loaderService;
    private readonly List<SceneFigure> _loadedFigures = [];
    private readonly Model3DGroup _scene = new();

    public MainWindow()
    {
        InitializeComponent();

        _loaderService = new FigureLoaderService([
            "cube.yaml",
            "4pyramid.yaml",
            "bsp_house.yaml"
        ]);

        AvailableFiguresList.ItemsSource = _loaderService.GetFigures();
        AvailableFiguresList.SelectedIndex = 0;
        SceneViewport.Children.Add(new ModelVisual3D { Content = _scene });

        BuildScene();
        UpdateCamera();
    }

    private void LoadFigureButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (AvailableFiguresList.SelectedItem is not FigureDefinition definition)
            return;

        var instance = new SceneFigure(definition, _loadedFigures.Count + 1);
        _loadedFigures.Add(instance);
        ReloadLoadedList(instance);
        BuildScene();
    }

    private void RemoveFigureButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (LoadedFiguresList.SelectedItem is not SceneFigure selected)
            return;

        _loadedFigures.Remove(selected);
        ReloadLoadedList(_loadedFigures.LastOrDefault());
        BuildScene();
    }

    private void ReloadLoadedList(SceneFigure? selected)
    {
        LoadedFiguresList.ItemsSource = null;
        LoadedFiguresList.ItemsSource = _loadedFigures;
        LoadedFiguresList.SelectedItem = selected;
    }

    private void LoadedFiguresList_OnSelectionChanged(object sender, SelectionChangedEventArgs e) => BuildScene();

    private void ApplyTransformButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (LoadedFiguresList.SelectedItem is not SceneFigure selected)
            return;

        var matrix = Matrix4x4.Identity;
        matrix *= Affine.GetScaleMatrix(ReadFloat(ScaleXBox, 1), ReadFloat(ScaleYBox, 1), ReadFloat(ScaleZBox, 1));

        var axis = RotationAxisBox.Text switch
        {
            "X" => Vector4.UnitX,
            "Y" => Vector4.UnitY,
            _ => Vector4.UnitZ
        };
        matrix *= Affine.GetRotationMatrix(ReadFloat(RotationAngleBox), axis);
        matrix *= Affine.GetTranslationMatrix(new Vector3(ReadFloat(TranslateXBox), ReadFloat(TranslateYBox), ReadFloat(TranslateZBox)));
        matrix *= Affine.GetShearMatrix(
            ReadFloat(ShearXYBox), ReadFloat(ShearXZBox), ReadFloat(ShearYXBox),
            ReadFloat(ShearYZBox), ReadFloat(ShearZXBox), ReadFloat(ShearZYBox));
        matrix *= Perspective.ToX(ReadFloat(PerspectiveXBox));
        matrix *= Perspective.ToY(ReadFloat(PerspectiveYBox));
        matrix *= Perspective.ToZ(ReadFloat(PerspectiveZBox));

        selected.Figure.TransformAllVertices(matrix);
        BuildScene();
    }

    private void ResetTransformButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (LoadedFiguresList.SelectedItem is not SceneFigure selected)
            return;

        selected.Reset();
        BuildScene();
    }

    private static float ReadFloat(TextBox box, float fallback = 0f) =>
        float.TryParse(box.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : fallback;

    private void CameraSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (IsInitialized)
            UpdateCamera();
    }

    private void BuildScene()
    {
        _scene.Children.Clear();
        _scene.Children.Add(new AmbientLight(Color.FromRgb(255, 255, 255)));

        foreach (var figure in _loadedFigures)
            _scene.Children.Add(CreateFigureModel(figure));
    }

    private static Model3DGroup CreateFigureModel(SceneFigure sceneFigure)
    {
        var group = new Model3DGroup();
        foreach (var triangle in sceneFigure.Figure.Triangles)
        {
            var mesh = new MeshGeometry3D();
            foreach (var vertexIndex in triangle.Vertices)
            {
                var vertex = sceneFigure.Figure.Vertices[vertexIndex];
                mesh.Positions.Add(new Point3D(vertex.X, vertex.Y, vertex.Z));
            }

            mesh.TriangleIndices = new Int32Collection([0, 1, 2]);
            var color = Color.FromArgb(triangle.Color.A, triangle.Color.R, triangle.Color.G, triangle.Color.B);
            var material = new DiffuseMaterial(new SolidColorBrush(color));
            group.Children.Add(new GeometryModel3D { Geometry = mesh, Material = material, BackMaterial = material });
        }

        return group;
    }

    private void UpdateCamera()
    {
        var yaw = Math.PI * YawSlider.Value / 180.0;
        var pitch = Math.PI * PitchSlider.Value / 180.0;
        var distance = DistanceSlider.Value;

        var x = distance * Math.Cos(pitch) * Math.Cos(yaw);
        var y = distance * Math.Sin(pitch);
        var z = distance * Math.Cos(pitch) * Math.Sin(yaw);

        MainCamera.Position = new Point3D(x, y, z);
        MainCamera.LookDirection = new Vector3D(-x, -y, -z);
        MainCamera.NearPlaneDistance = 1;
        MainCamera.FarPlaneDistance = 5000;
    }

    private sealed class SceneFigure
    {
        private readonly Figure _original;
        public Figure Figure { get; private set; }
        public string DisplayName { get; }

        public SceneFigure(FigureDefinition definition, int instanceIndex)
        {
            Figure = BuildFigure(definition);
            _original = Clone(Figure);
            DisplayName = $"{definition.Name} #{instanceIndex}";
        }

        public void Reset() => Figure = Clone(_original);

        private static Figure BuildFigure(FigureDefinition definition)
        {
            var figure = new Figure { Vertices = definition.Vertices.ToList(), Triangles = definition.Triangles.ToList() };
            if (definition.BspTree is not null)
            {
                var triangles = new List<Triangle>();
                CollectBspTriangles(definition.BspTree, triangles);
                figure.Triangles = triangles;
            }

            return figure;
        }

        private static void CollectBspTriangles(BspNode node, List<Triangle> output)
        {
            if (node.Polygon is { Count: 3 })
            {
                var c = node.Color ?? Colors.LightGray;
                output.Add(new Triangle
                {
                    Vertices = node.Polygon.ToList(),
                    Color = new FaceColor { R = c.R, G = c.G, B = c.B, A = c.A }
                });
            }

            if (node.Front is not null) CollectBspTriangles(node.Front, output);
            if (node.Back is not null) CollectBspTriangles(node.Back, output);
        }

        private static Figure Clone(Figure source) => new()
        {
            Vertices = source.Vertices.Select(v => new Vector4(v.X, v.Y, v.Z, v.W)).ToList(),
            Triangles = source.Triangles.Select(t => new Triangle
            {
                Vertices = t.Vertices.ToList(),
                Color = new FaceColor { A = t.Color.A, R = t.Color.R, G = t.Color.G, B = t.Color.B }
            }).ToList()
        };
    }
}
