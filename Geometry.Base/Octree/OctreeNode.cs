namespace Geometry.Base.Octree
{
    // Узел Octree
    public class OctreeNode
    {
        public BoundingBox Bounds { get; set; }
        public OctreeNode[] Children { get; set; }
        public bool IsLeaf => Children == null;
        public bool IsSolid { get; set; }

        public OctreeNode(BoundingBox bounds)
        {
            Bounds = bounds;
            Children = null;
            IsSolid = false;
        }
    }
}
