using Dragonfly.Graphics.Math;
using Dragonfly.Utils;
using System;
using System.Collections.Generic;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// Manage allocations of on a 2d atlas space as a a quad tree.
    /// Only power of two textures and allocations are supported.
    /// </summary>
    public class AtlasLayoutQuadTree : AtlasLayout
    {
        private QuadTree<SubTexData> atlasState;
        private int atlasSize;

        public AtlasLayoutQuadTree(int atlasSize)
        {
            if (!atlasSize.IsPowerOf2())
                throw new InvalidOperationException("Only power of 2 atlas are supported!");
            this.atlasSize = atlasSize;

            atlasState = new QuadTree<SubTexData>(new SubTexManager(this));
        }

        public override Int2 Resolution => (Int2)atlasSize;

        public float FreeSpacePercent
        {
            get
            {
                long totalPixels = Resolution.Width * Resolution.Height, totalUnusedPixels = 0;
                foreach (IQuadTreeNode<SubTexData> subTex in atlasState.Leaves)
                {
                    if (subTex.Value.Allocated) continue;
                    totalUnusedPixels += subTex.Value.PixSize * subTex.Value.PixSize;
                }
                return (float)totalUnusedPixels / totalPixels;
            }
        }

        public override bool TryAllocateSubTexture(Int2 preferredSize, out SubTextureReference subTexture)
        {
            int size = Math.Max(preferredSize.X, preferredSize.Y).CeilPower2();
            IQuadTreeNode<SubTexData> closestFit = null;
            subTexture = null;

            // search an available quad of the same size
            {
                foreach (IQuadTreeNode<SubTexData> subTex in atlasState.Leaves)
                {
                    if (subTex.Value.Allocated) continue;
                    if (subTex.Value.PixSize < size) continue;

                    if (closestFit == null || closestFit.Value.PixSize > subTex.Value.PixSize)
                        closestFit = subTex;

                    if (subTex.Value.PixSize == size)
                        break; // quad found
                }
            }

            if (closestFit == null) return false; // not enough space available!

            // divide the selected node until its of the required size
            while (closestFit.Value.PixSize > size)
            {
                closestFit.Divide();
                closestFit = closestFit.TopLeftChild;
            }

            closestFit.Value.Allocated = true;
            subTexture = new SubTextureReferenceQuadNode(closestFit.Value.TexArea, closestFit, this);
            return true;         
        }

        public override void ReleaseSubTexture(SubTextureReference subTexture)
        {
            // flag as not allocated
            IQuadTreeNode<SubTexData> curNode = (subTexture as SubTextureReferenceQuadNode).AtlasNode;
            curNode.Value.Allocated = false;

            // group free nodes to reduce fragmentation
            while(!curNode.IsRoot)
            {
                foreach(IQuadTreeNode<SubTexData> node in curNode.Parent.GetChildNodes())
                    if(node.Value.Allocated || !node.IsLeaf) goto DefragCompleted;
            
                curNode.Parent.Group();
                curNode = curNode.Parent;
            }

            DefragCompleted:;
        }

        private class SubTexData
        {
            public AARect TexArea;
            public bool Allocated;
            public int PixSize;
        }

        private class SubTexManager : IQuadTreeManager<SubTexData>
        {
            private AtlasLayoutQuadTree atlas;

            public SubTexManager(AtlasLayoutQuadTree parent)
            {
                atlas = parent;
            }

            public SubTexData CreateRoot()
            {
                SubTexData root = new SubTexData();
                root.TexArea = AARect.Bounding((Float2)0, (Float2)1);
                root.Allocated = false;
                root.PixSize = atlas.Resolution.Width;
                return root;
            }

            public SubTexData CreateBottomLeft(SubTexData parent)
            {
                return CreateSubTex(parent, new Float2(0, 0.5f));
            }

            public SubTexData CreateBottomRight(SubTexData parent)
            {
                return CreateSubTex(parent, new Float2(0.5f, 0.5f));
            }

            public SubTexData CreateTopLeft(SubTexData parent)
            {
                return CreateSubTex(parent, new Float2(0, 0));
            }

            public SubTexData CreateTopRight(SubTexData parent)
            {
                return CreateSubTex(parent, new Float2(0.5f, 0));
            }

            private SubTexData CreateSubTex(SubTexData parent, Float2 offset)
            {
                SubTexData child = new SubTexData();
                Float2 startCorner = parent.TexArea.Min + parent.TexArea.Size * offset;
                child.TexArea = AARect.Bounding(startCorner, startCorner + parent.TexArea.Size * 0.5f);
                child.Allocated = false;
                child.PixSize = parent.PixSize / 2;
                return child;
            }

            public void OnNodeEvent(QuadTreeNodeEventArgs<SubTexData> args)
            {
                if(args.Type ==  QuadTreeNodeEvent.Disabled)
                    args.NodeValue.Allocated = false;
            }
        }

        private class SubTextureReferenceQuadNode : SubTextureReference
        {
            internal IQuadTreeNode<SubTexData> AtlasNode { get; private set; }

            public SubTextureReferenceQuadNode(AARect area, IQuadTreeNode<SubTexData> atlasNode, AtlasLayoutQuadTree parent) : base(area, parent)
            {
                AtlasNode = atlasNode;
            }
        }

    }



}
