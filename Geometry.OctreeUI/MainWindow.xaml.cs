using Geometry.Base.Octree;
using System.Collections.ObjectModel;
using System.Numerics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Point = System.Windows.Point;
using Quaternion = System.Windows.Media.Media3D.Quaternion;

namespace Geometry.OctreeUI
{
    public partial class MainWindow : Window
    {
        private readonly OctreeSpace _space;
        private float _currentMinSize = 1f;
        private readonly Dictionary<Vector3, MeshGeometry3D> _cache = [];

        // Переменные для управления камерой
        private PerspectiveCamera _camera;
        private Point3D _cameraPosition;
        private Vector3D _cameraLookDirection;
        private Vector3D _cameraUpDirection;

        private bool _isRotating = false;
        private bool _isPanning = false;
        private Point _lastMousePosition;

        // Параметры чувствительности
        private readonly float _rotationSpeed = 0.1f;
        private readonly float _panSpeed = 0.001f;
        private readonly float _zoomSpeed = 1.1f;

        public ObservableCollection<OctreeModel> OctreeModels { get; set; } = [];

        public MainWindow()
        {
            _space = new OctreeSpace();
            InitializeComponent();
            this.DataContext = this;

            InitializeCamera();

            MinSizeSlider.ValueChanged += MinSizeSlider_ValueChanged;
        }

        private void InitializeCamera()
        {
            _cameraPosition = new(20, 20, 20);
            _cameraLookDirection = new(-1, -1, -1);
            _cameraUpDirection = new(0, 1, 0);

            _camera = new PerspectiveCamera(
                _cameraPosition,
                _cameraLookDirection,
                _cameraUpDirection,
                45f
            );

            MainViewport.Camera = _camera;
        }

        private void RefreshVisualization()
        {
            MainViewport.Children.Clear();

            var maxDepth = 10;

            for (var depth = 0; depth <= maxDepth; depth++)
            {
                var sizeGroups = new Dictionary<Vector3, List<Vector3>>();

                foreach (var space in _space.Octrees)
                {
                    CollectNodesByDepth(space.Root, depth, 0, sizeGroups);
                }

                foreach (var kvp in sizeGroups)
                {
                    if (kvp.Value.Count > 0)
                    {
                        RenderLayerMeshOptimized(kvp.Key, kvp.Value, depth);
                    }
                }
            }
        }

        private void RenderLayerMeshOptimized(Vector3 size, List<Vector3> centers, int depth)
        {
            if (centers.Count == 0) return;

            var mesh = new MeshGeometry3D();
            if (!_cache.TryGetValue(size, out var cube))
            {
                cube = GenerateCube(size);
                _cache.Add(size, cube);
            }

            foreach (var center in centers)
            {
                var baseIndex = mesh.Positions.Count;

                foreach (var position in cube.Positions)
                {
                    mesh.Positions.Add(new Point3D(
                        position.X + center.X,
                        position.Y + center.Y,
                        position.Z + center.Z
                    ));
                }

                foreach (var index in cube.TriangleIndices)
                {
                    mesh.TriangleIndices.Add(baseIndex + index);
                }
            }

            var rnd = new Random();
            var a = 127;
            var r = 255 * MathF.Sin(size.X);
            var g = 255 * MathF.Atan(size.Z);
            var b = 255 * MathF.Sqrt(size.Y);
            var color = Color.FromArgb((byte)a, (byte)r, (byte)g, (byte)b);

            var material = new DiffuseMaterial(new SolidColorBrush(color));
            var geometryModel = new GeometryModel3D(mesh, material);

            var group = new Model3DGroup();
            group.Children.Add(geometryModel);
            group.Children.Add(new AmbientLight(Colors.White));
            group.Children.Add(new DirectionalLight(Colors.White, new Vector3D(-1, -1, -1)));

            var visual = new ModelVisual3D { Content = group };
            MainViewport.Children.Add(visual);
        }

        private void CollectNodesByDepth(OctreeNode node, int targetDepth, int currentDepth, Dictionary<Vector3, List<Vector3>> sizeGroups)
        {
            if (currentDepth == targetDepth)
            {

                if (ShouldRenderNode(node))
                {
                    var size = node.Bounds.Size;
                    var center = node.Bounds.Center;

                    if (!sizeGroups.TryGetValue(size, out List<Vector3>? value))
                    {
                        value = [];
                        sizeGroups[size] = value;
                    }

                    value.Add(center * 2f);
                    node.IsRendered = true;
                }

            }
            else if (currentDepth < targetDepth && node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    if (child != null)
                    {
                        CollectNodesByDepth(child, targetDepth, currentDepth + 1, sizeGroups);
                    }
                }
            }
        }

