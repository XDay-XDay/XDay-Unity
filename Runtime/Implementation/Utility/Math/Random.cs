

namespace XDay.UtilityAPI
{
    internal class Random : IRandom
    {
        //[0, 1)
        public float Value => (float)m_Impl.NextDouble();

        public Random(int seed = 0)
        {
            if (seed == 0)
            {
                seed = System.DateTime.Now.Millisecond;
            }
            m_Impl = new System.Random(seed);
        }

        //[min, max)
        public float NextFloat(float min, float max)
        {
            return min + Value * (max - min);
        }

        //[min, max)
        public int NextInt(int min, int max)
        {
            return m_Impl.Next(min, max);
        }

        private System.Random m_Impl;
    }
}
