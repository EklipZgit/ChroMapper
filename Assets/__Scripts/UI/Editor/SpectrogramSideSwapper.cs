using UnityEngine;
using UnityEngine.Serialization;

public class SpectrogramSideSwapper : MonoBehaviour
{
    [SerializeField] private GridLane spectrogramGridLane;
    private bool IsNoteSide { get; set; } = true;

    public void SwapSides()
    {
        IsNoteSide = !IsNoteSide;
        var order = IsNoteSide ? -1000 : 1000;
        spectrogramGridLane.Order = order;
    }
}
