using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.AVL;

public class AvlTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, AvlNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{

    private int GetHeight(AvlNode<TKey, TValue>? node)
    {
        return node?.Height ?? 0;
    }

    private int GetBalanceFactor(AvlNode<TKey, TValue>? node)
    {
        if (node == null) return 0;
        return GetHeight(node.Right) - GetHeight(node.Left);
    }

    private void UpdateNodeHeight(AvlNode<TKey, TValue> node)
    {
        node.Height = 1 + Math.Max(GetHeight(node.Left), GetHeight(node.Right));
    }

    private void Balance(AvlNode<TKey, TValue> node) {
        UpdateNodeHeight(node);

        int balance = GetBalanceFactor(node);
        if (balance > 1) 
        {
            if (GetBalanceFactor(node.Right) < 0)
                RotateBigLeft(node);
            else
                RotateLeft(node);

            var sub = node.Parent ?? Root;
            if (sub != null)
            {
                UpdateNodeHeight(node);
                UpdateNodeHeight(sub);
            }
        }
        else if (balance < -1)
        {
            if (GetBalanceFactor(node.Left) > 0)
                RotateBigRight(node);
            else
                RotateRight(node);

            var sub = node.Parent ?? Root;
            if (sub != null)
            {
                UpdateNodeHeight(node);
                UpdateNodeHeight(sub);
            }
        }
    }

    protected override AvlNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);
    
    protected override void OnNodeAdded(AvlNode<TKey, TValue> newNode)
    {
        var current = newNode.Parent; 

        while (current != null)
        {
            Balance(current);
            current = current.Parent;
        }
    }

    protected override void OnNodeRemoved(AvlNode<TKey, TValue>? parent, AvlNode<TKey, TValue>? child)
    {
        var current = parent; 

        while (current != null)
        {
            Balance(current);
            current = current.Parent;
        }
    }
}