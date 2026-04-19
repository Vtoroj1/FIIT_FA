using System.Collections;
using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Interfaces;

namespace TreeDataStructures.Core;

public abstract class BinarySearchTreeBase<TKey, TValue, TNode>(IComparer<TKey>? comparer = null) 
    : ITree<TKey, TValue>
    where TNode : Node<TKey, TValue, TNode>
{
    protected TNode? Root;
    public IComparer<TKey> Comparer { get; protected set; } = comparer ?? Comparer<TKey>.Default; 

    public int Count { get; protected set; }
    
    public bool IsReadOnly => false;

    public ICollection<TKey> Keys => InOrder().Select(e => e.Key).ToList();
    public ICollection<TValue> Values => InOrder().Select(e => e.Value).ToList();
    
    
    public virtual void Add(TKey key, TValue value)
    {
        if (key == null) 
        { 
            throw new Exception("Ключ не может быть null.");
        }

        if (Root == null)
        {
            Root = CreateNode(key, value);
            Root.Parent = null;
            Count = 1;
            OnNodeAdded(Root);
            return;
        }

        TNode? current = Root;
        TNode? parent = null;
        int cmp = 0;

        while (current != null)
        {
            parent = current;
            cmp = Comparer.Compare(key, current.Key);

            if (cmp == 0)
            {
                current.Value = value;
                return;
            }

            current = cmp < 0 ? current.Left : current.Right;
        }

        TNode newNode = CreateNode(key, value);
        newNode.Parent = parent;

        if (Comparer.Compare(key, parent!.Key) < 0) 
        {
            parent.Left = newNode;  
        }
        else
        {
            parent.Right = newNode;
        }

        Count++;
        OnNodeAdded(newNode);
    }

    public virtual bool Remove(TKey key)
    {
        if (key == null) throw new ArgumentNullException("Ключ не может быть null.");

        TNode? node = FindNode(key);
        if (node == null) { return false; }

        RemoveNode(node);
        this.Count--;
        return true;
    }
    
    
    protected virtual void RemoveNode(TNode node)
    {
        if (node.Left == null)
        {
            Transplant(node, node.Right);
            OnNodeRemoved(node.Parent, node.Right);
            return;
        }

        if (node.Right == null)
        {
            Transplant(node, node.Left);
            OnNodeRemoved(node.Parent, node.Left);
            return;
        }

        TNode successor = Minimum(node.Right);

        if (successor.Parent != node)
        {
            Transplant(successor, successor.Right);
            successor.Right = node.Right;
            successor.Right!.Parent = successor;
        }

        Transplant(node, successor);
        successor.Left = node.Left;
        successor.Left!.Parent = successor;

        OnNodeRemoved(node.Parent, successor);
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

    private static TNode Minimum(TNode node)
    {
        while (node.Left != null)
            node = node.Left;
        return node;
    }

    protected void RotateLeft(TNode x)
    {
        if (x == null || x.Right == null) throw new System.Exception("Невозможно повернуть");

        TNode y = x.Right;

        x.Right = y.Left;
        if (y.Left != null) y.Left.Parent = x;

        y.Parent = x.Parent;
        if (x.Parent == null)
            Root = y;
        else if (x == x.Parent.Left)
            x.Parent.Left = y;
        else
            x.Parent.Right = y;

        y.Left = x;
        x.Parent = y;
    }

    protected void RotateRight(TNode y)
    {
        if (y == null || y.Left == null) throw new System.Exception("Невозможно повернуть");

        TNode x = y.Left;

        y.Left = x.Right;
        if (x.Right != null) x.Right.Parent = y;

        x.Parent = y.Parent;
        if (y.Parent == null)
            Root = x;
        else if (y == y.Parent.Left)
            y.Parent.Left = x;
        else
            y.Parent.Right = x;

        x.Right = y;
        y.Parent = x;
    }
    
    protected void RotateBigLeft(TNode x)
    {
        if (x.Right != null)
        {
            RotateRight(x.Right);
        }
        RotateLeft(x);
    }
    
    protected void RotateBigRight(TNode y)
    {
        if (y.Left != null) 
        {
            RotateLeft(y.Left);
        }
        RotateRight(y);
    }
    
    protected void RotateDoubleLeft(TNode x)
    {
        var son = x.Right ?? throw new Exception();
        RotateLeft(x);
        RotateLeft(son);
    }
    
    protected void RotateDoubleRight(TNode y)
    {
        var son = y.Left ?? throw new Exception();
        RotateRight(y);
        RotateRight(son);
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
    
    public IEnumerable<TreeEntry<TKey, TValue>> InOrder() 
        => new TreeIterator(Root, TraversalStrategy.InOrder);

    public IEnumerable<TreeEntry<TKey, TValue>> PreOrder() 
        => new TreeIterator(Root, TraversalStrategy.PreOrder);

    public IEnumerable<TreeEntry<TKey, TValue>> PostOrder() 
        => new TreeIterator(Root, TraversalStrategy.PostOrder);

    public IEnumerable<TreeEntry<TKey, TValue>> InOrderReverse() 
        => new TreeIterator(Root, TraversalStrategy.InOrderReverse);

    public IEnumerable<TreeEntry<TKey, TValue>> PreOrderReverse() 
        => new TreeIterator(Root, TraversalStrategy.PreOrderReverse);

    public IEnumerable<TreeEntry<TKey, TValue>> PostOrderReverse() 
        => new TreeIterator(Root, TraversalStrategy.PostOrderReverse);
    
    /// <summary>
    /// Внутренний класс-итератор. 
    /// Реализует паттерн Iterator вручную, без yield return (ban).
    /// </summary>
    private struct TreeIterator : IEnumerable<TreeEntry<TKey, TValue>>, IEnumerator<TreeEntry<TKey, TValue>>
    {
        private readonly TNode? _root;
        private readonly TraversalStrategy _strategy;
        private TNode? _currentNode; 
        private TNode? _previousNode;
        private TreeEntry<TKey, TValue> _currentEntry; 

        public TreeIterator(TNode? root, TraversalStrategy strategy)
        {
            _root = root;
            _strategy = strategy;
            _currentNode = _root;
            _previousNode = null;
            _currentEntry = default;
        }

        private int _numOfStrat => _strategy switch
        {
            TraversalStrategy.PreOrder or TraversalStrategy.PreOrderReverse => 0,   
            TraversalStrategy.InOrder or TraversalStrategy.InOrderReverse => 1,
            TraversalStrategy.PostOrder or TraversalStrategy.PostOrderReverse => 2,
            _ => 1
        };

        private bool _isReverse => _strategy switch
        {
            TraversalStrategy.InOrderReverse or TraversalStrategy.PreOrderReverse or TraversalStrategy.PostOrderReverse => true,
            _ => false
        };

        public TreeEntry<TKey, TValue> Current => _currentEntry;
        object IEnumerator.Current => Current;

        public IEnumerator<TreeEntry<TKey, TValue>> GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Reset()
        {
            _currentNode = _root;
            _previousNode = null;
            _currentEntry = default;
        }

        public void Dispose()
        {
        }

        private int GetDepth(TNode? node)
        {
            int depth = 0;
            while (node?.Parent != null)
            {
                depth++;
                node = node.Parent;
            }
            return depth;
        }

        public bool MoveNext()
        {
            while (_currentNode != null) {
                TNode node = _currentNode;

                TNode? first = _isReverse ? node.Right : node.Left;
                TNode? second = _isReverse ? node.Left : node.Right;

                if (_previousNode == node.Parent)
                {
                    if (_numOfStrat == 0) {
                        _currentEntry = new TreeEntry<TKey, TValue>(node.Key, node.Value, GetDepth(node));
                        _previousNode = node;

                        _currentNode = first ?? second ?? node.Parent;
                        return true;
                    }

                    if (first != null) {
                        _previousNode = node;
                        _currentNode = first;
                        continue;
                    }
                }

                if (_previousNode == first || (_previousNode == node.Parent && first == null))
                {
                    if (_numOfStrat == 1)
                    {
                        _currentEntry = new TreeEntry<TKey, TValue>(node.Key, node.Value, GetDepth(node));
                        _previousNode = node;

                        _currentNode = second ?? node.Parent;
                        return true;
                    }

                    if (second != null)
                    { 
                        _previousNode = node;
                        _currentNode = second; 
                        continue;
                    }
                }

                if (_numOfStrat == 2) 
                {
                    _currentEntry = new TreeEntry<TKey, TValue>(node.Key, node.Value, GetDepth(node));
                    _previousNode = node;
                    _currentNode = node.Parent;
                    return true;
                }

                _previousNode = node;
                _currentNode = node.Parent;
            }

            return false;
        }
    }
    
    private enum TraversalStrategy { InOrder, PreOrder, PostOrder, InOrderReverse, PreOrderReverse, PostOrderReverse }
    
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return InOrder().Select(element => new KeyValuePair<TKey, TValue>(element.Key, element.Value)).GetEnumerator();
    }
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
    public void Clear() { Root = null; Count = 0; }
    public bool Contains(KeyValuePair<TKey, TValue> item) => ContainsKey(item.Key);
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        if (array == null) throw new Exception(nameof(array));
        if (arrayIndex < 0 || arrayIndex > array.Length) throw new Exception(nameof(arrayIndex));
        if (array.Length - arrayIndex < Count) throw new Exception("Недостаточно места в массиве.");
        var iterator = new TreeIterator(Root, TraversalStrategy.InOrder);
        while (iterator.MoveNext())
        {
            array[arrayIndex++] = new KeyValuePair<TKey, TValue>(iterator.Current.Key, iterator.Current.Value);
        }
    }
    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);
}