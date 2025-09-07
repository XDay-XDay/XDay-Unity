

public abstract class Singleton<T> where T : new()
{
    private static T instance;

    public static T S
    {
        get
        {
            if (null == instance)
                instance = new T();

            return instance;
        }
    }

    protected Singleton()
    {
    }
}
