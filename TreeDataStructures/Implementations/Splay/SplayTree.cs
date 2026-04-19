using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Implementations.BST;

namespace TreeDataStructures.Implementations.Splay;

public class SplayTree<TKey, TValue> : BinarySearchTree<TKey, TValue>
    where TKey : IComparable<TKey>
{

    private void Splay(BstNode<TKey, TValue>? node)
    {
        while (node != null && node.Parent != null)
        {
            var parent = node.Parent;
            var grand = parent.Parent;

            if (grand == null)
            {
                if (node == parent.Left)
                    RotateRight(parent);
                else
                    RotateLeft(parent);
            } else {
                if (parent == grand.Left && node == parent.Left)
                {
                    RotateDoubleRight(grand);
                }
                else if (parent == grand.Right && node == parent.Right)
                {
                    RotateDoubleLeft(grand);
                }
                else if (parent == grand.Right && node == parent.Left)
                {
                    RotateBigLeft(grand);
                }
                else 
                {
                    RotateBigRight(grand);
                }
            }
        }
    }

    protected override BstNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);
    
    protected override void OnNodeAdded(BstNode<TKey, TValue> newNode)
    {
        Splay(newNode);
    }
    
    protected override void OnNodeRemoved(BstNode<TKey, TValue>? parent, BstNode<TKey, TValue>? child)
    {
        if (parent != null)
        {
            Splay(parent);
        }
    }
    
    public override bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        var node = FindNode(key);
        if (node != null)
        {
            Splay(node);
            value = node.Value;
            return true;
        }

        value = default;
        return false;
    }

    public override bool ContainsKey(TKey key)
    {
        var node = FindNode(key);
        if (node != null)
        {
            Splay(node); 
            return true;
        }
        return false;
    }
}