        private bool ShouldRenderNode(OctreeNode node)
        {
            return node.IsLeaf && node.IsSolid;
        }

        private MeshGeometry3D GenerateCube(Vector3 size)
        {
            var mesh = new MeshGeometry3D();

            var points = new Point3D[]
            {
                new (-size.X, -size.Y, -size.Z),
                new (size.X, -size.Y, -size.Z),
                new (size.X, size.Y, -size.Z),
                new (-size.X, size.Y, -size.Z),
                new (-size.X, -size.Y, size.Z),
                new (size.X, -size.Y, size.Z),
                new (size.X, size.Y, size.Z),
                new (-size.X, size.Y, size.Z),
            };

            foreach (var point in points)
            {
                mesh.Positions.Add(point);
            }

            var triangles = new int[][] {
                [0, 1, 2, 0, 2, 3], // передняя
                [1, 5, 6, 1, 6, 2], // правая
                [5, 4, 7, 5, 7, 6], // задняя
                [4, 0, 3, 4, 3, 7], // левая
                [3, 2, 6, 3, 6, 7], // верхняя
                [4, 5, 1, 4, 1, 0]  // нижняя
            };

            foreach (var face in triangles)
            {
                foreach (var index in face)
                {
                    mesh.TriangleIndices.Add(index);
                }
            }

            return mesh;
        }

