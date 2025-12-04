using System.Numerics;

namespace Geometry.Base.Octree
{
    // Пространство для размещения нескольких Octree
    public class OctreeSpace
    {
        public readonly List<Octree> Octrees;

        public OctreeSpace()
        {
            Octrees = [];
        }

        public float GetTotalVolume()
        {
            var volume = 0f;
            foreach (var octree in Octrees)
            {
                volume += octree.GetVolume();
            }
            return volume;
        }

        // Булевы операции между двумя Octree
        public static Octree Union(Octree tree1, Octree tree2, float minOctantSize)
        {
            var bounds = GetCombinedBounds(tree1.Bounds, tree2.Bounds);
            var result = new Octree(bounds, minOctantSize);

            // Сэмплируем пространство для определения объединения
            SampleSpaceForBooleanOp(result, tree1, tree2, (p1, p2) => p1 || p2);

            return result;
        }

        public static Octree Subtract(Octree tree1, Octree tree2, float minOctantSize)
        {
            var bounds = tree1.Bounds;
            var result = new Octree(bounds, minOctantSize);

            // Сэмплируем пространство для определения разности
            SampleSpaceForBooleanOp(result, tree1, tree2, (p1, p2) => p1 && !p2);

            return result;
        }

        public static Octree Intersect(Octree tree1, Octree tree2, float minOctantSize)
        {
            var bounds = GetIntersectionBounds(tree1.Bounds, tree2.Bounds);

            if (bounds.Size == Vector3.Zero)
                return new Octree(new BoundingBox(tree1.Bounds.Center, Vector3.Zero), minOctantSize);

            var result = new Octree(bounds, minOctantSize);
            result.BuildTree(result.Root);

            // Сэмплируем пространство для определения пересечения
            SampleSpaceForBooleanOp(result, tree1, tree2, (p1, p2) => p1 && p2);

            return result;
        }

        private static void SampleSpaceForBooleanOp(Octree result, Octree tree1, Octree tree2, Func<bool, bool, bool> operation)
        {
            // Рекурсивно обходим результат и определяем принадлежность на основе операции
            SampleNode(result.Root, tree1, tree2, operation);
        }

        private static void SampleNode(OctreeNode node, Octree tree1, Octree tree2, Func<bool, bool, bool> operation)
        {
            if (node.IsLeaf)
            {
                var center = node.Bounds.Center;
                bool inTree1 = tree1.ContainsPoint(center);
                bool inTree2 = tree2.ContainsPoint(center);
                node.IsSolid = operation(inTree1, inTree2);
                return;
            }

            foreach (var child in node.Children)
            {
                if (child != null)
                    SampleNode(child, tree1, tree2, operation);
            }
        }

        private static BoundingBox GetCombinedBounds(BoundingBox box1, BoundingBox box2)
        {
            var min = Vector3.Min(box1.Min, box2.Min);
            var max = Vector3.Max(box1.Max, box2.Max);
            var center = (min + max) * 0.5f;
            var size = max - min;

            return new BoundingBox(center, size);
        }

        private static BoundingBox GetIntersectionBounds(BoundingBox box1, BoundingBox box2)
        {
            var min = Vector3.Max(box1.Min, box2.Min);
            var max = Vector3.Min(box1.Max, box2.Max);

            if (min.X >= max.X || min.Y >= max.Y || min.Z >= max.Z)
                return new BoundingBox(box1.Center, Vector3.Zero);

            var center = (min + max) * 0.5f;
            var size = max - min;

            return new BoundingBox(center, size);
        }
    }
}
