using System;

[Serializable]
public struct LightControllerEntry : IEquatable<LightControllerEntry>
{
    public int Type;
    public int ID;
    public BaseLightController Controller;

    public bool Equals(LightControllerEntry other)
    {
        return ID == other.ID
            && Equals(Controller, other.Controller);
    }

    public override bool Equals(object obj) => obj is LightControllerEntry other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(ID, Controller);
}
