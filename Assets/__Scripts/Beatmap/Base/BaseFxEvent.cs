using SimpleJSON;

namespace Beatmap.Base
{
    public abstract class BaseFxEvent<T> : BaseGLSEvent where T : struct
    {
        protected BaseFxEvent()
        {
        }

        protected BaseFxEvent(
            float time,
            T value,
            int usePrevious,
            JSONNode customData = null) : base(time, customData)
        {
            Value = value;
            UsePrevious = usePrevious;
        }

        protected BaseFxEvent(BaseFxEvent<T> other) : base(other)
        {
            Value = other.Value;
            UsePrevious = other.UsePrevious;
        }

        public int UsePrevious;
        public T Value;

        public override void Apply(BaseObject originalData)
        {
            base.Apply(originalData);

            if (originalData is not BaseFxEvent<T> other)
                return;

            Value = other.Value;
            UsePrevious = other.UsePrevious;
        }
    }
}
