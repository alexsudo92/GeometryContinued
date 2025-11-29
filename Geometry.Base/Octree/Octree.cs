using System.Numerics;

namespace Geometry.Base.Octree
{
    // Основной класс Octree
    public class Octree
    {
        public OctreeNode Root { get; private set; }
        public BoundingBox Bounds => Root.Bounds;
        private readonly float _minOctantSize;

        public Octree(BoundingBox bounds, float minOctantSize)
        {
            _minOctantSize = minOctantSize;
            Root = new OctreeNode(bounds);
        }

        public void BuildTree(OctreeNode node)
        {
            var size = node.Bounds.Size;

            // Если размер октанта меньше минимального, делаем листом
            if (size <= _minOctantSize)
            {
                node.Children = null;
                return;
            }

            // Создаем 8 дочерних узлов
            node.Children = new OctreeNode[8];
            var childrenSize = size / 4f;

            for (int i = 0; i < 8; i++)
            {
                var offset = new Vector3(
                    (i & 1) == 0 ? -childrenSize : childrenSize,
                    (i & 2) == 0 ? -childrenSize : childrenSize,
                    (i & 4) == 0 ? -childrenSize : childrenSize
                );

                var childBoxSize = size / 2f;
                var parentCenter = node.Bounds.Center;
                var childCenter = parentCenter + offset;

                var childBounds = new BoundingBox(childCenter, childBoxSize);
                node.Children[i] = new OctreeNode(childBounds);

                BuildTree(node.Children[i]);
            }
        }

        // Проверка принадлежности точки объекту
        public bool ContainsPoint(Vector3 point)
        {
            if (!Bounds.ContainsPoint(point))
                return false;

            return ContainsPointRecursive(Root, point);
        }

        private bool ContainsPointRecursive(OctreeNode node, Vector3 point)
        {
            if (!node.Bounds.ContainsPoint(point))
                return false;

            if (node.IsLeaf)
                return node.IsSolid;

            foreach (var child in node.Children)
            {
                if (child.Bounds.ContainsPoint(point))
                    return ContainsPointRecursive(child, point);
            }

            return false;
        }

        // Вычисление объема
        public float GetVolume()
        {
            return CalculateVolumeRecursive(Root);
        }

        private float CalculateVolumeRecursive(OctreeNode node)
        {
            if (node.IsLeaf)
            {
                return node.IsSolid ? node.Bounds.Size * node.Bounds.Size * node.Bounds.Size : 0;
            }

            float volume = 0;
            foreach (var child in node.Children)
            {
                volume += CalculateVolumeRecursive(child);
            }
            return volume;
        }

        // Установка объекта (для инициализации)
        public void SetObject(Func<Vector3, bool> objectFunction)
        {
            SetObjectRecursive(Root, objectFunction);
        }

        private void SetObjectRecursive(OctreeNode node, Func<Vector3, bool> objectFunction)
        {
            if (node.IsLeaf)
            {
                node.IsSolid = objectFunction(node.Bounds.Center);
                return;
            }

            foreach (var child in node.Children)
            {
                SetObjectRecursive(child, objectFunction);
            }
        }
    }
}
