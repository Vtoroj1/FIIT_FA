using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.Treap;

public class Treap<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, TreapNode<TKey, TValue>>
{
    /// <summary>
    /// Разрезает дерево с корнем <paramref name="root"/> на два поддерева:
    /// Left: все ключи <= <paramref name="key"/>
    /// Right: все ключи > <paramref name="key"/>
    /// </summary>
    protected virtual (TreapNode<TKey, TValue>? Left, TreapNode<TKey, TValue>? Right) Split(TreapNode<TKey, TValue>? root, TKey key)
    {
        if (root == null) return (null, null);

        if (Comparer.Compare(root.Key, key) <= 0)
        {
            var (rLeft, right) = Split(root.Right, key);

            root.Right = rLeft;
            rLeft?.Parent = root;
            root.Parent = null;
            return (root, right);
        } else {
            var (left, lRight) = Split(root.Left, key);

            root.Left = lRight;
            lRight?.Parent = root;
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
        if (left == null)
        {
            right?.Parent = null;
            return right;
        }

        if (right == null)
        {
            left.Parent = null;
            return left;
        }

        if (left.Priority > right.Priority)
        {
            left.Right = Merge(left.Right, right);
            left.Right?.Parent = left;
            left.Parent = null;
            return left;
        } else {
            right.Left = Merge(left, right.Left);
            right.Left?.Parent = right;
            right.Parent = null;
            return right;
        }
    }

    public override void Add(TKey key, TValue value)
    {
        if (key == null) throw new Exception("NULL");
        var existing = FindNode(key);
        if (existing != null)
        {
            existing.Value = value;
            return;
        }

        var newNode = CreateNode(key, value);
        var (left, right) = Split(Root, key);
        Root = Merge(Merge(left, newNode), right);
        Root?.Parent = null;

        Count++;
    }

    public override bool Remove(TKey key)
    {
        if (key == null) throw new Exception("NULL");
        var node = FindNode(key);
        if (node == null) return false;

        var merged = Merge(node.Left, node.Right);

        if (node.Parent == null)
        {
            Root = merged;
            Root?.Parent = null;
        }
        else if (node.IsLeftChild)
        {
            node.Parent.Left = merged;
            merged?.Parent = node.Parent;
        } else {
            node.Parent.Right = merged;
            merged?.Parent = node.Parent;
        }

        Count--;
        return true;
    }

    protected override TreapNode<TKey, TValue> CreateNode(TKey key, TValue value)
    {
        return new TreapNode<TKey, TValue>(key, value);
    }
    protected override void OnNodeAdded(TreapNode<TKey, TValue> newNode)
    {
    }

    protected override void OnNodeRemoved(TreapNode<TKey, TValue>? parent, TreapNode<TKey, TValue>? child)
    {
    }

}
