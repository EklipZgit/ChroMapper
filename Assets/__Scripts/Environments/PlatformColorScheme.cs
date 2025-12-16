using System;
using Beatmap.Enums;
using UnityEngine;

[Serializable]
public class PlatformColorScheme
{
    private bool boost;

    public Color LeftNoteColor = DefaultColors.LeftNote;
    public Color RightNoteColor = DefaultColors.RightNote;
    public Color ObstacleColor = DefaultColors.LeftNote;

    private Color environmentLeftColor = DefaultColors.Left;
    private Color environmentRightColor = DefaultColors.Right;
    private Color environmentWhiteColor = DefaultColors.White;

    private Color environmentLeftBoostColor = DefaultColors.Left;
    private Color environmentRightBoostColor = DefaultColors.Right;
    private Color environmentWhiteBoostColor = DefaultColors.White;

    public Color CurrentEnvironmentLeftColor = DefaultColors.Left;
    public Color CurrentEnvironmentRightColor = DefaultColors.Right;
    public Color CurrentEnvironmentWhiteColor = DefaultColors.White;

    public Color EnvironmentLeftColor
    {
        get => environmentLeftColor;
        set
        {
            environmentLeftColor = value;
            CurrentEnvironmentLeftColor = boost ? environmentLeftBoostColor : environmentLeftColor;
        }
    }

    public Color EnvironmentRightColor
    {
        get => environmentRightColor;
        set
        {
            environmentRightColor = value;
            CurrentEnvironmentRightColor = boost ? environmentRightBoostColor : environmentRightColor;
        }
    }

    public Color EnvironmentWhiteColor
    {
        get => environmentWhiteColor;
        set
        {
            environmentWhiteColor = value;
            CurrentEnvironmentWhiteColor = boost ? environmentWhiteBoostColor : environmentWhiteColor;
        }
    }

    public Color EnvironmentLeftBoostColor
    {
        get => environmentLeftBoostColor;
        set
        {
            environmentLeftBoostColor = value;
            CurrentEnvironmentLeftColor = boost ? environmentLeftBoostColor : environmentLeftColor;
        }
    }

    public Color EnvironmentRightBoostColor
    {
        get => environmentRightBoostColor;
        set
        {
            environmentRightBoostColor = value;
            CurrentEnvironmentRightColor = boost ? environmentRightBoostColor : environmentRightColor;
        }
    }

    public Color EnvironmentWhiteBoostColor
    {
        get => environmentWhiteBoostColor;
        set
        {
            environmentWhiteBoostColor = value;
            CurrentEnvironmentWhiteColor = boost ? environmentWhiteBoostColor : environmentWhiteColor;
        }
    }

    public PlatformColorScheme SwapEnvironmentColors(bool boosted)
    {
        boost = boosted;
        CurrentEnvironmentLeftColor = boosted ? EnvironmentLeftBoostColor : EnvironmentLeftColor;
        CurrentEnvironmentRightColor = boosted ? EnvironmentRightBoostColor : EnvironmentRightColor;
        CurrentEnvironmentWhiteColor = boosted ? EnvironmentWhiteBoostColor : EnvironmentWhiteColor;
        return this;
    }

    public PlatformColorScheme Copy(PlatformColorScheme copy)
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

    public PlatformColorScheme Clone() =>
        new()
        {
            LeftNoteColor = LeftNoteColor,
            RightNoteColor = RightNoteColor,
            ObstacleColor = ObstacleColor,
            EnvironmentLeftColor = EnvironmentLeftColor,
            EnvironmentRightColor = EnvironmentRightColor,
            EnvironmentWhiteColor = EnvironmentWhiteColor,
            EnvironmentLeftBoostColor = EnvironmentLeftBoostColor,
            EnvironmentRightBoostColor = EnvironmentRightBoostColor,
            EnvironmentWhiteBoostColor = EnvironmentWhiteBoostColor,
        };

    public Color GetColorFrom(LightColor value, bool invert)
    {
        return value switch
        {
            LightColor.Blue when invert => CurrentEnvironmentLeftColor,
            LightColor.Blue => CurrentEnvironmentRightColor,
            LightColor.Red when invert => CurrentEnvironmentRightColor,
            LightColor.Red => CurrentEnvironmentLeftColor,
            LightColor.White => CurrentEnvironmentWhiteColor,
            _ => Color.white
        };
    }
}
