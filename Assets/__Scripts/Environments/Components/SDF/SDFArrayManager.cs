using System;
using UnityEngine;

[ExecuteAlways]
public class SDFArrayManager : MonoBehaviour
{
    [SerializeField] public SDFPoint[] SDFPointArray = Array.Empty<SDFPoint>();

    private Vector4[] sdfArrayValues;
    private bool isInitialized;

    private static readonly int sdfPointsArray = Shader.PropertyToID("_SDFPointArray");

    protected void Awake() => InitIfNeeded();

    private void InitIfNeeded()
    {
        if (isInitialized) return;
        isInitialized = true;
        sdfArrayValues = new Vector4[SDFPointArray.Length];
    }

    protected void Update()
    {
        InitIfNeeded();
        for (var i = 0; i < SDFPointArray.Length; i++)
        {
            var position = SDFPointArray[i].transform.position;
            sdfArrayValues[i] = new Vector4(position.x, position.y, position.z, SDFPointArray[i].SqrtRadius);
        }

        Shader.SetGlobalVectorArray(sdfPointsArray, sdfArrayValues);
    }
}
