using System.Numerics;

namespace Geometry.Base.Octree
{
    public class OctreeSpace
    {
        public readonly List<Octree> Octrees;

        public OctreeSpace()
        {
            Octrees = [];
        }

        public static Octree Union(Octree tree1, Octree tree2, float minOctantSize)
        {
            var combinedBounds = CombineBounds(tree1.Bounds, tree2.Bounds);
            var resultTree = new Octree(combinedBounds, minOctantSize);

            resultTree.SetObject(point =>
                tree1.ContainsPoint(point) || tree2.ContainsPoint(point));

            return resultTree;
        }

        public static Octree Intersection(Octree tree1, Octree tree2, float minOctantSize)
        {
            var intersectionBounds = GetIntersectionBounds(tree1.Bounds, tree2.Bounds);

            if (intersectionBounds == null)
                return new Octree(new BoundingBox(Vector3.Zero, 0f), minOctantSize);

            var resultTree = new Octree(intersectionBounds.Value, minOctantSize);

            resultTree.SetObject(point =>
                tree1.ContainsPoint(point) && tree2.ContainsPoint(point));

            return resultTree;
        }

        public static Octree Subtract(Octree tree1, Octree tree2, float minOctantSize)
        {
            var resultTree = new Octree(tree1.Bounds, minOctantSize);

            resultTree.SetObject(point =>
                tree1.ContainsPoint(point) && !tree2.ContainsPoint(point));

            return resultTree;
        }

        private static BoundingBox CombineBounds(BoundingBox box1, BoundingBox box2)
        {
            var min1 = box1.Min;
            var max1 = box1.Max;
            var min2 = box2.Min;
            var max2 = box2.Max;

            var combinedMin = new Vector3(
                MathF.Min(min1.X, min2.X),
                MathF.Min(min1.Y, min2.Y),
                MathF.Min(min1.Z, min2.Z)
            );

            var combinedMax = new Vector3(
                MathF.Max(max1.X, max2.X),
                MathF.Max(max1.Y, max2.Y),
                MathF.Max(max1.Z, max2.Z)
            );

            var center = (combinedMin + combinedMax) / 2;
            var size = combinedMax - combinedMin;

            return new BoundingBox(center, MathF.Max(size.X, MathF.Max(size.Y, size.Z)));
        }

        private static BoundingBox? GetIntersectionBounds(BoundingBox box1, BoundingBox box2)
        {
            var min1 = box1.Min;
            var max1 = box1.Max;
            var min2 = box2.Min;
            var max2 = box2.Max;

            var intersectionMin = new Vector3(
                MathF.Max(min1.X, min2.X),
                MathF.Max(min1.Y, min2.Y),
                MathF.Max(min1.Z, min2.Z)
            );

            var intersectionMax = new Vector3(
                MathF.Min(max1.X, max2.X),
                MathF.Min(max1.Y, max2.Y),
                MathF.Min(max1.Z, max2.Z)
            );

            if (intersectionMin.X >= intersectionMax.X ||
                intersectionMin.Y >= intersectionMax.Y ||
                intersectionMin.Z >= intersectionMax.Z)
            {
                return null;
            }

            var center = (intersectionMin + intersectionMax) / 2;
            var size = intersectionMax - intersectionMin;

            return new BoundingBox(center, MathF.Max(size.X, MathF.Max(size.Y, size.Z)));
        }
    }
}
