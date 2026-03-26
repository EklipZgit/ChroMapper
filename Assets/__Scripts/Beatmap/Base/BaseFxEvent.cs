namespace Beatmap.Base
{
    public abstract class BaseFxEvent<T> : BaseGLSEvent where T : struct
    {
        public int UsePrevious;
        public T Value;
    }
}