        #region CameraControls
        private void Viewport_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed)
            {
                _isRotating = true;
                _lastMousePosition = e.GetPosition(MainViewport);
                MainViewport.CaptureMouse();
                MainViewport.Cursor = Cursors.SizeAll;
            }
            else if (e.MiddleButton == MouseButtonState.Pressed)
            {
                _isPanning = true;
                _lastMousePosition = e.GetPosition(MainViewport);
                MainViewport.CaptureMouse();
                MainViewport.Cursor = Cursors.ScrollAll;
            }
        }

        private void Viewport_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isRotating = false;
            _isPanning = false;
            MainViewport.ReleaseMouseCapture();
            MainViewport.Cursor = Cursors.Arrow;
        }

        private void Viewport_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isRotating && !_isPanning) return;

            var currentPosition = e.GetPosition(MainViewport);
            var delta = currentPosition - _lastMousePosition;

            if (_isRotating)
            {
                RotateCamera(delta.X, delta.Y);
            }
            else if (_isPanning)
            {
                PanCamera(delta.X, delta.Y);
            }

            _lastMousePosition = currentPosition;
        }

        private void Viewport_MouseWheel(object sender, MouseWheelEventArgs e)
        {

            ZoomCamera(e.Delta > 0f ? _zoomSpeed : 1.0f / _zoomSpeed);
        }

        private void RotateCamera(double deltaX, double deltaY)
        {
            var lookDirection = _cameraLookDirection;
            var upDirection = _cameraUpDirection;

            var rightDirection = Vector3D.CrossProduct(lookDirection, upDirection);

            var rotationY = new Quaternion(upDirection, -deltaX * _rotationSpeed);
            var rotationX = new Quaternion(rightDirection, -deltaY * _rotationSpeed);
            var totalRotation = rotationX * rotationY;

            var rotationMatrix = new Matrix3D();
            rotationMatrix.Rotate(totalRotation);

            lookDirection = rotationMatrix.Transform(lookDirection);
            upDirection = rotationMatrix.Transform(upDirection);

            _cameraLookDirection = lookDirection;
            _cameraUpDirection = upDirection;

            UpdateCamera();
        }

        private void PanCamera(double deltaX, double deltaY)
        {
            var actualDirection = Vector3D.CrossProduct(_cameraLookDirection, _cameraUpDirection);
            actualDirection.Normalize();

            var actualUp = Vector3D.CrossProduct(actualDirection, _cameraLookDirection);
            actualUp.Normalize();

            var cameraDistance = ((Vector3D)_cameraPosition).Length;
            var panFactor = _panSpeed * cameraDistance;

            _cameraPosition -= actualDirection * deltaX * panFactor;
            _cameraPosition += actualUp * deltaY * panFactor;

            UpdateCamera();
        }

        private void ZoomCamera(double factor)
        {
            var currentDistance = ((Vector3D)_cameraPosition).Length;

            var lookDirection = _cameraLookDirection;
            lookDirection.Normalize();

            _cameraPosition += lookDirection * currentDistance * (factor - 1.0);

            UpdateCamera();
        }

        private void UpdateCamera()
        {
            _camera.Position = _cameraPosition;
            _camera.LookDirection = _cameraLookDirection;
            _camera.UpDirection = _cameraUpDirection;
        }

        private void CameraReset_Click(object sender, RoutedEventArgs e)
        {
            _cameraPosition = new(20, 20, 20);
            _cameraLookDirection = new(-1, -1, -1);
            _cameraUpDirection = new(0, 1, 0);

            UpdateCamera();
        }
        #endregion

        private void MinSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            MinSizeText.Text = e.NewValue.ToString("F1");
            _currentMinSize = (float)e.NewValue;
        }

        private void BuildCylinder_Click(object sender, RoutedEventArgs e)
        {
            var x = Convert.ToSingle(string.IsNullOrEmpty(CylinderX.Text) ? 0f : CylinderX.Text.Replace('.', ','));
            var y = Convert.ToSingle(string.IsNullOrEmpty(CylinderY.Text) ? 0f : CylinderY.Text.Replace('.', ','));
            var z = Convert.ToSingle(string.IsNullOrEmpty(CylinderZ.Text) ? 0f : CylinderZ.Text.Replace('.', ','));

            var radius = Convert.ToSingle(string.IsNullOrEmpty(CylinderRadius.Text) ? 0f : CylinderRadius.Text.Replace('.', ','));
            var height = Convert.ToSingle(string.IsNullOrEmpty(CylinderHeight.Text) ? 0f : CylinderHeight.Text.Replace('.', ','));

            var position = new Vector3(x, y, z);
            var size = new Vector3(radius * 2f, height * 2f, radius * 2f);
            var bounds = new BoundingBox(position, size);

            var tree = new Octree(bounds, _currentMinSize);
            var cylinder = GeometricPrimitives.CreateCylinder(position, Vector3.UnitY, radius, height);
            tree.SetObject(cylinder);

            _space.Octrees.Add(tree);
            AddModel(new OctreeModel { Name = $"{OctreeModels.Count + 1}. Цилиндр\nR={radius}\nH={height}", Position = position, Volume = tree.GetVolume() });

            RefreshVisualization();
        }

        private void BuildSphere_Click(object sender, RoutedEventArgs e)
        {
            var x = Convert.ToSingle(string.IsNullOrEmpty(SphereX.Text) ? 0f : SphereX.Text.Replace('.', ','));
            var y = Convert.ToSingle(string.IsNullOrEmpty(SphereY.Text) ? 0f : SphereY.Text.Replace('.', ','));
            var z = Convert.ToSingle(string.IsNullOrEmpty(SphereZ.Text) ? 0f : SphereZ.Text.Replace('.', ','));

            var radius = Convert.ToSingle(string.IsNullOrEmpty(SphereRadius.Text) ? 0f : SphereRadius.Text.Replace('.', ','));

            var position = new Vector3(x, y, z);
            var size = new Vector3(radius * 2f);
            var bounds = new BoundingBox(position, size);

            var tree = new Octree(bounds, _currentMinSize);
            var sphere = GeometricPrimitives.CreateSphere(position, radius);
            tree.SetObject(sphere);

            _space.Octrees.Add(tree);
            AddModel(new OctreeModel { Name = $"{OctreeModels.Count + 1}. Сфера\nR={radius}", Position = position, Volume = tree.GetVolume() });

            RefreshVisualization();
        }

        private void AddModel(OctreeModel octreeModel)
        {
            OctreeModels.Add(octreeModel);
        }

        private void GetPoint_Click(object sender, RoutedEventArgs e)
        {
            var x = Convert.ToSingle(string.IsNullOrEmpty(TestX.Text) ? 0f : TestX.Text.Replace('.', ','));
            var y = Convert.ToSingle(string.IsNullOrEmpty(TestY.Text) ? 0f : TestY.Text.Replace('.', ','));
            var z = Convert.ToSingle(string.IsNullOrEmpty(TestZ.Text) ? 0f : TestZ.Text.Replace('.', ','));

            var testObject = TestPointObject.SelectedIndex;

            var tree = _space.Octrees[testObject];

            if (tree is null)
            {
                MessageBox.Show("Объект не выбран");
                return;
            }

            var inside = tree.ContainsPoint(new Vector3(x, y, z));

            if (inside)
            {
                MessageBox.Show("Точка внутри объекта");
            }
            else
            {
                MessageBox.Show("Точка снаружи объекта");
            }
        }

        private void ClearVisualization_Click(object sender, RoutedEventArgs e)
        {
            _space.Octrees.Clear();
            OctreeModels.Clear();
            MainViewport.Children.Clear();
        }

        private void SubtractButton_Click(object sender, RoutedEventArgs e)
        {
            var first = FirstObject.SelectedIndex;
            var second = SecondObject.SelectedIndex;

            if (first == second)
            {
                MessageBox.Show("Выберите разные объекты для пересечения");
                return;
            }

            var tree1 = _space.Octrees[first];
            var tree2 = _space.Octrees[second];

            var model1 = OctreeModels[first];
            var model2 = OctreeModels[second];

            if (tree1 is not null && tree2 is not null)
            {
                var tree = OctreeSpace.Subtract(tree1, tree2, _currentMinSize);

                _space.Octrees.Remove(tree1);
                _space.Octrees.Remove(tree2);
                OctreeModels.Remove(model1);
                OctreeModels.Remove(model2);

                _space.Octrees.Add(tree);
                AddModel(new OctreeModel { Name = $"{OctreeModels.Count + 1}. Вычитание", Position = tree.Bounds.Center, Volume = tree.GetVolume() });
                RefreshVisualization();
            }
            else
            {
                MessageBox.Show("Один из выбранных объектов не существует");
                return;
            }
        }

        private void IntersectButton_Click(object sender, RoutedEventArgs e)
        {
            var first = FirstObject.SelectedIndex;
            var second = SecondObject.SelectedIndex;

            if (first == second)
            {
                MessageBox.Show("Выберите разные объекты для пересечения");
                return;
            }

            var tree1 = _space.Octrees[first];
            var tree2 = _space.Octrees[second];

            var model1 = OctreeModels[first];
            var model2 = OctreeModels[second];

            if (tree1 is not null && tree2 is not null)
            {
                var tree = OctreeSpace.Intersection(tree1, tree2, _currentMinSize);

                _space.Octrees.Remove(tree1);
                _space.Octrees.Remove(tree2);
                OctreeModels.Remove(model1);
                OctreeModels.Remove(model2);

                _space.Octrees.Add(tree);
                AddModel(new OctreeModel { Name = $"{OctreeModels.Count + 1}. Пересечение", Position = tree.Bounds.Center, Volume = tree.GetVolume() });
                RefreshVisualization();

            }
            else
            {
                MessageBox.Show("Один из выбранных объектов не существует");
                return;
            }
        }

        private void UnionButton_Click(object sender, RoutedEventArgs e)
        {
            var first = FirstObject.SelectedIndex;
            var second = SecondObject.SelectedIndex;

            if (first == second)
            {
                MessageBox.Show("Выберите разные объекты для пересечения");
                return;
            }

            var tree1 = _space.Octrees[first];
            var tree2 = _space.Octrees[second];

            var model1 = OctreeModels[first];
            var model2 = OctreeModels[second];

            if (tree1 is not null && tree2 is not null)
            {
                var tree = OctreeSpace.Union(tree1, tree2, _currentMinSize);

                _space.Octrees.Remove(tree1);
                _space.Octrees.Remove(tree2);
                OctreeModels.Remove(model1);
                OctreeModels.Remove(model2);

                _space.Octrees.Add(tree);
                AddModel(new OctreeModel { Name = $"{OctreeModels.Count + 1}. Объединение", Position = tree.Bounds.Center, Volume = tree.GetVolume() });
                RefreshVisualization();

            }
            else
            {
                MessageBox.Show("Один из выбранных объектов не существует");
                return;
            }
        }

        private void RemoveObject_Click(object sender, RoutedEventArgs e)
        {
            var selected = ModelsList.SelectedIndex;

            if (selected == -1)
            {
                MessageBox.Show("Выберите объект для удаления");
                return;
            }

            var tree = _space.Octrees[selected];
            var model = OctreeModels[selected];

            if (model is not null && tree is not null)
            {
                _space.Octrees.Remove(tree);
                OctreeModels.Remove(model);
                RefreshVisualization();
            }
            else
            {
                MessageBox.Show("Объект не существует");
                return;
            }
        }
    }
}