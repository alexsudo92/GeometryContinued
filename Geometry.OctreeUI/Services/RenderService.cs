using Geometry.Base.Octree;
using System.Numerics;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Geometry.OctreeUI.Services
{
    public class RenderService
    {
        private static readonly Dictionary<float, MeshGeometry3D> _cache = [];

        public static List<ModelVisual3D> RenderOctreeSpace(OctreeSpace space)
        {
            var models = new List<ModelVisual3D>();
            var sizeGroups = new Dictionary<float, List<Vector3>>();

            foreach (var octree in space.Octrees)
            {
                CollectNodesByDepth(octree.Root, sizeGroups);
            }

            foreach (var kvp in sizeGroups)
            {
                if (kvp.Value.Count > 0)
                {
                    var model = RenderLayerMeshOptimized(kvp.Key, kvp.Value);
                    if (model is not null)
                    {
                        models.Add(model);
                    }
                }
            }

            return models;
        }

        private static ModelVisual3D? RenderLayerMeshOptimized(float size, List<Vector3> centers)
        {
            if (centers.Count == 0) return null;

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

            var a = 127;
            var r = 255 * MathF.Sin(size);
            var g = 255 * MathF.Atan(size);
            var b = 255 * MathF.Sqrt(size);
            var color = Color.FromArgb((byte)a, (byte)r, (byte)g, (byte)b);

            var material = new DiffuseMaterial(new SolidColorBrush(color));
            var geometryModel = new GeometryModel3D(mesh, material);

            var group = new Model3DGroup();
            group.Children.Add(geometryModel);
            group.Children.Add(new AmbientLight(Colors.White));
            group.Children.Add(new DirectionalLight(Colors.White, new Vector3D(-1, -1, -1)));

            var visual = new ModelVisual3D { Content = group };

            return visual;
        }

        private static void CollectNodesByDepth(OctreeNode node, Dictionary<float, List<Vector3>> sizeGroups)
        {
            if (node.IsLeaf && node.IsSolid)
            {
                var size = node.Bounds.Size;
                var center = node.Bounds.Center;

                if (!sizeGroups.TryGetValue(size, out List<Vector3>? value))
                {
                    value = [];
                    sizeGroups[size] = value;
                }

                value.Add(center * 2f);
            }
            else if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    if (child != null)
                    {
                        CollectNodesByDepth(child, sizeGroups);
                    }
                }
            }
        }

        private static MeshGeometry3D GenerateCube(float size)
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

            return mesh;
        }
    }
}
