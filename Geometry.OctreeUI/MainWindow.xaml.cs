using Geometry.Base.Octree;
using System.Collections.Concurrent;
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
        private Octree _sphereTree;
        private Octree _boxTree;
        private OctreeSpace _space;
        private float _currentMinSize = 1f;
        private Model3DGroup _group;

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
            InitializeComponent();
            MinSizeSlider.ValueChanged += MinSizeSlider_ValueChanged;
            InitializeCamera();
            RefreshVisualization();
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

        private void InitializeOctrees()
        {
            //_currentMinSize = (float)MinSizeSlider.Value;

            _currentMinSize = 2f;

            var sphereBounds = new BoundingBox(new Vector3(0, 0, 0), 20f);
            _sphereTree = new Octree(sphereBounds, _currentMinSize);
            _sphereTree.BuildTree(_sphereTree.Root);
            _sphereTree.SetObject(GeometricPrimitives.CreateSphere(new Vector3(0, 0, 0), 10f));

            //var sphereBounds2 = new BoundingBox(new Vector3(0, 0, 0), 4f);
            //var sphereTree2 = new Octree(sphereBounds2, _currentMinSize);
            //sphereTree2.BuildTree(sphereTree2.Root);
            //sphereTree2.SetObject(GeometricPrimitives.CreateSphere(new Vector3(0, 0, 0), 4f));

            _space = new OctreeSpace();
            _space.Octrees.Add(_sphereTree);
            //_space.Octrees.Add(sphereTree2);
        }

        private void RefreshVisualization()
        {
            MainViewport.Children.Clear();
            InitializeOctrees();
            VisualizeSpace(_space);
            //UpdateInfoText();
        }

        private float GetMinimumCube(float size)
        {
            var minSize = size;
            while (minSize > _currentMinSize)
            {
                minSize /= 2;
            }

            return minSize;
        }

        private void VisualizeSpace(OctreeSpace space)
        {
            space.Octrees.ForEach((s) =>
            {
                //var minCubeSize = GetMinimumCube(s.Root.Bounds.Size);
                _group = GenerateCube(_currentMinSize);
                VisualizeOctree(s.Root);
            });
        }

        private void VisualizeOctree(OctreeNode root)
        {
            if (root.IsLeaf && root.IsSolid)
            {
                RenderOctant(root.Bounds.Center*2);
            }
            else if (!root.IsLeaf)
            {
                foreach (var child in root.Children)
                {
                    if (child != null)
                        VisualizeOctree(child);
                }
            }
        }

        private void RenderOctant(Vector3 center)
        {
            var model = new ModelVisual3D
            {
                Content = _group,
                Transform = new TranslateTransform3D(center.X, center.Y, center.Z)
            };

            MainViewport.Children.Add(model);
        }

        private static Model3DGroup GenerateCube(float size)
        {
            var mesh = new MeshGeometry3D();

            var points = new Point3D[]
            {
                new (-size, -size, -size),
                new (size, -size, -size),
                new (size, size, -size),
                new (-size, size, -size),
                new (-size, -size, size),
                new (size, -size, size),
                new (size, size, size),
                new (-size, size, size),
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
            var material = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(127, 0, 0, 0)));
            var geometryModel = new GeometryModel3D(mesh, material);
            var ambientLight = new AmbientLight(Colors.DarkGray);
            var directionalLight = new DirectionalLight(Colors.White, new Vector3D(-1, -1, -1));

            var group = new Model3DGroup();
            group.Children.Add(geometryModel);
            group.Children.Add(ambientLight);
            group.Children.Add(directionalLight);

            return group;
        }

        private void UpdateInfoText()
        {
            string volumeInfo = $"Объем сферы: {_sphereTree.GetVolume():F2}\n" +
                              //$"Объем параллелепипеда: {_boxTree.GetVolume():F2}\n" +
                              $"Общий объем: {_space.GetTotalVolume():F2}";

            VolumeInfoText.Text = volumeInfo;
        }









        // Обработчики событий мыши для управления камерой
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
            // Вращение с помощью правой кнопки мыши
            var lookDirection = _cameraLookDirection;
            var upDirection = _cameraUpDirection;

            // Создаем кватернионы для вращения
            var rightDirection = Vector3D.CrossProduct(lookDirection, upDirection);
            rightDirection.Normalize();

            // Вращение вокруг вертикальной оси (Y)
            var rotationY = new Quaternion(upDirection, -deltaX * _rotationSpeed);

            // Вращение вокруг горизонтальной оси (X)
            var rotationX = new Quaternion(rightDirection, -deltaY * _rotationSpeed);

            // Комбинируем вращения
            var totalRotation = rotationX * rotationY;

            // Применяем вращение к направлениям камеры
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
            // Панорамирование с помощью колесика мыши
            var lookDirection = _cameraLookDirection;
            var upDirection = _cameraUpDirection;

            // Вычисляем правый вектор
            var rightDirection = Vector3D.CrossProduct(lookDirection, upDirection);
            rightDirection.Normalize();

            // Вычисляем реальный вектор "вверх" (перпендикулярно взгляду и правому вектору)
            var actualUp = Vector3D.CrossProduct(rightDirection, lookDirection);
            actualUp.Normalize();

            // Вычисляем коэффициент панорамирования на основе расстояния камеры
            var cameraDistance = ((Vector3D)_cameraPosition).Length;
            var panFactor = _panSpeed * cameraDistance;

            // Перемещаем камеру в противоположном направлении (чтобы двигать сцену)
            _cameraPosition -= rightDirection * deltaX * panFactor;
            _cameraPosition += actualUp * deltaY * panFactor;

            UpdateCamera();
        }

        private void ZoomCamera(double factor)
        {
            // Масштабирование с помощью колеса мыши
            var currentDistance = ((Vector3D)_cameraPosition).Length;

            // Вычисляем новую позицию камеры вдоль направления взгляда
            var lookDirection = _cameraLookDirection;
            lookDirection.Normalize();

            // Уменьшаем/увеличиваем расстояние камеры
            _cameraPosition += lookDirection * currentDistance * (factor - 1.0);

            UpdateCamera();
        }

        private void UpdateCamera()
        {
            _camera.Position = _cameraPosition;
            _camera.LookDirection = _cameraLookDirection;
            _camera.UpDirection = _cameraUpDirection;
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

        //private void UnionButton_Click(object sender, RoutedEventArgs e)
        //{
        //    _currentBooleanTree = OctreeSpace.Union(_sphereTree, _boxTree, _currentMinSize);
        //    RefreshVisualization();
        //}

        //private void SubtractButton_Click(object sender, RoutedEventArgs e)
        //{
        //    _currentBooleanTree = OctreeSpace.Subtract(_sphereTree, _boxTree, _currentMinSize);
        //    RefreshVisualization();
        //}

        //private void IntersectButton_Click(object sender, RoutedEventArgs e)
        //{
        //    _currentBooleanTree = OctreeSpace.Intersect(_sphereTree, _boxTree, _currentMinSize);
        //    RefreshVisualization();
        //}

        //private void ResetButton_Click(object sender, RoutedEventArgs e)
        //{
        //    _currentBooleanTree = null;
        //    RefreshVisualization();
        //}

        private void CameraReset_Click(object sender, RoutedEventArgs e)
        {
            _cameraPosition = new Point3D(20, 20, 20);
            _cameraLookDirection = new Vector3D(-1, -1, -1);
            _cameraUpDirection = new Vector3D(0, 1, 0);

            UpdateCamera();
        }
    }
}