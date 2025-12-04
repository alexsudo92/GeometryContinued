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
            if (size.X / 2f <= _minOctantSize || size.Y / 2f <= _minOctantSize || size.Z / 2f <= _minOctantSize)
            {
                node.Children = null;
                return;
            }

            // Создаем 8 дочерних узлов
            node.Children = new OctreeNode[8];
            var childrenSizeX = size.X / 4f;
            var childrenSizeY = size.Y / 4f;
            var childrenSizeZ = size.Z / 4f;


            for (int i = 0; i < 8; i++)
            {
                var offset = new Vector3(
                    (i & 1) == 0 ? -childrenSizeX : childrenSizeX,
                    (i & 2) == 0 ? -childrenSizeY : childrenSizeY,
                    (i & 4) == 0 ? -childrenSizeZ : childrenSizeZ
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
                return node.IsSolid ? node.Bounds.Size.X * node.Bounds.Size.Y * node.Bounds.Size.Z : 0;
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

        public void Optimize()
        {
            bool changed;
            do
            {
                changed = false;
                var oldRoot = Root;
                CleanupRecursive(Root); changed = !ReferenceEquals(oldRoot, Root) || HasStructureChanged(oldRoot, Root);
            }
            while (changed);
        }

        private bool HasStructureChanged(OctreeNode oldNode, OctreeNode newNode)
        {
            if (oldNode == null && newNode == null) return false;
            if (oldNode == null || newNode == null) return true;

            if (oldNode.IsLeaf != newNode.IsLeaf) return true;
            if (oldNode.IsSolid != newNode.IsSolid) return true;

            if (!oldNode.IsLeaf)
            {
                for (int i = 0; i < 8; i++)
                {
                    if (HasStructureChanged(oldNode.Children[i], newNode.Children[i]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private OctreeNode CleanupRecursive(OctreeNode node)
        {
            // Если узел является листом, просто возвращаем его
            if (node.IsLeaf)
            {
                return node;
            }

            // Рекурсивно обрабатываем всех детей
            bool allChildrenNull = true;
            bool allChildrenSolidLeaves = true;

            for (int i = 0; i < 8; i++)
            {
                if (node.Children[i] != null)
                {
                    node.Children[i] = CleanupRecursive(node.Children[i]);

                    if (node.Children[i] != null)
                    {
                        allChildrenNull = false;

                        // Проверяем, является ли ребенок сплошным листом
                        if (!node.Children[i].IsLeaf || !node.Children[i].IsSolid)
                        {
                            allChildrenSolidLeaves = false;
                        }
                    }
                }
                else
                {
                    allChildrenSolidLeaves = false;
                }
            }

            // Случай 1: Все дети null - делаем текущий узел null
            if (allChildrenNull)
            {
                return null;
            }

            // Случай 2: Все дети являются сплошными листами - объединяем в один сплошной лист
            if (allChildrenSolidLeaves)
            {
                // Создаем новый лист на основе текущего узла
                var mergedLeaf = new OctreeNode(node.Bounds)
                {
                    IsSolid = true,
                    Children = null
                };

                return mergedLeaf;
            }

            return node;
        }
    }
}
