using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.RedBlackTree;

public class RedBlackTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, RbNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    protected override RbNode<TKey, TValue> CreateNode(TKey key, TValue value)
    {
        return new RbNode<TKey, TValue>(key, value);
    }

    private bool IsRed(RbNode<TKey, TValue>? node)
    {
        return node != null && node.Color == RbColor.Red;
    }

    protected override void RemoveNode(RbNode<TKey, TValue> node)
    {
        RbNode<TKey, TValue>? x = null;
        RbNode<TKey, TValue>? x_parent = null;

        RbNode<TKey, TValue> y = node;
        RbColor y_orig_col = y.Color;

        if (node.Left == null)
        {
            x = node.Right; 
            x_parent = node.Parent;
            Transplant(node, node.Right);
        }
        else if (node.Right == null)
        {
            x = node.Left;
            x_parent = node.Parent;
            Transplant(node, node.Left);
        } else {
            y = node.Right;
            while (y.Left != null)
            {
                y = y.Left;
            }

            y_orig_col = y.Color;
            x = y.Right;

            if (y.Parent == node)
            {
                x_parent = y;
            } else {
                x_parent = y.Parent;
                Transplant(y, y.Right);
                y.Right = node.Right;

                if (y.Right != null) y.Right.Parent = y;
            }

            Transplant(node, y);
            y.Left = node.Left;
            if (y.Left != null) y.Left.Parent = y;
            
            y.Color = node.Color;
        }

        if (y_orig_col == RbColor.Black)
        {
            OnNodeRemoved(x_parent, x);
        }
    }
    
    protected override void OnNodeAdded(RbNode<TKey, TValue> newNode)
    {
        while (newNode.Parent != null && IsRed(newNode.Parent))
        {
            var dad = newNode.Parent;
            var grand = dad.Parent;

            if (grand == null) break; 

            if (dad == grand.Left)
            {
                var uncle = grand.Right;

                if (uncle != null && IsRed(uncle))
                {
                    dad.Color = RbColor.Black;
                    uncle.Color = RbColor.Black;
                    grand.Color = RbColor.Red;

                    newNode = grand;
                } else {
                    if (newNode == dad.Right)
                    {
                        newNode = dad;
                        RotateLeft(newNode);
                        dad = newNode.Parent;
                    }

                    dad.Color = RbColor.Black;
                    grand.Color = RbColor.Red;
                    RotateRight(grand);
                }
            } else {
                var uncle = grand.Left;

                if (uncle != null && IsRed(uncle))
                {
                    dad.Color = RbColor.Black;
                    uncle.Color = RbColor.Black;
                    grand.Color = RbColor.Red;

                    newNode = grand;
                } else {
                    if (newNode == dad.Left)
                    {
                        newNode = dad;
                        RotateRight(newNode);
                        dad = newNode.Parent;
                    }

                    dad.Color = RbColor.Black;
                    grand.Color = RbColor.Red;
                    RotateLeft(grand);
                }
            }
        }

        if (Root != null) { 
            Root.Color = RbColor.Black;
        }
    }

    protected override void OnNodeRemoved(RbNode<TKey, TValue>? parent, RbNode<TKey, TValue>? child)
    {
        var x = child;
        var x_parent = parent;

        while (x != Root && !IsRed(x))
        {
            if (x_parent == null) break;

            if (x == x_parent.Left)
            {
                var brother = x_parent.Right;

                if (IsRed(brother))
                {
                    if (brother != null) brother.Color = RbColor.Black;
                    x_parent.Color = RbColor.Red;
                    RotateLeft(x_parent);
                    brother = x_parent.Right;
                }

                if (brother == null) break;

                if (!IsRed(brother.Left) && !IsRed(brother.Right))
                {
                    brother.Color = RbColor.Red;
                    x = x_parent;
                    x_parent = x.Parent;
                } else {
                    if (!IsRed(brother.Right))
                    {
                        if (brother.Left != null) brother.Left.Color = RbColor.Black;
                        brother.Color = RbColor.Red;
                        RotateRight(brother);
                        brother = x_parent.Right;
                    }

                    if (brother != null)
                    {
                        brother.Color = x_parent.Color;
                        x_parent.Color = RbColor.Black;
                        if (brother.Right != null) brother.Right.Color = RbColor.Black;
                        RotateLeft(x_parent);
                    }
                    x = Root;
                }
            } else {
                var brother = x_parent.Left;

                if (IsRed(brother))
                {
                    if (brother != null) brother.Color = RbColor.Black;
                    x_parent.Color = RbColor.Red;
                    RotateRight(x_parent);
                    brother = x_parent.Left;
                }

                if (brother == null) break;

                if (!IsRed(brother.Right) && !IsRed(brother.Left))
                {
                    brother.Color = RbColor.Red;
                    x = x_parent;
                    x_parent = x.Parent;
                } else {
                    if (!IsRed(brother.Left))
                    {
                        if (brother.Right != null) brother.Right.Color = RbColor.Black;
                        brother.Color = RbColor.Red;
                        RotateLeft(brother);
                        brother = x_parent.Left;
                    }

                    if (brother != null)
                    {
                        brother.Color = x_parent.Color;
                        x_parent.Color = RbColor.Black;
                        if (brother.Left != null) brother.Left.Color = RbColor.Black;
                        RotateRight(x_parent);
                    }
                    
                    x = Root;
                }
            }
        }

        if (x != null)
        {
            x.Color = RbColor.Black;
        }
    }

}