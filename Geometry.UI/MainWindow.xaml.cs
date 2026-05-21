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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Geometry.UI;

public partial class MainWindow : Window
{
    private readonly FigureLoaderService _loaderService;
    private readonly List<LoadedFigureInstance> _loadedFigures = [];
    private readonly Model3DGroup _scene = new();

    private double _cameraYaw = -35;
    private double _cameraPitch = -18;
    private double _cameraDistance = 700;
    private Vector3 _cameraTarget = Vector3.Zero;
    private bool _isRotating;
    private bool _isPanning;
    private Point _lastMousePosition;

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
        if (AvailableFiguresList.SelectedItem is not FigureDefinition figure) return;

        var instance = new LoadedFigureInstance(figure, _loadedFigures.Count + 1);
        _loadedFigures.Add(instance);
        ReloadLoadedFiguresList();
        LoadedFiguresList.SelectedItem = instance;
        BuildScene();
    }

    private void RemoveFigureButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (LoadedFiguresList.SelectedItem is not LoadedFigureInstance instance) return;

        _loadedFigures.Remove(instance);
        ReloadLoadedFiguresList();
        BuildScene();
    }

    private void LoadedFiguresList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LoadedFiguresList.SelectedItem is not LoadedFigureInstance instance)
        {
            return;
        }

        ScaleBox.Text = $"{instance.Scale.X} {instance.Scale.Y} {instance.Scale.Z}";
        RotationBox.Text = $"{instance.RotationAngle} {instance.RotationAxis.X} {instance.RotationAxis.Y} {instance.RotationAxis.Z}";
        TranslationBox.Text = $"{instance.Translation.X} {instance.Translation.Y} {instance.Translation.Z}";
        ShearBox.Text = $"{instance.Shear.XY} {instance.Shear.XZ} {instance.Shear.YX} {instance.Shear.YZ} {instance.Shear.ZX} {instance.Shear.ZY}";
        PerspectiveBox.Text = $"{instance.Perspective.X} {instance.Perspective.Y} {instance.Perspective.Z}";

        BuildScene();
    }

    private void ApplyScaleButton_OnClick(object sender, RoutedEventArgs e) => ApplyToSelectedFigure(instance =>
    {
        instance.Scale = ParseVector3(ScaleBox.Text);
    });

    private void ApplyRotationButton_OnClick(object sender, RoutedEventArgs e) => ApplyToSelectedFigure(instance =>
    {
        var rotation = ParseFloatArray(RotationBox.Text, 4);
        instance.RotationAngle = rotation[0];
        instance.RotationAxis = new Vector4(rotation[1], rotation[2], rotation[3], 0f);
    });

    private void ApplyTranslationButton_OnClick(object sender, RoutedEventArgs e) => ApplyToSelectedFigure(instance =>
    {
        instance.Translation = ParseVector3(TranslationBox.Text);
    });

    private void ApplyShearButton_OnClick(object sender, RoutedEventArgs e) => ApplyToSelectedFigure(instance =>
    {
        var shear = ParseFloatArray(ShearBox.Text, 6);
        instance.Shear = new ShearValues(shear[0], shear[1], shear[2], shear[3], shear[4], shear[5]);
    });

    private void ApplyPerspectiveButton_OnClick(object sender, RoutedEventArgs e) => ApplyToSelectedFigure(instance =>
    {
        instance.Perspective = ParseVector3(PerspectiveBox.Text);
    });

    private void ApplyToSelectedFigure(Action<LoadedFigureInstance> update)
    {
        if (LoadedFiguresList.SelectedItem is not LoadedFigureInstance instance) return;

        try
        {
            update(instance);
            BuildScene();
        }
        catch (FormatException ex)
        {
            MessageBox.Show(ex.Message, "Ошибка формата", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ResetTransformButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (LoadedFiguresList.SelectedItem is not LoadedFigureInstance instance) return;

        instance.ResetTransforms();
        LoadedFiguresList_OnSelectionChanged(sender, new SelectionChangedEventArgs(ListBox.SelectionChangedEvent, [], []));
    }

    private void ViewportBorder_OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        _cameraDistance = Math.Clamp(_cameraDistance - e.Delta * 0.3, 80, 4000);
        UpdateCamera();
    }

    private void ViewportBorder_OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isRotating = true;
        _lastMousePosition = e.GetPosition(SceneViewport);
        Mouse.Capture((IInputElement)sender);
    }

    private void ViewportBorder_OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isRotating = false;
        if (!_isPanning) Mouse.Capture(null);
    }

    private void ViewportBorder_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Middle) return;
        _isPanning = true;
        _lastMousePosition = e.GetPosition(SceneViewport);
        Mouse.Capture((IInputElement)sender);
    }

    private void ViewportBorder_OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Middle) return;
        _isPanning = false;
        if (!_isRotating) Mouse.Capture(null);
    }

    private void ViewportBorder_OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isRotating && !_isPanning) return;

        var current = e.GetPosition(SceneViewport);
        var delta = current - _lastMousePosition;
        _lastMousePosition = current;

        if (_isRotating)
        {
            _cameraYaw += delta.X * 0.35;
            _cameraPitch = Math.Clamp(_cameraPitch - delta.Y * 0.25, -89, 89);
        }

        if (_isPanning)
        {
            var yawRad = _cameraYaw * Math.PI / 180.0;
            var right = new Vector3((float)Math.Sin(yawRad), 0f, (float)-Math.Cos(yawRad));
            var up = Vector3.UnitY;
            var panSpeed = (float)(_cameraDistance * 0.0015);
            _cameraTarget += right * (float)(-delta.X * panSpeed);
            _cameraTarget += up * (float)(delta.Y * panSpeed);
        }

        UpdateCamera();
    }

    private void BuildScene()
    {
        _scene.Children.Clear();
        _scene.Children.Add(new AmbientLight(Color.FromRgb(255, 255, 255)));

        foreach (var instance in _loadedFigures)
        {
            _scene.Children.Add(CreateFigureModel(instance));
        }
    }

    private static Model3DGroup CreateFigureModel(LoadedFigureInstance instance)
    {
        var group = new Model3DGroup();
        var baseFigure = new Figure
        {
            Vertices = instance.Source.Vertices.Select(v => v).ToList(),
            Triangles = BuildTriangles(instance.Source)
        };

        var matrix = Matrix4x4.Identity;
        matrix *= Affine.GetScaleMatrix(instance.Scale.X, instance.Scale.Y, instance.Scale.Z);
        matrix *= Affine.GetRotationMatrix(instance.RotationAngle, instance.RotationAxis);
        matrix *= Affine.GetShearMatrix(instance.Shear.XY, instance.Shear.XZ, instance.Shear.YX, instance.Shear.YZ, instance.Shear.ZX, instance.Shear.ZY);
        matrix *= Affine.GetTranslationMatrix(instance.Translation);
        matrix *= Perspective.ToX(instance.Perspective.X);
        matrix *= Perspective.ToY(instance.Perspective.Y);
        matrix *= Perspective.ToZ(instance.Perspective.Z);

        baseFigure.TransformAllVertices(matrix);

        foreach (var triangle in baseFigure.Triangles)
        {
            var mesh = new MeshGeometry3D();
            foreach (var vertexIndex in triangle.Vertices)
            {
                var vertex = baseFigure.Vertices[vertexIndex];
                mesh.Positions.Add(new Point3D(vertex.X, vertex.Y, vertex.Z));
            }

            mesh.TriangleIndices = new Int32Collection([0, 1, 2]);
            var color = Color.FromArgb(triangle.Color.A, triangle.Color.R, triangle.Color.G, triangle.Color.B);
            var material = new DiffuseMaterial(new SolidColorBrush(color));

            group.Children.Add(new GeometryModel3D
            {
                Geometry = mesh,
                Material = material,
                BackMaterial = material
            });
        }

        return group;
    }

    private static List<Triangle> BuildTriangles(FigureDefinition source)
    {
        var triangles = source.Triangles.Select(t => new Triangle
        {
            Vertices = [.. t.Vertices],
            Color = new FaceColor { A = t.Color.A, R = t.Color.R, G = t.Color.G, B = t.Color.B }
        }).ToList();

        if (source.BspTree is not null)
        {
            CollectBspTriangles(source.BspTree, triangles);
        }

        return triangles;
    }

    private static void CollectBspTriangles(BspNodeDefinition node, List<Triangle> triangles)
    {
        triangles.AddRange(node.Polygons.Select(t => new Triangle
        {
            Vertices = [.. t.Vertices],
            Color = new FaceColor { A = t.Color.A, R = t.Color.R, G = t.Color.G, B = t.Color.B }
        }));

        if (node.Front is not null) CollectBspTriangles(node.Front, triangles);
        if (node.Back is not null) CollectBspTriangles(node.Back, triangles);
    }

    private void UpdateCamera()
    {
        var yaw = Math.PI * _cameraYaw / 180.0;
        var pitch = Math.PI * _cameraPitch / 180.0;

        var x = _cameraDistance * Math.Cos(pitch) * Math.Cos(yaw);
        var y = _cameraDistance * Math.Sin(pitch);
        var z = _cameraDistance * Math.Cos(pitch) * Math.Sin(yaw);

        var position = new Vector3((float)x, (float)y, (float)z) + _cameraTarget;
        var target = _cameraTarget;

        MainCamera.Position = new Point3D(position.X, position.Y, position.Z);
        MainCamera.LookDirection = new Vector3D(target.X - position.X, target.Y - position.Y, target.Z - position.Z);
        MainCamera.NearPlaneDistance = 1;
        MainCamera.FarPlaneDistance = 5000;
    }

    private void ReloadLoadedFiguresList()
    {
        LoadedFiguresList.ItemsSource = null;
        LoadedFiguresList.ItemsSource = _loadedFigures;
    }

    private static Vector3 ParseVector3(string text)
    {
        var parts = ParseFloatArray(text, 3);
        return new Vector3(parts[0], parts[1], parts[2]);
    }

    private static float[] ParseFloatArray(string text, int count)
    {
        var parts = text.Split([' ', ';', ','], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != count)
        {
            throw new FormatException($"Ожидается {count} чисел, получено {parts.Length}.");
        }

        return parts.Select(p => float.Parse(p, CultureInfo.InvariantCulture)).ToArray();
    }

    private sealed class LoadedFigureInstance(FigureDefinition source, int index)
    {
        public FigureDefinition Source { get; } = source;
        public string DisplayName => $"{index}. {Source.Name}";
        public Vector3 Scale { get; set; } = new(1, 1, 1);
        public float RotationAngle { get; set; }
        public Vector4 RotationAxis { get; set; } = Vector4.UnitY;
        public Vector3 Translation { get; set; }
        public ShearValues Shear { get; set; } = new(0, 0, 0, 0, 0, 0);
        public Vector3 Perspective { get; set; }

        public void ResetTransforms()
        {
            Scale = new Vector3(1, 1, 1);
            RotationAngle = 0;
            RotationAxis = Vector4.UnitY;
            Translation = Vector3.Zero;
            Shear = new ShearValues(0, 0, 0, 0, 0, 0);
            Perspective = Vector3.Zero;
        }
    }

    private readonly record struct ShearValues(float XY, float XZ, float YX, float YZ, float ZX, float ZY);
}
