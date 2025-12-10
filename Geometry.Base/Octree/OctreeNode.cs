namespace Geometry.Base.Octree
{
    public class OctreeNode(BoundingBox bounds)
    {
        public BoundingBox Bounds { get; set; } = bounds;
        public OctreeNode[]? Children { get; set; } = null;
        public bool IsLeaf => Children == null;
        public bool IsSolid { get; set; } = false;
        public bool IsRendered { get; set; }
    }
}
