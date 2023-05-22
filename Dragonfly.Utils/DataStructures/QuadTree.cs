using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dragonfly.Utils
{
    public class QuadTree<T> : IQuadTreeNode<T>
    {
        private struct QuadTreeSideConnection
        {
            public QuadTree<T> Tree;
            public QuadTreeSide Side;
            public bool FlipCoordinates;
        }

        private QuadTreeSideConnection[] connections;
        private IQuadTreeManager<T> t;
		private IQuadTreeNode<T> root;

        public QuadTree(IQuadTreeManager<T> treeManager)
        {
            this.t = treeManager;
            this.root = new Node(t.CreateRoot(), null, t) { Tree = this };
            this.connections = new QuadTreeSideConnection[4];
            NodeCount = 1;
            LeafCount = 1;
            t.OnNodeEvent(new QuadTreeNodeEventArgs<T> { NodeValue = root.Value, Type = QuadTreeNodeEvent.Enabled });
        }

        public int NodeCount { get; private set; }

        public int LeafCount { get; private set; }

        public static void Connect(QuadTree<T> srcTree, QuadTreeSide srcSide, QuadTree<T> destTree, QuadTreeSide destSide, bool flipCoordinates)
        {
            srcTree.connections[(int)srcSide].Tree = destTree;
            srcTree.connections[(int)srcSide].Side = destSide;
            srcTree.connections[(int)srcSide].FlipCoordinates = flipCoordinates;
            destTree.connections[(int)destSide].Tree = srcTree;
            destTree.connections[(int)destSide].Side = srcSide;
            destTree.connections[(int)destSide].FlipCoordinates = flipCoordinates;
        }

        #region IQuadTreeNode<T> facade

        public T Value
        {
            get { return root.Value; }
        }

        public IQuadTreeNode<T> Parent { get { return root.Parent; } }

        public bool IsLeaf { get { return root.IsLeaf; } }

        public int Depth { get { return root.Depth; } }

        public bool IsRoot { get { return true; } }

        public void Divide()
        {
            root.Divide();
        }

        public void Group()
        {
            root.Group();
        }

        public void RemoveUnusedNodes()
        {
            root.RemoveUnusedNodes();
        }

        public IReadOnlyList<IQuadTreeNode<T>> GetChildNodes()
        {
            return root.GetChildNodes();
        }

        public IQuadTreeNode<T> GetEdgeAtCoord(QuadTreeSide side, long coord)
        {
            return root.GetEdgeAtCoord(side, coord);
        }

        public IQuadTreeNode<T> TopLeftChild
        {
            get { return root.TopLeftChild; }
        }

        public IQuadTreeNode<T> TopRightChild
        {
            get { return root.TopRightChild; }
        }

        public IQuadTreeNode<T> BottomLeftChild
        {
            get { return root.BottomLeftChild; }
        }

        public IQuadTreeNode<T> BottomRightChild
        {
            get { return root.BottomRightChild; }
        }

        public IQuadTreeNode<T> Top
        {
            get { return root.Top; }
        }
        public IQuadTreeNode<T> Left
        {
            get { return root.Left; }
        }

        public IQuadTreeNode<T> Bottom
        {
            get { return root.Bottom; }
        }

        public IQuadTreeNode<T> Right
        {
            get { return root.Right; }
        }

        public long LeftRightCoordinate => root.LeftRightCoordinate;

        public long TopBottonCoordinate => root.TopBottonCoordinate;

        #endregion

        #region Enumeration

        /// <summary>
        /// Iterates all the leaves of this quadtree.
        /// </summary>
        public LeavesEnumerator Leaves { get { return new LeavesEnumerator(this); } }

        public struct LeavesEnumerator : IEnumerator<IQuadTreeNode<T>>
        {
            private QuadTree<T> quadTree;

            public LeavesEnumerator(QuadTree<T> quadTree)
            {
                this.quadTree = quadTree;
                Current = null;
            }

            // Foreach compatibility
            public LeavesEnumerator GetEnumerator()
            {
                return this;
            }

            public IQuadTreeNode<T> Current { get; private set; }

            object IEnumerator.Current => Current;

            public void Dispose() { }

            public bool MoveNext()
            {
                if (Current == null)
                {
                    // initial state, start navigating from the root
                    Current = quadTree.root;
                }
                else
                {
                    // navigate out of completed branches
                    while (Current.Parent != null && Current == Current.Parent.BottomRightChild)
                        Current = Current.Parent;

                    // if root is reached, all branches have been enumerated
                    if (Current.IsRoot)
                            return false;

                    // move to the next sibling
                    if (Current.Parent != null)
                    {
                        if (Current == Current.Parent.TopLeftChild)
                            Current = Current.Parent.TopRightChild;
                        else if (Current == Current.Parent.TopRightChild)
                            Current = Current.Parent.BottomLeftChild;
                        else // if (curNode == curNode.Parent.BottomLeftChild)
                            Current = Current.Parent.BottomRightChild;
                    }
                }

                // navigate to a leaf           
                while (!Current.IsLeaf)
                    Current = Current.TopLeftChild;
                
                return true;
            }

            public void Reset()
            {
                Current = null;
            }
        }

        /// <summary>
        /// Iterates all the nodes of this quadtree, strarting from the root. Children are iterated recursively.
        /// </summary>
        public TopDownEnumerator TopDown { get { return new TopDownEnumerator(this); } }

        public struct TopDownEnumerator : IEnumerator<IQuadTreeNode<T>>
        {
            private bool endReached;
            private QuadTree<T> quadTree;

            public TopDownEnumerator(QuadTree<T> quadTree) : this()
            {
                this.quadTree = quadTree;
            }

            // Foreach compatibility
            public TopDownEnumerator GetEnumerator()
            {
                return this;
            }

            public IQuadTreeNode<T> Current { get; private set; }

            object IEnumerator.Current => Current;

            public void Dispose() { }

            /// <summary>
            /// Can be set to true to stop enumerating down the children of the current node.
            /// </summary>
            public bool SkipNextBranch { get; set; }

            public bool MoveNext()
            {
                if (endReached)
                    return false;

                if (Current == null)
                {
                    // initial state, start navigating from the root
                    Current = quadTree.root;
                }
                else if (!Current.IsLeaf && !SkipNextBranch)
                {
                    // move down to the first child
                    Current = Current.TopLeftChild;
                }
                else
                {
                    SkipNextBranch = false;

                    // navigate out of completed branches
                    while (Current.Parent != null && Current == Current.Parent.BottomRightChild)
                        Current = Current.Parent;

                    // if root is reached, all branches have been enumerated
                    if (Current.IsRoot)
                    {
                        endReached = true;
                        return false;
                    }

                    // move to the next sibling
                    if (Current.Parent != null)
                    {
                        if (Current == Current.Parent.TopLeftChild)
                            Current = Current.Parent.TopRightChild;
                        else if (Current == Current.Parent.TopRightChild)
                            Current = Current.Parent.BottomLeftChild;
                        else // if (curNode == curNode.Parent.BottomLeftChild)
                            Current = Current.Parent.BottomRightChild;
                    }
                }

                return true;
            }

            public void Reset()
            {
                endReached = false;
                Current = null;
            }
        }

        /// <summary>
        /// Iterates all the nodes of this quadtree, starting from the root. Nodes at lower depths are iterated first.
        /// </summary>
        public LayeredTopDownEnumerator LayeredTopDown { get { return new LayeredTopDownEnumerator(this); } }

        public struct LayeredTopDownEnumerator : IEnumerator<IQuadTreeNode<T>>
        {
            private bool endReached, nextDepthAvailable;
            private int curDepth;
            private QuadTree<T> quadTree;

            public LayeredTopDownEnumerator(QuadTree<T> quadTree) : this()
            {
                this.quadTree = quadTree;
            }

            // Foreach compatibility
            public LayeredTopDownEnumerator GetEnumerator()
            {
                return this;
            }

            public IQuadTreeNode<T> Current { get; private set; }

            object IEnumerator.Current => Current;

            public void Dispose() { }

            /// <summary>
            /// Can be set to true to stop enumerating down the children of the current node.
            /// </summary>
            public bool SkipNextBranch { get; set; }

            public bool MoveNext()
            {
                if (endReached)
                    return false;

                searchNext:

                if (Current == null)
                {
                    // initial state, start navigating from the root
                    Current = quadTree.root;
                }
                else
                {
                    // navigate out of completed branches
                    while (Current.Parent != null && Current == Current.Parent.BottomRightChild)
                        Current = Current.Parent;

                    // if root is reached, all branches have been enumerated
                    if (Current.IsRoot)
                    {
                        if (nextDepthAvailable)
                        {
                            nextDepthAvailable = false;
                            curDepth++;
                            Current = null;
                            goto searchNext;
                        }
                        else
                        {
                            endReached = true;
                            return false;
                        }
                    }

                    // move to the next sibling
                    if (Current.Parent != null)
                    {
                        if (Current == Current.Parent.TopLeftChild)
                            Current = Current.Parent.TopRightChild;
                        else if (Current == Current.Parent.TopRightChild)
                            Current = Current.Parent.BottomLeftChild;
                        else // if (curNode == curNode.Parent.BottomLeftChild)
                            Current = Current.Parent.BottomRightChild;
                    }
                }

                // navigate to a leaf at the current depth          
                while (!Current.IsLeaf && Current.Depth < curDepth)
                    Current = Current.TopLeftChild;

                // if the current node is not at the required depth, keep iterating
                if (Current.Depth < curDepth)
                    goto searchNext;

                // if the current depth has non-leaf nodes, at least other 4 nodes are available at the next depth
                nextDepthAvailable |= !Current.IsLeaf;

                return true;
            }

            public void Reset()
            {
                endReached = false;
                curDepth = 0;
                nextDepthAvailable = false;
                Current = null;
            }
        }

        #endregion

        private class Node : IQuadTreeNode<T>
        {
            private static readonly int TOP_LEFT = 0;
            private static readonly int TOP_RIGHT = 1;
            private static readonly int BOTTOM_LEFT = 2;
            private static readonly int BOTTOM_RIGHT = 3;

            private IQuadTreeManager<T> t;
            private IQuadTreeNode<T>[] childNodes;

            public Node(T value, Node parent, IQuadTreeManager<T> treeManager)
            {
                this.t = treeManager;
                this.Value = value;
                this.Parent = parent;
                this.IsLeaf = true;
                this.Depth = parent != null ? parent.Depth + 1 : 0;
                if (parent != null) Tree = parent.Tree;
            }

            public QuadTree<T> Tree { get; set; }

            public T Value { get; private set; }

            public IQuadTreeNode<T> Parent { get; private set; }

            public bool IsLeaf { get; private set; }

            public int Depth { get; private set; }

            public bool IsRoot
            {
                get
                {
                    return Depth == 0;
                }
            }

            public void Divide()
            {
                if (!IsLeaf) return; // already divided!

                // create children if missing
                if (childNodes == null)
                {
                    T topLeftValue = t.CreateTopLeft(this.Value);
                    T topRightValue = t.CreateTopRight(this.Value);
                    T bottomLeftValue = t.CreateBottomLeft(this.Value);
                    T bottomRightValue = t.CreateBottomRight(this.Value);

                    // delta coordiantes of children from this node, along each direction
                    long deltaCoords = (long)1 << (61 - Depth); // leave out 1 bit to avoid overflow in coordinates calculations later

                    childNodes = new IQuadTreeNode<T>[4];
                    childNodes[TOP_LEFT] = new Node(topLeftValue, this, t) { LeftRightCoordinate = LeftRightCoordinate - deltaCoords, TopBottonCoordinate = TopBottonCoordinate - deltaCoords };
                    childNodes[TOP_RIGHT] = new Node(topRightValue, this, t) { LeftRightCoordinate = LeftRightCoordinate + deltaCoords, TopBottonCoordinate = TopBottonCoordinate - deltaCoords };
                    childNodes[BOTTOM_LEFT] = new Node(bottomLeftValue, this, t) { LeftRightCoordinate = LeftRightCoordinate - deltaCoords, TopBottonCoordinate = TopBottonCoordinate + deltaCoords };
                    childNodes[BOTTOM_RIGHT] = new Node(bottomRightValue, this, t) { LeftRightCoordinate = LeftRightCoordinate + deltaCoords, TopBottonCoordinate = TopBottonCoordinate + deltaCoords };
                    Tree.NodeCount += 4;
                    Tree.LeafCount += 3;
                }

                // group children sub-nodes if any and enable them
                foreach (IQuadTreeNode<T> child in childNodes)
                {
                    child.Group();
                    t.OnNodeEvent(new QuadTreeNodeEventArgs<T> { NodeValue = child.Value, Type = QuadTreeNodeEvent.Enabled });
                }

                this.IsLeaf = false;
                t.OnNodeEvent(new QuadTreeNodeEventArgs<T> { NodeValue = Value, Type = QuadTreeNodeEvent.Divided });
            }

            public void Group()
            {
                if (IsLeaf) return; // already grouped!

                foreach (IQuadTreeNode<T> child in childNodes)
                {
                    child.Group();
                    t.OnNodeEvent(new QuadTreeNodeEventArgs<T> { NodeValue = child.Value, Type = QuadTreeNodeEvent.Disabled });
                }

                IsLeaf = true;
                Tree.NodeCount -= 4;
                Tree.LeafCount -= 3;
                t.OnNodeEvent(new QuadTreeNodeEventArgs<T> { NodeValue = Value, Type = QuadTreeNodeEvent.Grouped });
            }

            public void RemoveUnusedNodes()
            {
                if (childNodes == null) return;

                foreach (IQuadTreeNode<T> child in childNodes)
                    child.RemoveUnusedNodes();

                if (IsLeaf)
                {
                    foreach (IQuadTreeNode<T> child in childNodes)
                        t.OnNodeEvent(new QuadTreeNodeEventArgs<T> { NodeValue = child.Value, Type = QuadTreeNodeEvent.Deleted });

                    childNodes = null;
                }
            }

            public IReadOnlyList<IQuadTreeNode<T>> GetChildNodes()
            {
                if (IsLeaf) throw new InvalidOperationException("A leaft node don't have child nodes.");
                return childNodes;
            }

            public IQuadTreeNode<T> TopLeftChild
            {
                get
                {
                    return GetChildNodes()[TOP_LEFT];
                }
            }

            public IQuadTreeNode<T> TopRightChild
            {
                get
                {
                    return GetChildNodes()[TOP_RIGHT];
                }
            }

            public IQuadTreeNode<T> BottomLeftChild
            {
                get
                {
                    return GetChildNodes()[BOTTOM_LEFT];
                }
            }

            public IQuadTreeNode<T> BottomRightChild
            {
                get
                {
                    return GetChildNodes()[BOTTOM_RIGHT];
                }
            }

            private IQuadTreeNode<T> GetCloserLeftRight(long atCoord, IQuadTreeNode<T> node1, IQuadTreeNode<T> node2)
            {
                long diff1 = Math.Abs(atCoord - node1.LeftRightCoordinate);
                long diff2 = Math.Abs(atCoord - node2.LeftRightCoordinate);
                return diff1 < diff2 ? node1 : node2;
            }

            private IQuadTreeNode<T> GetCloserTopBottom(long atCoord, IQuadTreeNode<T> node1, IQuadTreeNode<T> node2)
            {
                long diff1 = Math.Abs(atCoord - node1.TopBottonCoordinate);
                long diff2 = Math.Abs(atCoord - node2.TopBottonCoordinate);
                return diff1 < diff2 ? node1 : node2;
            }

            public IQuadTreeNode<T> Top
            {
                get
                {
                    // search common anchestor where this node is on the bottom side
                    IQuadTreeNode<T> anchestor = this;
                    while (anchestor.Parent != null && anchestor != anchestor.Parent.BottomLeftChild && anchestor != anchestor.Parent.BottomRightChild)
                        anchestor = anchestor.Parent;
                    anchestor = anchestor.Parent;

                    if (anchestor == null) // not found..
                    {
                        // search for a connection
                        QuadTreeSideConnection connection = Tree.connections[(int)QuadTreeSide.Top];
                        if (connection.Tree == null)
                            return null; // not connected, out of bounds

                        // connection found, return a search on the edge of the connected tree
                        return connection.Tree.GetEdgeAtCoord(connection.Side, connection.FlipCoordinates ? -LeftRightCoordinate : LeftRightCoordinate);
                    }

                    // move down the tree to search the left node closer to the current
                    IQuadTreeNode<T> topNode = GetCloserLeftRight(LeftRightCoordinate, anchestor.TopLeftChild, anchestor.TopRightChild);
                    return topNode.GetEdgeAtCoord(QuadTreeSide.Bottom, LeftRightCoordinate);                  
                }
            }

            public IQuadTreeNode<T> Bottom
            {
                get
                {
                    // search common anchestor where this node is on the top side
                    IQuadTreeNode<T> anchestor = this;
                    while (anchestor.Parent != null && anchestor != anchestor.Parent.TopLeftChild && anchestor != anchestor.Parent.TopRightChild)
                        anchestor = anchestor.Parent;
                    anchestor = anchestor.Parent;

                    if(anchestor == null) // not found..
                    {
                        // search for a connection
                        QuadTreeSideConnection connection = Tree.connections[(int)QuadTreeSide.Bottom];
                        if (connection.Tree == null)
                            return null; // not connected, out of bounds

                        // connection found, return a search on the edge of the connected tree
                        return connection.Tree.GetEdgeAtCoord(connection.Side, connection.FlipCoordinates ? -LeftRightCoordinate : LeftRightCoordinate);
                    }

                    // move down the tree to search the node closer to the current
                    IQuadTreeNode<T> bottomNode = GetCloserLeftRight(LeftRightCoordinate, anchestor.BottomLeftChild, anchestor.BottomRightChild);
                    return bottomNode.GetEdgeAtCoord(QuadTreeSide.Top, LeftRightCoordinate);         
                }
            }

            public IQuadTreeNode<T> Left
            {
                get
                {
                    // search common anchestor where this node is on the right side
                    IQuadTreeNode<T> anchestor = this;
                    while (anchestor.Parent != null && anchestor != anchestor.Parent.TopRightChild && anchestor != anchestor.Parent.BottomRightChild)
                        anchestor = anchestor.Parent;
                    anchestor = anchestor.Parent;

                    if (anchestor == null) // not found..
                    {
                        // search for a connection
                        QuadTreeSideConnection connection = Tree.connections[(int)QuadTreeSide.Left];
                        if (connection.Tree == null)
                            return null; // not connected, out of bounds

                        // connection found, return a search on the edge of the connected tree
                        return connection.Tree.GetEdgeAtCoord(connection.Side, connection.FlipCoordinates ? -TopBottonCoordinate : TopBottonCoordinate);
                    }

                    // move down the tree to search the node closer to the current
                    IQuadTreeNode<T> leftNode = GetCloserTopBottom(TopBottonCoordinate, anchestor.TopLeftChild, anchestor.BottomLeftChild);
                    return leftNode.GetEdgeAtCoord(QuadTreeSide.Right, TopBottonCoordinate);
                }
            }

            public IQuadTreeNode<T> Right
            {
                get
                {
                    // search common anchestor where this node is on the left side
                    IQuadTreeNode<T> anchestor = this;
                    while (anchestor.Parent != null && anchestor != anchestor.Parent.TopLeftChild && anchestor != anchestor.Parent.BottomLeftChild)
                        anchestor = anchestor.Parent;
                    anchestor = anchestor.Parent;

                    if (anchestor == null) // not found..
                    {
                        // search for a connection
                        QuadTreeSideConnection connection = Tree.connections[(int)QuadTreeSide.Right];
                        if (connection.Tree == null)
                            return null; // not connected, out of bounds

                        // connection found, return a search on the edge of the connected tree
                        return connection.Tree.GetEdgeAtCoord(connection.Side, connection.FlipCoordinates ? -TopBottonCoordinate : TopBottonCoordinate);
                    }

                    // move down the tree to search the node closer to the current
                    IQuadTreeNode<T> rightNode = GetCloserTopBottom(TopBottonCoordinate, anchestor.TopRightChild, anchestor.BottomRightChild);
                    return rightNode.GetEdgeAtCoord(QuadTreeSide.Left, TopBottonCoordinate);
                }
            }

            public long LeftRightCoordinate { get; private set; }

            public long TopBottonCoordinate { get; private set; }

            public IQuadTreeNode<T> GetEdgeAtCoord(QuadTreeSide side, long coord)
            {
                IQuadTreeNode<T> edge = this;
                while (!edge.IsLeaf)
                    switch (side)
                    {
                        case QuadTreeSide.Left:
                            edge = GetCloserTopBottom(coord, edge.TopLeftChild, edge.BottomLeftChild);
                            break;
                        case QuadTreeSide.Right:
                            edge = GetCloserTopBottom(coord, edge.TopRightChild, edge.BottomRightChild);
                            break;
                        case QuadTreeSide.Top:
                            edge = GetCloserLeftRight(coord, edge.TopLeftChild, edge.TopRightChild);
                            break;
                        case QuadTreeSide.Bottom:
                            edge = GetCloserLeftRight(coord, edge.BottomLeftChild, edge.BottomRightChild);
                            break;
                    }
                return edge;
            }

        }
    }

	public interface IQuadTreeNode<T>
	{	
		T Value { get; }

        IQuadTreeNode<T> Parent { get; }

        bool IsLeaf { get; }

        int Depth { get; }

        bool IsRoot { get; }
		
		void Divide();
		
		void Group();
		
		void RemoveUnusedNodes();
		
		IReadOnlyList<IQuadTreeNode<T>> GetChildNodes();

        IQuadTreeNode<T> TopLeftChild { get; }

        IQuadTreeNode<T> TopRightChild { get; }

        IQuadTreeNode<T> BottomLeftChild { get; }

        IQuadTreeNode<T> BottomRightChild { get; }

        /// <summary>
        /// Returns a tree node located above this node, at a depth level equal or lower of the current. If this node doesn't exist, this call returns null.
        /// </summary>
        IQuadTreeNode<T> Top { get; }
        /// <summary>
        /// Returns a tree node located to the left of this node, at a depth level equal or lower of the current. If this node doesn't exist, this call returns null.
        /// </summary>
        IQuadTreeNode<T> Left { get; }
        /// <summary>
        /// Returns a tree node located below this node, at a depth level equal or lower of the current. If this node doesn't exist, this call returns null.
        /// </summary>
        IQuadTreeNode<T> Bottom { get; }
        /// <summary>
        /// Returns a tree node located on the right of this node, at a depth level equal or lower of the current. If this node doesn't exist, this call returns null.
        /// </summary>
        IQuadTreeNode<T> Right { get; }

        /// <summary>
        /// Return an absolute coordinate from left to right of this node in the tree, which can be compared to check relative positioning of nodes.
        /// </summary>
        long LeftRightCoordinate { get; }

        /// <summary>
        /// Return an absolute coordinate from top to bottom of this node in the tree, which can be compared to check relative positioning of nodes.
        /// </summary>
        long TopBottonCoordinate { get; }

        /// <summary>
        /// Returns the leaf on the specified edge of the tree that is the closest to the specified coordinate.
        /// </summary>
        IQuadTreeNode<T> GetEdgeAtCoord(QuadTreeSide side, long coord);
    }

    public interface IQuadTreeManager<T>
    {
        T CreateRoot();

        T CreateTopLeft(T parent);

        T CreateTopRight(T parent);

        T CreateBottomLeft(T parent);

        T CreateBottomRight(T parent);

        /// <summary>
        /// Called when a quad-tree event occur on a node of a tree.
        /// </summary>
        void OnNodeEvent(QuadTreeNodeEventArgs<T>  args);
    }
	
    public enum QuadTreeNodeEvent
    {
        /// <summary>
        /// Occur when an existing disabled node has been enabled. (e.g. enable an existing children, when the parent is divided).
        /// </summary>
        Enabled,
        /// <summary>
        /// The node has been disable (do not appear in the tree but has not been deleted and could be re-enabled in the future).
        /// </summary>
        Disabled,
        /// <summary>
        /// The node has been deleted.
        /// </summary>
        Deleted,
        /// <summary>
        /// Notify a node that its children have been disabled, and this node is now a leaf.
        /// </summary>
        Grouped,
        /// <summary>
        /// Notify a node that its children have been created and enabled, and this node is no longer a leaf.
        /// </summary>
        Divided
    }

    public class QuadTreeNodeEventArgs<T>
    {
        /// <summary>
        /// The type of event occurred on the node.
        /// </summary>
        public QuadTreeNodeEvent Type { get; set; }

        /// <summary>
        /// The value of the affected node.
        /// </summary>
        public T NodeValue { get;  set; }
    }

    public enum QuadTreeSide
    {
        Left = 0,
        Right = 1,
        Top = 2,
        Bottom = 3
    }

}
