

namespace XDay.UtilityAPI
{
    public interface IRandom
    {
        static IRandom Create(int seed = 0)
        {
            return new Random(seed);
        }

        //[0, 1)
        float Value { get; }

        //[min, max)
        float NextFloat(float min, float max);
        //[min, max)
        int NextInt(int min, int max);
    }
}
