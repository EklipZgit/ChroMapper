namespace Beatmap.Base
{
    public abstract class BaseFxEvent<T> : BaseObject where T : struct
    {
        public int UsePrevious;
        public T Value;
    }
}
