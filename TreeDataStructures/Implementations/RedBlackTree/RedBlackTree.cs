using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.RedBlackTree;

public class RedBlackTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, RbNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    protected override RbNode<TKey, TValue> CreateNode(TKey key, TValue value) => new(key, value);

    private static RbColor ColorOf(RbNode<TKey, TValue>? node) => node?.Color ?? RbColor.Black;

    private static bool IsBlack(RbNode<TKey, TValue>? node) => ColorOf(node) == RbColor.Black;

    private static bool IsRed(RbNode<TKey, TValue>? node) => ColorOf(node) == RbColor.Red;
    
    protected override void OnNodeAdded(RbNode<TKey, TValue> newNode)
    {
        while (true)
        {
            if (Root is null) return;
            if (newNode.Parent is null) // Root == newNode
            {
                Root.Color = RbColor.Black;
                return;
            }

            if (IsBlack(newNode.Parent)) return;
            // if (newNode.Parent.Color == RbColor.Red)
            var p = newNode.Parent; // parent
            var g = p.Parent; // grand
            if (g is null)
            {
                p.Color = RbColor.Black;
                return;
            }

            var u = p.IsLeftChild ? g.Right : g.Left; // uncle

            if (u is not null && IsRed(u))
            {
                p.Color = RbColor.Black;
                u.Color = RbColor.Black;
                g.Color = RbColor.Red;
                newNode = g;
                continue;
            }
            // if (u is null || u.Color == RbColor.Black)
            if (newNode.IsLeftChild && p.IsLeftChild)
            {
                RotateRight(p);
                p.Color = RbColor.Black;
                g.Color = RbColor.Red;
            }
            else if (newNode.IsRightChild && p.IsRightChild)
            {
                RotateLeft(p);
                p.Color = RbColor.Black;
                g.Color = RbColor.Red;
            }
            else if (newNode.IsRightChild && p.IsLeftChild)
            {
                RotateBigRight(newNode);
                newNode.Color = RbColor.Black;
                g.Color = RbColor.Red;
            }
            else if (newNode.IsLeftChild && p.IsRightChild)
            {
                RotateBigLeft(newNode);
                newNode.Color = RbColor.Black;
                g.Color = RbColor.Red;
            }
            break;
        }
    }

    protected override void RemoveNode(RbNode<TKey, TValue> node)
    {
        if (node.Color == RbColor.Red)
        {
            if (node.Left is null && node.Right is null) // no subtrees
            {
                Transplant(node, null);
            }
            else if ((node.Left is null) != (node.Right is null)) // 1 subtree
            {
                throw new InvalidOperationException("Invalid red-black tree!"); // Impossible situation
            }
            else // 2 subtrees
            { 
                var currNode = node.Right!;
                while (currNode.Left is not null)
                {
                    currNode = currNode.Left;
                }

                (node.Key, currNode.Key) = (currNode.Key, node.Key);
                (node.Value, currNode.Value) = (currNode.Value, node.Value);

                RemoveNode(currNode);
            }
        }
        else // if (node.Color == RbColor.Black)
        {
            if (node.Left is null && node.Right is null) // no subtrees
            {
                if (node.Parent is null)
                {
                    base.RemoveNode(node);
                    return;
                }
                
                var p = node.Parent;
                var deletedWasLeft = node.IsLeftChild;
                
                base.RemoveNode(node);

                while (p is not null)
                {
                    var b = deletedWasLeft ? p.Right : p.Left;
                    var n =  deletedWasLeft ? b?.Left : b?.Right;
                    var fn =  deletedWasLeft ? b?.Right : b?.Left;
                    
                    if (IsBlack(b)) // Case 1
                    {
                        if (IsRed(fn)) // case 1.1a
                        {
                            b!.Color = p.Color;
                            p.Color = RbColor.Black;
                            fn!.Color = RbColor.Black;
                            
                            if (deletedWasLeft)
                            {
                                RotateLeft(b);
                            }
                            else
                            {
                                RotateRight(b);
                            }

                            return;
                        }
                        
                        if (IsRed(n) && IsBlack(fn)) // case 1.1b
                        {
                            n!.Color = RbColor.Black;
                            b!.Color = RbColor.Red;
                            
                            if (deletedWasLeft)
                            {
                                RotateRight(n);
                            }
                            else
                            {
                                RotateLeft(n);
                            }
                            // Not its case 1.1a
                            continue;
                        }
                        
                        if (IsBlack(n) && IsBlack(fn)) // case 1.2
                        {
                            b?.Color = RbColor.Red;
                            if (IsRed(p))
                            {
                                p.Color = RbColor.Black;
                                return;
                            }

                            if (p.Parent is null) return;
                            
                            deletedWasLeft = p.IsLeftChild;
                            p = p.Parent;
                        }
                    }
                    else // if (IsRed(b)) (Case 2)
                    {
                        if (deletedWasLeft)
                        {
                            RotateLeft(b!);
                        }
                        else
                        {
                            RotateRight(b!);
                        }

                        p.Color = RbColor.Red;
                        b!.Color = RbColor.Black;
                    }
                }
            }
            else if ((node.Left is null) != (node.Right is null)) // 1 subtree
            {
                var currNode = node.Left ?? node.Right;

                (node.Key, currNode!.Key) = (currNode.Key, node.Key);
                (node.Value, currNode.Value) = (currNode.Value, node.Value);

                RemoveNode(currNode);
            }
            else
            {
                var currNode = node.Right!;
                while (currNode.Left is not null)
                {
                    currNode = currNode.Left;
                }
                
                (node.Key, currNode.Key) = (currNode.Key, node.Key);
                (node.Value, currNode.Value) = (currNode.Value, node.Value);

                RemoveNode(currNode);
            }
        }
    }
}
