using System.Linq;

public class MaterialPropertyValuesSetterComponent
{
    public string MaterialPropertyBlockController;
    public PropertyNameFloatValuePair[] Floats;
    public PropertyNameVectorValuePair[] Vectors;
    public PropertyNameColorValuePair[] Colors;
    public PropertyNameIntValuePair[] Ints;

    public void CopyTo(MaterialPropertyValuesSetter target)
    {
        target.Floats = Floats
            .Select(x =>
                new MaterialPropertyValuesSetter.PropertyNameFloatValuePair
                {
                    PropertyName = x.PropertyName, Value = x.Value
                })
            .ToArray();
        target.Vectors = Vectors
            .Select(x =>
                new MaterialPropertyValuesSetter.PropertyNameVectorValuePair
                {
                    PropertyName = x.PropertyName, Vector = ConvertUtils.ToVector4(x.Vector)
                })
            .ToArray();
        target.Colors = Colors
            .Select(x =>
                new MaterialPropertyValuesSetter.PropertyNameColorValuePair
                {
                    PropertyName = x.PropertyName, Color = ConvertUtils.ToColor(x.Color)
                })
            .ToArray();
        target.Ints = Ints
            .Select(x =>
                new MaterialPropertyValuesSetter.PropertyNameIntValuePair
                {
                    PropertyName = x.PropertyName, Value = x.Value
                })
            .ToArray();
    }
}

public class PropertyValuePairBase
{
    public string PropertyName;
}

public class PropertyNameFloatValuePair : PropertyValuePairBase
{
    public float Value;
}

public class PropertyNameIntValuePair : PropertyValuePairBase
{
    public int Value;
}

public class PropertyNameVectorValuePair : PropertyValuePairBase
{
    public float[] Vector;
}

public class PropertyNameColorValuePair : PropertyValuePairBase
{
    public float[] Color;
}
