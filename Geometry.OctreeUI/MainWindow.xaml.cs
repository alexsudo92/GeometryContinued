using Geometry.Base.Octree;
using System;
using System.Numerics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Geometry.OctreeUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Octree _sphereTree;
        private Octree _boxTree;
        private Octree _currentBooleanTree;
        private OctreeSpace _space;
        private float _currentMinSize = 1f;

        public MainWindow()
        {
            InitializeComponent();
            MinSizeSlider.ValueChanged += MinSizeSlider_ValueChanged;
            InitializeOctrees();
            RefreshVisualization();
        }

        private void InitializeOctrees()
        {
            _currentMinSize = (float)MinSizeSlider.Value;

            // Создаем сферу с центром (0,0,0) и радиусом 3
            var bounds = new BoundingBox(new Vector3(0, 0, 0), 50f);
            _sphereTree = new Octree(bounds, _currentMinSize);
            _sphereTree.BuildTree(_sphereTree.Root);
            //_sphereTree.SetObject(GeometricPrimitives.CreateSphere(new Vector3(0, 0, 0), 3f));

            _sphereTree.SetObject(GeometricPrimitives.CreateBox(new Vector3(0, 5, 0), new Vector3(4f, 5f, 2f)));

            //Создаем параллелепипед с центром(-2.5, -3.5, 3) и размерами 6x5x7
            //_boxTree = new Octree(boxBounds, _currentMinSize);
            //_boxTree.SetObject(GeometricPrimitives.CreateBox(new Vector3(0f, 10f, 0f), new Vector3(4, 4, 4)));

            _space = new OctreeSpace();
            _space.AddOctree(_sphereTree);
            //_space.AddOctree(_boxTree);

            _currentBooleanTree = null;
        }

        private void RefreshVisualization()
        {
            InitializeOctrees();
            MainViewport.Children.Clear();

            if (ShowObjectsCheckBox.IsChecked == true)
            {
                // Визуализируем исходные объекты
                if (_currentBooleanTree == null)
                {
                    VisualizeOctree(_sphereTree.Root, System.Windows.Media.Color.FromArgb((byte)(0.5 * 255), 0, 0, 0));
                    //VisualizeOctree(_boxTree.Root, System.Windows.Media.Color.FromArgb((byte)(0.5 * 255), 1, 1, 255));
                }
                else
                {
                }
            }

            //UpdateInfoText();
        }

        private void VisualizeOctree(OctreeNode root, System.Windows.Media.Color color)
        {
            if (root.IsLeaf && root.IsSolid)
            {
                AddCube(root.Bounds.Center, root.Bounds.Size, color);
            }
            else if (!root.IsLeaf)
            {
                foreach (var child in root.Children)
                {
                    if (child != null)
                        VisualizeOctree(child, color);
                }
            }
        }

        private void AddCube(Vector3 center, Vector3 size, System.Windows.Media.Color color)
        {
            var mesh = new MeshGeometry3D();

            // Создаем куб
            var halfX = size.X / 2f;
            var halfY = size.Y / 2f;
            var halfZ = size.Z / 2f;
            Point3D[] points = {
                new Point3D(center.X - halfX, center.Y - halfY, center.Z - halfZ),
                new Point3D(center.X + halfX, center.Y - halfY, center.Z - halfZ),
                new Point3D(center.X + halfX, center.Y + halfY, center.Z - halfZ),
                new Point3D(center.X - halfX, center.Y + halfY, center.Z - halfZ),
                new Point3D(center.X - halfX, center.Y - halfY, center.Z + halfZ),
                new Point3D(center.X + halfX, center.Y - halfY, center.Z + halfZ),
                new Point3D(center.X + halfX, center.Y + halfY, center.Z + halfZ),
                new Point3D(center.X - halfX, center.Y + halfY, center.Z + halfZ)
            };

            // Добавляем вершины
            foreach (var point in points)
            {
                mesh.Positions.Add(point);
            }

            // Добавляем треугольники для всех 6 граней
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

            var material = new DiffuseMaterial(new SolidColorBrush(color));
            var geometryModel = new GeometryModel3D(mesh, material);


            var modelVisual = new ModelVisual3D
            {
                Content = geometryModel
            };

            MainViewport.Children.Add(modelVisual);
        }

        private void UpdateInfoText()
        {
            string volumeInfo = $"Объем сферы: {_sphereTree.GetVolume():F2}\n" +
                              $"Объем параллелепипеда: {_boxTree.GetVolume():F2}\n" +
                              $"Общий объем: {_space.GetTotalVolume():F2}";

            VolumeInfoText.Text = volumeInfo;
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

        private void UnionButton_Click(object sender, RoutedEventArgs e)
        {
            _currentBooleanTree = OctreeSpace.Union(_sphereTree, _boxTree, _currentMinSize);
            RefreshVisualization();
        }

        private void SubtractButton_Click(object sender, RoutedEventArgs e)
        {
            _currentBooleanTree = OctreeSpace.Subtract(_sphereTree, _boxTree, _currentMinSize);
            RefreshVisualization();
        }

        private void IntersectButton_Click(object sender, RoutedEventArgs e)
        {
            _currentBooleanTree = OctreeSpace.Intersect(_sphereTree, _boxTree, _currentMinSize);
            RefreshVisualization();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            _currentBooleanTree = null;
            RefreshVisualization();
        }
    }
}