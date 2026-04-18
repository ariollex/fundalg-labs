using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.AVL;

public class AvlTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, AvlNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    private static int Height(AvlNode<TKey, TValue>? node) => node?.Height ?? 0;
    
    private static void RecalcHeight(AvlNode<TKey, TValue> node)
    {
        node.Height = 1 + Math.Max(Height(node.Left), Height(node.Right));
    }
    
    protected override AvlNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);

    protected override void OnNodeAdded(AvlNode<TKey, TValue> newNode)
    {
        var node = newNode;

        while (node.Parent != null) // node != Root
        {
            node = node.Parent;
            RecalcHeight(node);
            
            var bfP = Bf(node);
            var bfL = Bf(node.Left);
            var bfR = Bf(node.Right);
            
            if (bfP == 0) return;
            if (bfP is 1 or -1) continue;
            
            if (bfP == -2 && bfL <= 0) RotateRight(node.Left!);
            if (bfP == -2 && bfL > 0) RotateBigRight(node.Left!.Right!);
            if (bfP == 2 && bfR >= 0) RotateLeft(node.Right!);
            if (bfP == 2 && bfR < 0) RotateBigLeft(node.Right!.Left!);
            return;
        }
    }

    protected override void OnNodeRemoved(AvlNode<TKey, TValue>? parent, AvlNode<TKey, TValue>? child)
    {
        var node = parent;

        while (node != null) // node != Root
        {
            RecalcHeight(node);

            var bfP = Bf(node);
            var bfL = Bf(node.Left);
            var bfR = Bf(node.Right);
            
            if (bfP == -2 && bfL <= 0)
            {
                node = node.Left;
                RotateRight(node!);
            }
            else if (bfP == -2 && bfL > 0)
            {
                node = node.Left!.Right;
                RotateBigRight(node!);
            }
            else if (bfP == 2 && bfR >= 0)
            {
                node = node.Right;
                RotateLeft(node!);
            }
            else if (bfP == 2 && bfR < 0)
            {
                node = node.Right!.Left;
                RotateBigLeft(node!);
            }

            node = node?.Parent;
        }
    }
    
    protected override void RotateLeft(AvlNode<TKey, TValue> x)
    {
        base.RotateLeft(x);
        RecalcHeight(x.Left!);
        RecalcHeight(x);
    }
    
    protected override void RotateRight(AvlNode<TKey, TValue> x)
    {
        base.RotateRight(x);
        RecalcHeight(x.Right!);
        RecalcHeight(x);
    }

    private static int Bf(AvlNode<TKey, TValue>? node)
    {
        return Height(node?.Right) - Height(node?.Left);
    }
}
