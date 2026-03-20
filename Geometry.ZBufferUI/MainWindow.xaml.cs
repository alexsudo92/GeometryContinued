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

        UpdateRenderModeDescription();
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

    private void UseZBufferCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        if (!IsInitialized)
        {
            return;
        }

        UpdateRenderModeDescription();
        BuildScene();
    }

    private void BuildScene()
    {
        _scene.Children.Clear();
        _scene.Children.Add(new AmbientLight(Color.FromRgb(80, 80, 80)));
        _scene.Children.Add(new DirectionalLight(Colors.White, new Vector3D(-1, -1, -2)));

        var useZBuffer = UseZBufferCheckBox.IsChecked != false;
        var cameraDirection = GetCameraDirection();
        var triangleIndex = 0;

        for (var index = 0; index < _loadedFigures.Count; index++)
        {
            var figure = _loadedFigures[index];
            var offset = new Vector3D((index - (_loadedFigures.Count - 1) / 2.0) * 240.0, 0, 0);
            var isSelected = ReferenceEquals(LoadedFiguresList.SelectedItem, figure);
            _scene.Children.Add(CreateFigureModel(figure, offset, isSelected, useZBuffer, cameraDirection, ref triangleIndex));
        }
    }

    private static Model3DGroup CreateFigureModel(
        FigureDefinition figure,
        Vector3D offset,
        bool isSelected,
        bool useZBuffer,
        Vector3D cameraDirection,
        ref int triangleIndex)
    {
        var group = new Model3DGroup();

        foreach (var triangle in figure.Triangles)
        {
            var mesh = new MeshGeometry3D();
            var depthBias = useZBuffer ? new Vector3D() : cameraDirection * triangleIndex * 3.0;

            foreach (var vertexIndex in triangle.Vertices)
            {
                var vertex = figure.Vertices[vertexIndex];
                mesh.Positions.Add(new Point3D(
                    vertex.X + offset.X + depthBias.X,
                    vertex.Y + offset.Y + depthBias.Y,
                    vertex.Z + offset.Z + depthBias.Z));
            }

            mesh.TriangleIndices = new Int32Collection([0, 1, 2]);
            mesh.Freeze();

            var color = Color.FromArgb(triangle.Color.A, triangle.Color.R, triangle.Color.G, triangle.Color.B);
            var materialColor = isSelected ? Lighten(color) : color;
            var material = new DiffuseMaterial(new SolidColorBrush(materialColor));
            material.Freeze();

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

        if (UseZBufferCheckBox.IsChecked == false)
        {
            BuildScene();
        }
    }

    private Vector3D GetCameraDirection()
    {
        var direction = MainCamera.LookDirection;
        if (direction.LengthSquared < double.Epsilon)
        {
            return new Vector3D(0, 0, -1);
        }

        direction.Normalize();
        return direction;
    }

    private void UpdateRenderModeDescription()
    {
        var useZBuffer = UseZBufferCheckBox.IsChecked != false;
        RenderModeDescriptionTextBlock.Text = useZBuffer
            ? "При включённом Z-буфере видимость треугольников определяется их реальной глубиной."
            : "Z-буфер выключен: треугольники принудительно смещаются к камере по порядку отрисовки, поэтому ближние грани уже не всегда перекрывают дальние корректно.";
    }

    private static Color Lighten(Color color)
    {
        byte Blend(byte value) => (byte)Math.Min(255, value + 45);
        return Color.FromArgb(color.A, Blend(color.R), Blend(color.G), Blend(color.B));
    }
}
