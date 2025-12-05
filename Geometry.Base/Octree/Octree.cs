using System.Numerics;

namespace Geometry.Base.Octree
{
    public class Octree(BoundingBox bounds, float minOctantSize)
    {
        public OctreeNode Root { get; private set; } = new OctreeNode(bounds);
        public BoundingBox Bounds => Root.Bounds;

        public void SetObject(Func<Vector3, bool> objectFunction)
        {
            Root = BuildTree(Root, objectFunction);
        }

        public OctreeNode BuildTree(OctreeNode node, Func<Vector3, bool> objectFunction)
        {
            var size = node.Bounds.Size;

            bool shouldBeLeaf = size.X / 2f <= minOctantSize ||
                               size.Y / 2f <= minOctantSize ||
                               size.Z / 2f <= minOctantSize;

            if (shouldBeLeaf)
            {
                node.IsSolid = objectFunction(node.Bounds.Center);
                return node;
            }

            node.Children = new OctreeNode[8];
            var childrenSizeX = size.X / 4f;
            var childrenSizeY = size.Y / 4f;
            var childrenSizeZ = size.Z / 4f;
            var center = node.Bounds.Center;

            for (int i = 0; i < 8; i++)
            {
                var offset = new Vector3(
                    (i & 1) == 0 ? -childrenSizeX : childrenSizeX,
                    (i & 2) == 0 ? -childrenSizeY : childrenSizeY,
                    (i & 4) == 0 ? -childrenSizeZ : childrenSizeZ
                );

                var childBoxSize = size / 2f;
                var childCenter = center + offset;

                var childBounds = new BoundingBox(childCenter, childBoxSize);
                var childNode = new OctreeNode(childBounds);

                node.Children[i] = BuildTree(childNode, objectFunction);
            }

            var allChildrenAreLeaves = true;
            var firstChildSolidState = false;
            var allChildrenSame = true;

            foreach (var child in node.Children)
            {
                if (!child.IsLeaf)
                {
                    allChildrenAreLeaves = false;
                    break;
                }

                if (!firstChildSolidState)
                {
                    firstChildSolidState = child.IsSolid;
                }
                else if (child.IsSolid != firstChildSolidState)
                {
                    allChildrenSame = false;
                    break;
                }
            }

            if (allChildrenAreLeaves && allChildrenSame && firstChildSolidState)
            {
                node.Children = null;
                node.IsSolid = firstChildSolidState;
            }
            else
            {
                node.IsSolid = false;
            }

            return node;
        }

        public bool ContainsPoint(Vector3 point)
        {
            if (!Bounds.ContainsPoint(point))
                return false;

            return ContainsPointRecursive(Root, point);
        }

        public float GetVolume()
        {
            return CalculateVolumeRecursive(Root);
        }

        private bool ContainsPointRecursive(OctreeNode node, Vector3 point)
        {
            if (!node.Bounds.ContainsPoint(point))
                return false;

            if (node.IsLeaf)
                return node.IsSolid;

            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    if (child.Bounds.ContainsPoint(point))
                        return ContainsPointRecursive(child, point);
                }
            }

            return false;
        }

        private float CalculateVolumeRecursive(OctreeNode node)
        {
            if (node.IsLeaf)
            {
                return node.IsSolid ? node.Bounds.Size.X * node.Bounds.Size.Y * node.Bounds.Size.Z : 0;
            }

            var volume = 0f;
            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    volume += CalculateVolumeRecursive(child);
                }
            }

            return volume;
        }
    }
}
