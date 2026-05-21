using Geometry.ZBufferUI.Models;
using Geometry.ZBufferUI.Services;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Geometry.ZBufferUI;

public partial class MainWindow : Window
{
    private readonly FigureLoaderService _loaderService;
    private readonly List<FigureDefinition> _loadedFigures = [];
    private readonly Model3DGroup _scene = new();

    public MainWindow()
    {
        InitializeComponent();

        _loaderService = new FigureLoaderService([
            "cube.yaml",
            "4pyramid.yaml"
        ]);

        AvailableFiguresList.ItemsSource = _loaderService.GetFigures();
        AvailableFiguresList.SelectedIndex = 0;
        SceneViewport.Children.Add(new ModelVisual3D { Content = _scene });

        BuildScene();
        UpdateCamera();
    }

    private void LoadFigureButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (AvailableFiguresList.SelectedItem is not FigureDefinition figure)
        {
            return;
        }

        _loadedFigures.Add(figure);
        LoadedFiguresList.ItemsSource = null;
        LoadedFiguresList.ItemsSource = _loadedFigures;
        LoadedFiguresList.SelectedItem = figure;
        BuildScene();
    }

    private void LoadedFiguresList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        BuildScene();
    }

    private void CameraSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsInitialized)
        {
            return;
        }

        UpdateCamera();
    }

    private void BuildScene()
    {
        _scene.Children.Clear();
        _scene.Children.Add(new AmbientLight(Color.FromRgb(255, 255, 255)));

        var triangleIndex = 0;

        for (var index = 0; index < _loadedFigures.Count; index++)
        {
            var figure = _loadedFigures[index];
            _scene.Children.Add(CreateFigureModel(figure, ref triangleIndex));
        }
    }

    private static Model3DGroup CreateFigureModel(
        FigureDefinition figure,
        ref int triangleIndex)
    {
        var group = new Model3DGroup();

        foreach (var triangle in figure.Triangles)
        {
            var mesh = new MeshGeometry3D();

            foreach (var vertexIndex in triangle.Vertices)
            {
                var vertex = figure.Vertices[vertexIndex];
                mesh.Positions.Add(new Point3D(
                    vertex.X,
                    vertex.Y,
                    vertex.Z));
            }

            mesh.TriangleIndices = new Int32Collection([0, 1, 2]);
            mesh.Freeze();

            var color = Color.FromArgb(triangle.Color.A, triangle.Color.R, triangle.Color.G, triangle.Color.B);
            var material = new DiffuseMaterial(new SolidColorBrush(color));

            group.Children.Add(new GeometryModel3D
            {
                Geometry = mesh,
                Material = material,
                BackMaterial = material
            });

            triangleIndex++;
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
}
