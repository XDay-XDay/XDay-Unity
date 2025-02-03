
namespace XDay.WorldAPI.CDLODTerrain
{
    internal class Tile
    {
        public int X => m_X;
        public int Y => m_Y;
        public int Index => m_Index;
        public QuadTree QuadTree => m_QuadTree;
        public TerrainSystem Terrain => m_Terrain;
        public string MaterialPath => m_MaterialPath;
        public bool IsVisible { get => m_Visible; set => m_Visible = value; }

        public Tile(string materialPath, int x, int y, int index)
        {
            m_X = x;
            m_Y = y;
            m_Index = index;
            m_MaterialPath = materialPath;
        }

        public void Init(TerrainSystem terrain)
        {
            m_Terrain = terrain;
            var lodCount = m_Terrain.LODCount;
            var tileSize = (int)m_Terrain.TileWidth;
            m_QuadTree = new QuadTree(m_X * tileSize, m_Y * tileSize, tileSize, tileSize, lodCount - 1, m_Terrain.GetHeight);
        }

        private readonly int m_X;
        private readonly int m_Y;
        private readonly int m_Index;
        private QuadTree m_QuadTree;
        private TerrainSystem m_Terrain;
        private readonly string m_MaterialPath;
        private bool m_Visible = false;
    }
}

//XDay