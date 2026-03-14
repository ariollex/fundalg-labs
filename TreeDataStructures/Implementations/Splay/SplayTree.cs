using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Implementations.BST;

namespace TreeDataStructures.Implementations.Splay;

public class SplayTree<TKey, TValue> : BinarySearchTree<TKey, TValue>
    where TKey : IComparable<TKey>
{
    protected override BstNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);
    
    protected override void OnNodeAdded(BstNode<TKey, TValue> newNode)
    {
        Splay(newNode);
    }

    protected override void RemoveNode(BstNode<TKey, TValue> node)
    {
        Splay(node);
        
        var left = node.Left; // left tree
        var right = node.Right; // right tree

        left?.Parent = null; // disconnect node from tree
        right?.Parent = null;
        node.Left = null;
        node.Right = null;
        
        Root = Merge(left, right);
    }

    protected override void OnNodeRemoved(BstNode<TKey, TValue>? parent, BstNode<TKey, TValue>? child)
    {
    }
    
    
    public override bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        var node = FindNode(key);
        if (node is null) // Value not found
        {
            value = default;
            return false;
        }
        Splay(node); // Value found
        value = node.Value;
        return true;
    }

    public override bool ContainsKey(TKey key)
    {
        var node = FindNode(key);
        if (node is not null) Splay(node);
        return node != null;
    }

    private void Splay(BstNode<TKey, TValue> x)
    {
        while (x != Root)
        {
            var p = x.Parent;
            if (p == Root && x.IsRightChild) // "zig" case
            {
                RotateLeft(x);
            }
            else if (p == Root && x.IsLeftChild)
            {
                RotateRight(x);
            }
            else if (x.IsRightChild && p!.IsRightChild) // "zig-zig" case
            {
                RotateDoubleLeft(x);
            }
            else if (x.IsLeftChild && p!.IsLeftChild)
            {
                RotateDoubleRight(x);
            }
            else if (x.IsLeftChild && p!.IsRightChild) // "zig-zag" case
            {
                RotateBigLeft(x);
            }
            else if (x.IsRightChild && p!.IsLeftChild)
            {
                RotateBigRight(x);
            }
        }
    }

    private BstNode<TKey, TValue>? Merge(BstNode<TKey, TValue>? left, BstNode<TKey, TValue>? right)
    {
        if (left is null) return right;
        if (right is null) return left;
        
        Root = left;
        Splay(Maximum(left));
        
        Root!.Right = right;
        right.Parent = Root;
        
        return Root;
    }
}
