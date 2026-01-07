namespace Editor.Environments.Structures.Components
{
    public class ParametricBoxControllerComponent
    {
        public float AlphaStart;
        public float AlphaEnd;
        public float AlphaMultiplier;
        public float Width;
        public float WidthStart;
        public float WidthEnd;
        public float Center;
        public float Height;
        public float Length;
        public float MinAlpha;

        public void CopyTo(ParametricBoxLight obj)
        {
            obj.AlphaStart = AlphaStart;
            obj.AlphaEnd = AlphaEnd;
            obj.AlphaMultiplier = AlphaMultiplier;
            obj.Width = Width;
            obj.WidthStart = WidthStart;
            obj.WidthEnd = WidthEnd;
            obj.Center = Center;
            obj.Height = Height;
            obj.Length = Length;
            obj.MinAlpha = MinAlpha;
        }
    }
}
