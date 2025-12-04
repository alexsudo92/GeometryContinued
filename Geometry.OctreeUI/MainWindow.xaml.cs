using Geometry.Base.Octree;
using System.Numerics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Point = System.Windows.Point;
using Quaternion = System.Windows.Media.Media3D.Quaternion;

namespace Geometry.OctreeUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private OctreeSpace _space;
        private float _currentMinSize = 1f;

        // Переменные для управления камерой
        private PerspectiveCamera _camera;
        private Point3D _cameraPosition = new Point3D(20, 20, 20);
        private Vector3D _cameraLookDirection = new Vector3D(-1, -1, -1);
        private Vector3D _cameraUpDirection = new Vector3D(0, 1, 0);
        private float _cameraFieldOfView = 45f;

        private bool _isRotating = false;
        private bool _isPanning = false;
        private Point _lastMousePosition;

        // Параметры чувствительности
        private float _rotationSpeed = 0.1f;
        private float _panSpeed = 0.001f;
        private float _zoomSpeed = 1.2f;


        public MainWindow()
        {
            _space = new OctreeSpace();
            InitializeComponent();

            InitializeCamera();

            MinSizeSlider.ValueChanged += MinSizeSlider_ValueChanged;
        }

        private void InitializeCamera()
        {
            _camera = new PerspectiveCamera(
                _cameraPosition,
                _cameraLookDirection,
                _cameraUpDirection,
                _cameraFieldOfView
            );

            MainViewport.Camera = _camera;
        }

        private void RefreshVisualization()
        {
            MainViewport.Children.Clear();
            VisualizeSpace(_space);
        }

        private void VisualizeSpace(OctreeSpace space)
        {
            space.Octrees.ForEach((s) =>
            {
                VisualizeOctree(s.Root);
            });
        }

        private void VisualizeOctree(OctreeNode root)
        {
            if (root.IsSolid && !root.IsRendered)
            {
                RenderOctant(root.Bounds.Size, root.Bounds.Center * 2f);
                root.IsRendered = true;
            }
            else if (root.Children != null)
            {
                var count = 0;
                foreach (var child in root.Children)
                {
                    if (child.IsSolid)
                    {
                        count++;
                    }
                }

                if (count == 8 && !root.IsRendered)
                {
                    RenderOctant(root.Bounds.Size, root.Bounds.Center * 2f);
                    root.IsRendered = true;
                }
                else
                {
                    foreach (var child in root.Children)
                    {
                        if (child != null)
                            VisualizeOctree(child);
                    }
                }
            }
        }

        private readonly Dictionary<Vector3, Model3DGroup> _cache = [];
        private int counter;
        private void RenderOctant(Vector3 size, Vector3 center)
        {
            _cache.TryGetValue(size, out Model3DGroup? cube);

            if (cube == null)
            {
                cube = GenerateCube(size);
                _cache.Add(size, cube);
            }

            var model = new ModelVisual3D
            {
                Content = cube,
                Transform = new TranslateTransform3D(center.X, center.Y, center.Z)
            };

            counter++;
            MainViewport.Children.Add(model);
        }

        private Model3DGroup GenerateCube(Vector3 size)
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

            var a = 127;

            var r = 255 * MathF.Tan(size.X);
            var b = 255 * MathF.Sin(size.Y);
            var g = 255 * MathF.Cos(size.Z);

            //var r = 0;
            //var g = 0;
            //var b = 0;

            var material = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb((byte)a, (byte)r, (byte)g, (byte)b)));
            var geometryModel = new GeometryModel3D(mesh, material);
            var ambientLight = new AmbientLight(Colors.White);
            var directionalLight = new DirectionalLight(Colors.White, new Vector3D(-1, -1, -1));

            var group = new Model3DGroup();
            group.Children.Add(geometryModel);
            group.Children.Add(ambientLight);
            group.Children.Add(directionalLight);

            return group;
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
            _cameraPosition = new Point3D(20, 20, 20);
            _cameraLookDirection = new Vector3D(-1, -1, -1);
            _cameraUpDirection = new Vector3D(0, 1, 0);

            UpdateCamera();
        }
        #endregion

        private void UpdateInfoText()
        {
            var volumeList = new List<float>();

            _space.Octrees.ForEach((s) =>
            {
                var v = s.GetVolume();
                volumeList.Add(v);
            });

            var volumeInfo = "";

            volumeList.ForEach((v) =>
            {
                volumeInfo += $"{v:F2}\n";
            });

            SphereVolume.Text = volumeInfo;
        }

        private void MinSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            MinSizeText.Text = e.NewValue.ToString("F1");
            _currentMinSize = (float)e.NewValue;
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshVisualization();
        }

        //private void SubtractButton_Click(object sender, RoutedEventArgs e)
        //{
        //    var tree = OctreeSpace.Subtract(_sphereTree, _boxTree, _currentMinSize);
        //    _space.Octrees.Clear();
        //    _space.Octrees.Add(tree);
        //    RefreshVisualization();
        //}

        //private void IntersectButton_Click(object sender, RoutedEventArgs e)
        //{
        //    var tree = OctreeSpace.Intersect(_sphereTree, _boxTree, _currentMinSize);
        //    _space.Octrees.Clear();
        //    _space.Octrees.Add(tree);
        //    RefreshVisualization();
        //}

        private void BuildBox_Click(object sender, RoutedEventArgs e)
        {
            var x = Convert.ToSingle(BoxX.Text.Replace('.', ','));
            var y = Convert.ToSingle(BoxY.Text.Replace('.', ','));
            var z = Convert.ToSingle(BoxZ.Text.Replace('.', ','));

            var width = Convert.ToSingle(BoxWidth.Text.Replace('.', ','));
            var height = Convert.ToSingle(BoxHeight.Text.Replace('.', ','));
            var length = Convert.ToSingle(BoxLength.Text.Replace('.', ','));

            var position = new Vector3(x, y, z);
            var size = new Vector3(width, height, length);
            var bounds = new BoundingBox(position, size);

            var tree = new Octree(bounds, _currentMinSize);
            tree.BuildTree(tree.Root);
            tree.SetObject(GeometricPrimitives.CreateBox(position, size));
            tree.OptimizeTree();

            _space.Octrees.Add(tree);

            RefreshVisualization();
        }

        private void BuildSphere_Click(object sender, RoutedEventArgs e)
        {
            var x = Convert.ToSingle(SphereX.Text.Replace('.', ','));
            var y = Convert.ToSingle(SphereY.Text.Replace('.', ','));
            var z = Convert.ToSingle(SphereZ.Text.Replace('.', ','));

            var radius = Convert.ToSingle(SphereRadius.Text.Replace('.', ','));

            var position = new Vector3(x, y, z);
            var size = new Vector3(radius * 2f);
            var bounds = new BoundingBox(position, size);

            _space.Octrees.Clear();
            var tree = new Octree(bounds, _currentMinSize);
            tree.BuildTree(tree.Root);
            tree.SetObject(GeometricPrimitives.CreateSphere(position, radius));
            tree.OptimizeTree();

            _space.Octrees.Add(tree);

            RefreshVisualization();
        }

        private void GetPoint_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}