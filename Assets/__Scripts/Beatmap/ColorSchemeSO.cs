using Beatmap.Enums;
using UnityEngine;

public class ColorSchemeSO : ScriptableObject
{
    private bool boost;

    public Color LeftNoteColor = DefaultColors.LeftNote;
    public Color RightNoteColor = DefaultColors.RightNote;
    public Color ObstacleColor = DefaultColors.LeftNote;

    [SerializeField] private Color environmentLeftColor = DefaultColors.Left;
    [SerializeField] private Color environmentRightColor = DefaultColors.Right;
    [SerializeField] private Color environmentWhiteColor = DefaultColors.White;

    [SerializeField] private Color environmentLeftBoostColor = DefaultColors.Left;
    [SerializeField] private Color environmentRightBoostColor = DefaultColors.Right;
    [SerializeField] private Color environmentWhiteBoostColor = DefaultColors.White;

    private Color currentEnvironmentLeftColor = DefaultColors.Left;
    private Color currentEnvironmentRightColor = DefaultColors.Right;
    private Color currentEnvironmentWhiteColor = DefaultColors.White;

    public Color EnvironmentLeftColor
    {
        get => environmentLeftColor;
        set
        {
            environmentLeftColor = value;
            currentEnvironmentLeftColor = boost ? environmentLeftBoostColor : environmentLeftColor;
        }
    }

    public Color EnvironmentRightColor
    {
        get => environmentRightColor;
        set
        {
            environmentRightColor = value;
            currentEnvironmentRightColor = boost ? environmentRightBoostColor : environmentRightColor;
        }
    }

    public Color EnvironmentWhiteColor
    {
        get => environmentWhiteColor;
        set
        {
            environmentWhiteColor = value;
            currentEnvironmentWhiteColor = boost ? environmentWhiteBoostColor : environmentWhiteColor;
        }
    }

    public Color EnvironmentLeftBoostColor
    {
        get => environmentLeftBoostColor;
        set
        {
            environmentLeftBoostColor = value;
            currentEnvironmentLeftColor = boost ? environmentLeftBoostColor : environmentLeftColor;
        }
    }

    public Color EnvironmentRightBoostColor
    {
        get => environmentRightBoostColor;
        set
        {
            environmentRightBoostColor = value;
            currentEnvironmentRightColor = boost ? environmentRightBoostColor : environmentRightColor;
        }
    }

    public Color EnvironmentWhiteBoostColor
    {
        get => environmentWhiteBoostColor;
        set
        {
            environmentWhiteBoostColor = value;
            currentEnvironmentWhiteColor = boost ? environmentWhiteBoostColor : environmentWhiteColor;
        }
    }

    public ColorSchemeSO SwapEnvironmentColors(bool boosted)
    {
        boost = boosted;
        currentEnvironmentLeftColor = boosted ? EnvironmentLeftBoostColor : EnvironmentLeftColor;
        currentEnvironmentRightColor = boosted ? EnvironmentRightBoostColor : EnvironmentRightColor;
        currentEnvironmentWhiteColor = boosted ? EnvironmentWhiteBoostColor : EnvironmentWhiteColor;
        return this;
    }

    public ColorSchemeSO Copy(ColorSchemeSO copy)
    {
        LeftNoteColor = copy.LeftNoteColor;
        RightNoteColor = copy.RightNoteColor;
        ObstacleColor = copy.ObstacleColor;
        EnvironmentLeftColor = copy.EnvironmentLeftColor;
        EnvironmentRightColor = copy.EnvironmentRightColor;
        EnvironmentWhiteColor = copy.EnvironmentWhiteColor;
        EnvironmentLeftBoostColor = copy.EnvironmentLeftBoostColor;
        EnvironmentRightBoostColor = copy.EnvironmentRightBoostColor;
        EnvironmentWhiteBoostColor = copy.EnvironmentWhiteBoostColor;
        return this;
    }

    public ColorSchemeSO Clone() => CreateInstance<ColorSchemeSO>().Copy(this);

    public Color GetColorFrom(LightColor value, bool invert)
    {
        return value switch
        {
            LightColor.Blue when invert => currentEnvironmentLeftColor,
            LightColor.Blue => currentEnvironmentRightColor,
            LightColor.Red when invert => currentEnvironmentRightColor,
            LightColor.Red => currentEnvironmentLeftColor,
            LightColor.White => currentEnvironmentWhiteColor,
            _ => Color.white
        };
    }
}
