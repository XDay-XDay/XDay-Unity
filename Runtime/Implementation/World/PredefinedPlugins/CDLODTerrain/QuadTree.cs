using UnityEngine;

namespace XDay.WorldAPI.CDLODTerrain
{
    internal class QuadTreeNode
    {
        public int Code => m_Code;
        public int DebugID => m_DebugID;
        public int Depth => m_Depth;
        public float MinHeight { get => m_MinHeight; set => m_MinHeight = value; }
        public float MaxHeight { get => m_MaxHeight; set => m_MaxHeight = value; }
        public int X => m_X;
        public int Y => m_Y;
        public int Width => m_Width;
        public int Height => m_Height;
        public QuadTreeNode BottomLeftChild { get => m_BottomLeftChild; set => m_BottomLeftChild = value; }
        public QuadTreeNode TopLeftChild { get => m_TopLeftChild; set => m_TopLeftChild = value; }
        public QuadTreeNode TopRightChild { get => m_TopRightChild; set => m_TopRightChild = value; }
        public QuadTreeNode BottomRightChild { get => m_BottomRightChild; set => m_BottomRightChild = value; }

        public QuadTreeNode(int id, int code, int x, int y, int width, int height, int depth)
        {
            m_DebugID = id;
            m_Code = code;
            m_X = x;
            m_Y = y;
            m_Width = width;
            m_Height = height;
            m_Depth = depth;
        }

        private int m_Code;
        private float m_MinHeight;
        private float m_MaxHeight;
        private int m_Depth;
        //height map coordinate
        //TODO: remove m_X, m_Y, m_Width, m_Height, m_DebugID
        private int m_DebugID;
        private int m_X;
        private int m_Y;
        private int m_Width;
        private int m_Height;
        private QuadTreeNode m_BottomLeftChild;
        private QuadTreeNode m_TopLeftChild;
        private QuadTreeNode m_BottomRightChild;
        private QuadTreeNode m_TopRightChild;
    }

    internal class QuadTree
    {
        public QuadTreeNode RootNode => m_RootNode;
        public int StartX => m_StartX;
        public int StartY => m_StartY;

        //depth表示有多少层子节点,0表示无子节点,1表示一层子节点,以此类推
        public QuadTree(int x, int y, int width, int height, int depth, GetHeightFunction getHeightFunction)
        {
            m_TotalDepth = depth;
            m_GetHeightFunction = getHeightFunction;
            m_StartX = x;
            m_StartY = y;

            m_RootNode = new QuadTreeNode(++m_NextNodeID, code:0, x, y, width, height, depth: 0);
            Build(m_RootNode, x, y, width, height, depth: 0);
        }

        private void Build(QuadTreeNode parentNode, int nodeX, int nodeY, int nodeWidth, int nodeHeight, int depth)
        {
            if (depth == m_TotalDepth)
            {
                var minHeight = float.MaxValue;
                var maxHeight = float.MinValue;
                //parentNode is leaf node
                for (var y = 0; y < nodeHeight; ++y)
                {
                    for (var x = 0; x < nodeWidth; ++x)
                    {
                        var height = m_GetHeightFunction(x + nodeX, y + nodeY, out var valid);
                        if (valid)
                        {
                            if (height < minHeight)
                            {
                                minHeight = height;
                            }
                            if (height > maxHeight)
                            {
                                maxHeight = height;
                            }
                        }
                    }
                }
                parentNode.MinHeight = minHeight;
                parentNode.MaxHeight = maxHeight;
                return;
            }

            parentNode.BottomLeftChild = new QuadTreeNode(++m_NextNodeID, parentNode.Code * 10 + 0, nodeX, nodeY, nodeWidth / 2, nodeHeight / 2, depth + 1);
            Build(parentNode.BottomLeftChild, nodeX, nodeY, nodeWidth / 2, nodeHeight / 2, depth + 1);

            parentNode.TopLeftChild = new QuadTreeNode(++m_NextNodeID, parentNode.Code * 10 + 1, nodeX, nodeY + nodeHeight / 2, nodeWidth / 2, nodeHeight / 2, depth + 1);
            Build(parentNode.TopLeftChild, nodeX, nodeY + nodeHeight / 2, nodeWidth / 2, nodeHeight / 2, depth + 1);

            parentNode.TopRightChild = new QuadTreeNode(++m_NextNodeID, parentNode.Code * 10 + 2, nodeX + nodeWidth / 2, nodeY + nodeHeight / 2, nodeWidth / 2, nodeHeight / 2, depth + 1);
            Build(parentNode.TopRightChild, nodeX + nodeWidth / 2, nodeY + nodeHeight / 2, nodeWidth / 2, nodeHeight / 2, depth + 1);

            parentNode.BottomRightChild = new QuadTreeNode(++m_NextNodeID, parentNode.Code * 10 + 3, nodeX + nodeWidth / 2, nodeY, nodeWidth / 2, nodeHeight / 2, depth + 1);
            Build(parentNode.BottomRightChild, nodeX + nodeWidth / 2, nodeY, nodeWidth / 2, nodeHeight / 2, depth + 1);

            parentNode.MinHeight = Mathf.Min(parentNode.BottomLeftChild.MinHeight, parentNode.TopLeftChild.MinHeight, parentNode.TopRightChild.MinHeight, parentNode.BottomRightChild.MinHeight);
            parentNode.MaxHeight = Mathf.Max(parentNode.BottomLeftChild.MaxHeight, parentNode.TopLeftChild.MaxHeight, parentNode.TopRightChild.MaxHeight, parentNode.BottomRightChild.MaxHeight);
        }

        public delegate float GetHeightFunction(int x, int y, out bool isValid);

        private int m_TotalDepth;
        private int m_NextNodeID;
        private int m_StartX;
        private int m_StartY;
        private QuadTreeNode m_RootNode;
        private GetHeightFunction m_GetHeightFunction;
    }
}