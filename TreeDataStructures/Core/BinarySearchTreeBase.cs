using System.Collections;
using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Interfaces;

namespace TreeDataStructures.Core;

public abstract class BinarySearchTreeBase<TKey, TValue, TNode>(IComparer<TKey>? comparer = null) 
    : ITree<TKey, TValue>
    where TNode : Node<TKey, TValue, TNode>
{
    protected TNode? Root;
    public IComparer<TKey> Comparer { get; protected set; } = comparer ?? Comparer<TKey>.Default; // use it to compare Keys

    public int Count { get; protected set; }
    
    public bool IsReadOnly => false;

    public ICollection<TKey> Keys => InOrder().Select(x => x.Key).ToList();
    public ICollection<TValue> Values => InOrder().Select(x => x.Value).ToList();
    
    
    public virtual void Add(TKey key, TValue value)
    {
        TNode? parent = null;
        var currNode = Root;
        while (currNode is not null)
        {
            parent = currNode; // currNode can be null, parent saves value
            var cmp = Comparer.Compare(key, currNode.Key);
            switch (cmp)
            {
                case < 0:
                    currNode = currNode.Left;
                    break;
                case > 0:
                    currNode = currNode.Right;
                    break;
                default:
                    currNode.Value = value;
                    return;
            }
        }

        var newNode = CreateNode(key, value);
        if (parent is null)
        {
            Root = newNode;
        }
        else if (Comparer.Compare(key, parent.Key) < 0)
        {
            parent.Left = newNode;
            newNode.Parent = parent;
        }
        else
        {
            parent.Right = newNode;
            newNode.Parent = parent;
        }
        ++this.Count;
        OnNodeAdded(newNode);
    }

    
    public virtual bool Remove(TKey key)
    {
        TNode? node = FindNode(key);
        if (node == null) { return false; }

        RemoveNode(node);
        this.Count--;
        return true;
    }
    
    
    protected virtual void RemoveNode(TNode node)
    {
        if ((node.Right is null) && (node.Left is null)) { // No children
            Transplant(node, null);
            OnNodeRemoved(node.Parent, null);
        } else if ((node.Right is null) != (node.Left is null)) { // Our node has only one child
            Transplant(node, node.Left ?? node.Right); // returns left if not null, else returns right
            OnNodeRemoved(node, node.Left ?? node.Right);
        } else
        {
            TNode currNode = node.Right!;
            // Inorder successor
            while (currNode.Left is not null)
            {
                currNode = currNode.Left;
            }

            if (currNode.Parent != node) // Successor is not node.Right
            {
                // currNode.Right may be exits
                Transplant(currNode, currNode.Right); // currNode no more in tree
                currNode.Right = node.Right!; // save links to right child
                currNode.Right.Parent = currNode;
            }

            Transplant(node, currNode);
            currNode.Left = node.Left!;
            currNode.Left.Parent = currNode;
            OnNodeRemoved(currNode, node);
        }
    }

    public virtual bool ContainsKey(TKey key) => FindNode(key) != null;
    
    public virtual bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        TNode? node = FindNode(key);
        if (node != null)
        {
            value = node.Value;
            return true;
        }
        value = default;
        return false;
    }

    public TValue this[TKey key]
    {
        get => TryGetValue(key, out TValue? val) ? val : throw new KeyNotFoundException();
        set => Add(key, value);
    }

    
    #region Hooks
    
    /// <summary>
    /// Вызывается после успешной вставки
    /// </summary>
    /// <param name="newNode">Узел, который встал на место</param>
    protected virtual void OnNodeAdded(TNode newNode) { }
    
    /// <summary>
    /// Вызывается после удаления. 
    /// </summary>
    /// <param name="parent">Узел, чей ребенок изменился</param>
    /// <param name="child">Узел, который встал на место удаленного</param>
    protected virtual void OnNodeRemoved(TNode? parent, TNode? child) { }
    
    #endregion
    
    
    #region Helpers
    protected abstract TNode CreateNode(TKey key, TValue value);
    
    protected TNode? FindNode(TKey key)
    {
        TNode? current = Root;
        while (current != null)
        {
            int cmp = Comparer.Compare(key, current.Key);
            if (cmp == 0) { return current; }
            current = cmp < 0 ? current.Left : current.Right;
        }
        return null;
    }

    protected void RotateLeft(TNode x)
    {
        if (x.Parent is null || !x.IsRightChild) return;

        TNode? tmp = x.Left;
        TNode? pp = x.Parent.Parent;
        bool pWasLeftChild = x.Parent.IsLeftChild;

        // Rotate
        x.Left = x.Parent;
        x.Parent.Parent = x; // Link to y.Parent.Parent is lost
        x.Parent.Right = tmp;
        tmp?.Parent = x.Left;  // if (tmp is not null)

        x.Parent = pp;

        if (pp is null)
        {
            Root = x;
        } 
        else if (pWasLeftChild)
        {
            pp.Left = x;
        } 
        else
        {
            pp.Right = x;
        }
    }

    protected void RotateRight(TNode y)
    {
        if (y.Parent is null || !y.IsLeftChild) return;

        TNode? tmp = y.Right;
        TNode? pp = y.Parent.Parent;
        bool pWasLeftChild = y.Parent.IsLeftChild;

        y.Right = y.Parent;
        y.Parent.Parent = y;
        y.Parent.Left = tmp;
        tmp?.Parent = y.Right;
    
        y.Parent = pp;

        if (pp is null)
        {
            Root = y;
        } 
        else if (pWasLeftChild)
        {
            pp.Left = y;
        } 
        else
        {
            pp.Right = y;
        }
    }
    
    protected void RotateBigLeft(TNode x)
    {
        if (x.Parent is null || x.Parent.Parent is null) return;
        if (!x.IsLeftChild || !x.Parent.IsRightChild) return;

        RotateRight(x);
        RotateLeft(x);
    }
    
    protected void RotateBigRight(TNode y)
    {
        if (y.Parent is null || y.Parent.Parent is null) return;
        if (!y.IsRightChild || !y.Parent.IsLeftChild) return;

        RotateLeft(y);
        RotateRight(y);
    }
    
    protected void RotateDoubleLeft(TNode x)
    {
        if (x.Parent?.Parent is null) return;
        if (!x.IsRightChild || !x.Parent.IsRightChild) return;

        RotateLeft(x);
        RotateLeft(x);
    }
    
    protected void RotateDoubleRight(TNode y)
    {
        if (y.Parent?.Parent is null) return;
        if (!y.IsLeftChild || !y.Parent.IsLeftChild) return;

        RotateRight(y);
        RotateRight(y);
    }
    
    protected void Transplant(TNode u, TNode? v)
    {
        if (u.Parent == null)
        {
            Root = v;
        }
        else if (u.IsLeftChild)
        {
            u.Parent.Left = v;
        }
        else
        {
            u.Parent.Right = v;
        }
        v?.Parent = u.Parent;
    }
    #endregion
    
    public IEnumerable<TreeEntry<TKey, TValue>>  InOrder() => new TreeIterator(Root, TraversalStrategy.InOrder);
    public IEnumerable<TreeEntry<TKey, TValue>>  PreOrder() => new TreeIterator(Root, TraversalStrategy.PreOrder);
    public IEnumerable<TreeEntry<TKey, TValue>>  PostOrder() => new TreeIterator(Root, TraversalStrategy.PostOrder);
    public IEnumerable<TreeEntry<TKey, TValue>>  InOrderReverse() => new TreeIterator(Root, TraversalStrategy.InOrderReverse);
    public IEnumerable<TreeEntry<TKey, TValue>>  PreOrderReverse() => new TreeIterator(Root, TraversalStrategy.PreOrderReverse);
    public IEnumerable<TreeEntry<TKey, TValue>>  PostOrderReverse() => new TreeIterator(Root, TraversalStrategy.PostOrderReverse);
    
    /// <summary>
    /// Внутренний класс-итератор. 
    /// Реализует паттерн Iterator вручную, без yield return (ban).
    /// </summary>
    private struct TreeIterator :
        IEnumerable<TreeEntry<TKey, TValue>>,
        IEnumerator<TreeEntry<TKey, TValue>>
    {
        private readonly TNode? _root;
        private readonly TraversalStrategy _strategy;
        private TNode? _current;
        private TNode? _previous;
        private int _depth;

        public TreeIterator(TNode? root, TraversalStrategy strategy)
        {
            _root = root;
            _strategy = strategy;
            _current = _root;
            _previous = null;
            _depth = 0;
        }

        public IEnumerator<TreeEntry<TKey, TValue>> GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => this;
        
        public TreeEntry<TKey, TValue> Current => _current is null ? 
            throw new InvalidOperationException() : new TreeEntry<TKey, TValue>(_current.Key, _current.Value, _depth);
        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (_current is null) return false; // Tree without any nodes
            switch (_strategy)
            {
                case TraversalStrategy.InOrder:
                {
                    if (_previous is null)
                    {
                        _previous = _current;
                        while (SwitchToLeft()) { }
                        return true; // Return first (leftest) node
                    }

                    if (_current.Right is not null)
                    {
                        SwitchToRight(); 
                        while (SwitchToLeft()) { }
                        return true; // return leftest right node;
                    }

                    while (SwitchToParent())
                    {
                        if (_current.Left == _previous)
                        {
                            return true; // Returned from left node;
                        }
                    } 
                    return false; // Root case
                }
                case TraversalStrategy.PreOrder:
                {
                    if (_previous is null)
                    {
                        _previous = _current;
                        return true; // return Root node
                    }

                    if (_previous == _current)
                    {
                        if (SwitchToLeft()) return true;
                        if (SwitchToRight()) return true;
                    }
                    if (_previous == _current.Parent) // came from parent
                    {
                        if (SwitchToLeft()) return true;
                        if (SwitchToRight()) return true;
                    }
                    else if (_previous == _current.Left) // "returned" from left subtree
                    {
                        if (SwitchToRight()) return true;
                    }

                    while (_current.Parent is not null) // "returned" from right subtree,
                        // or current is a leaf (2 false in first if) (need to up)
                    {
                        if (!SwitchToParent()) break; // Parent doesnt exists -> end.
                        if (_previous == _current.Left && SwitchToRight())
                        {
                            return true;
                        }
                    }

                    return false;
                }
                case TraversalStrategy.PostOrder:
                {
                    if (_previous is null)
                    {
                        while (SwitchToLeft() || SwitchToRight()) { }
                        return true; // Return first (downest) node
                    }
                    
                    if (!SwitchToParent()) return false;

                    if (_previous == _current.Left && _current.Right is not null) //if we can go to the right after left
                    {
                        SwitchToRight();
                        while (SwitchToLeft() || SwitchToRight()) { } // return downest left->right node
                    }
                    
                    return true;
                }
                case TraversalStrategy.InOrderReverse:
                {
                    if (_previous is null)
                    {
                        _previous = _current;
                        while (SwitchToRight()) { }
                        return true; // Return first (leftest) node
                    }

                    if (_current.Left is not null)
                    {
                        SwitchToLeft(); 
                        while (SwitchToRight()) { }
                        return true; // return leftest right node;
                    }

                    while (SwitchToParent())
                    {
                        if (_current.Right == _previous)
                        {
                            return true; // Returned from left node;
                        }
                    } 
                    return false; // Root case
                }
                case TraversalStrategy.PreOrderReverse:
                {
                    if (_previous is null)
                    {
                        while (SwitchToRight() || SwitchToLeft()) { }
                        return true; // Return first (downest) node
                    }
                    
                    if (!SwitchToParent()) return false;

                    if (_previous == _current.Right && _current.Left is not null) //if we can go to the left after right
                    {
                        SwitchToLeft();
                        while (SwitchToRight() || SwitchToLeft()) { } // return downest right->left node
                    }
                    
                    return true;
                }
                case TraversalStrategy.PostOrderReverse:
                {
                    if (_previous is null)
                    {
                        _previous = _current;
                        return true; // return Root node
                    }
                    
                    if (_previous == _current)
                    {
                        if (SwitchToRight()) return true;
                        if (SwitchToLeft()) return true;
                    }
                    if (_previous == _current.Parent) // came from parent
                    {
                        if (SwitchToRight()) return true;
                        if (SwitchToLeft()) return true;
                    }
                    else if (_previous == _current.Right) // "returned" from right subtree
                    {
                        if (SwitchToLeft()) return true;
                    }

                    while (_current.Parent is not null) // "returned" from left subtree,
                        // or current is a leaf (2 false in first if) (need to up)
                    {
                        if (!SwitchToParent()) break; // Parent doesnt exists -> end.
                        if (_previous == _current.Right && SwitchToLeft())
                        {
                            return true;
                        }
                    }

                    return false;
                }
                default: throw new ArgumentException("Unknown traversal strategy");
            }
        }

        private bool SwitchToLeft()
        {
            if (_current?.Left is null) return false;
            _previous = _current;
            _current = _current.Left;
            ++_depth;
            return true;
        }
        private bool SwitchToRight()
        {
            if (_current?.Right is null) return false;
            _previous = _current;
            _current = _current.Right;
            ++_depth;
            return true;
        }
        private bool SwitchToParent()
        {
            if (_current?.Parent is null) return false;
            _previous = _current;
            _current = _current.Parent;
            --_depth;
            return true;
        }
        
        public void Reset()
        {
            _current = _root;
            _previous = null;
            _depth = 0;
        }
        
        public void Dispose()
        {
        }
    }
    
    
    private enum TraversalStrategy { InOrder, PreOrder, PostOrder, InOrderReverse, PreOrderReverse, PostOrderReverse }
    
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return InOrder().Select(x => new KeyValuePair<TKey, TValue>(x.Key, x.Value)).GetEnumerator();
    }
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    

    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
    public void Clear() { Root = null; Count = 0; }
    public bool Contains(KeyValuePair<TKey, TValue> item) => ContainsKey(item.Key);

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        if (array is null) throw new ArgumentNullException(nameof(array));
        if (arrayIndex < 0) throw new ArgumentOutOfRangeException(nameof(arrayIndex));
        if (arrayIndex > array.Length) throw new ArgumentOutOfRangeException(nameof(arrayIndex));
        if (Count - array.Length < 0) throw new InvalidOperationException("The collection has not enough space.");
        foreach (var item in this)
        {
            array[arrayIndex++] = item;
        }
    }

    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);
    
    protected TNode Maximum(TNode node)
    {
        if (node is null) throw new NullReferenceException("Tree is empty"); 
        while (node.Right is not null)
        {
            node = node.Right;
        }

        return node;
    }

    protected TNode Minimum(TNode node)
    {
        if (node is null) throw new NullReferenceException("Tree is empty"); 
        while (node.Left is not null)
        {
            node = node.Left;
        }

        return node;
    }
}
