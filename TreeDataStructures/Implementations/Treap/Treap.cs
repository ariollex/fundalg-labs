using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.Treap;

public class Treap<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, TreapNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    /// <summary>
    /// Разрезает дерево с корнем <paramref name="root"/> на два поддерева:
    /// Left: все ключи <= <paramref name="key"/>
    /// Right: все ключи > <paramref name="key"/>
    /// </summary>
    protected virtual (TreapNode<TKey, TValue>? Left, TreapNode<TKey, TValue>? Right) Split(TreapNode<TKey, TValue>? root, TKey key)
    {
        if (root is null) return (null, null);

        if (Comparer.Compare(key, root.Key) >= 0)
        {
            var (left, right) = Split(root.Right, key);
            root.Right = left;
            
            left?.Parent = root;
            root.Parent = null;
            right?.Parent = null;
            return (root, right);
        }
        else
        {
            var (left, right) = Split(root.Left, key);
            root.Left = right;

            right?.Parent = root;
            left?.Parent = null;
            root.Parent = null;
            return (left, root);
        }
    }

    /// <summary>
    /// Сливает два дерева в одно.
    /// Важное условие: все ключи в <paramref name="left"/> должны быть меньше ключей в <paramref name="right"/>.
    /// Слияние происходит на основе Priority (куча).
    /// </summary>
    protected virtual TreapNode<TKey, TValue>? Merge(TreapNode<TKey, TValue>? left, TreapNode<TKey, TValue>? right)
    {
        if (left is null) return right;
        if (right is null) return left;

        if (left.Priority > right.Priority)
        {
            left.Right = Merge(left.Right, right);
            
            left.Right?.Parent = left;
            left.Parent = null;
            return left;
        }
        else
        {
            right.Left = Merge(left, right.Left);
            
            right.Left?.Parent = right;
            right.Parent = null;
            return right;
        }
    }
    

    public override void Add(TKey key, TValue value)
    {
        var existing = FindNode(key); // Duplicates (key already in tree)
        if (existing is not null)
        {
            existing.Value = value;
            return;
        }
        
        var newNode = CreateNode(key, value);
        
        var (left, right) = Split(Root, key);
        Root = Merge(Merge(left, newNode), right);
        ++Count;
    }
    
    public override bool Remove(TKey key)
    {
        var (left, right) = Split(Root, key);
        if (left is null)
        {
            Root = right;
            return false;
        }

        var maxNode = Maximum(left);

        if (Comparer.Compare(maxNode.Key, key) != 0) // otherwise, maxNode.key can be only less than key, so key not in tree
        {
            Root = Merge(left, right);
            return false;
        }

        Root = left;
        RemoveNode(maxNode); // Can change Root (left/right subtree?), but we need to change only Root in left subtree
        left = Root;
        
        Root = Merge(left, right);
        --Count;
        
        return true;
    }

    protected override TreapNode<TKey, TValue> CreateNode(TKey key, TValue value) => new(key, value);
    
    protected override void OnNodeAdded(TreapNode<TKey, TValue> newNode)
    {
    }
    
    protected override void OnNodeRemoved(TreapNode<TKey, TValue>? parent, TreapNode<TKey, TValue>? child)
    {
    }
}