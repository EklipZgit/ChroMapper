using System;
using UnityEngine;

[ExecuteAlways]
public class LightManager : MonoBehaviour
{
    private static readonly int directionalLightDirectionsID = Shader.PropertyToID("_DirectionalLightDirections");

    private static readonly int directionalLightPositionRadiiID =
        Shader.PropertyToID("_DirectionalLightPositionsRadii");

    private static readonly int directionalLightColorsID = Shader.PropertyToID("_DirectionalLightColors");
    private static readonly int pointLightPositionsID = Shader.PropertyToID("_PointLightPositions");
    private static readonly int pointLightColorsID = Shader.PropertyToID("_PointLightColors");

    private Vector4[] directionalLightDirections = new Vector4[5];
    private Vector4[] directionalLightColors = new Vector4[5];
    private Vector4[] directionalLightPositionRadii = new Vector4[5];
    private Vector4[] pointLightPositions = new Vector4[1];
    private Vector4[] pointLightColors = new Vector4[1];

    private int lastRefreshFrameNum = -1;

    private void OnEnable()
    {
        DirectionalLight.Lights ??= new();
        PointLight.Lights ??= new();
        Camera.onPreRender = (Camera.CameraCallback)Delegate.Combine(
            Camera.onPreRender,
            new Camera.CameraCallback(OnCameraPreRender));
    }

    private void OnDisable() =>
        Camera.onPreRender = (Camera.CameraCallback)Delegate.Remove(
            Camera.onPreRender,
            new Camera.CameraCallback(OnCameraPreRender));

    private void OnCameraPreRender(Camera currentCamera)
    {
        if (currentCamera.cullingMask != (currentCamera.cullingMask | (1 << gameObject.layer))
            || lastRefreshFrameNum == Time.frameCount)
            return;
        
        lastRefreshFrameNum = Time.frameCount;
        var dirLight = DirectionalLight.Lights;
        for (var i = 0; i < 5; i++)
        {
            if (i < dirLight.Count && dirLight[i].isActiveAndEnabled)
            {
                var directionalLight = dirLight[i];
                var tr = directionalLight.transform;
                directionalLightPositionRadii[i] = tr.position;
                directionalLightPositionRadii[i].w = directionalLight.Radius;
                directionalLightDirections[i] = -tr.forward;
                directionalLightColors[i] = (directionalLight.Color * directionalLight.Intensity).linear;
            }
            else
            {
                directionalLightColors[i] = new Color(0f, 0f, 0f, 0f);
                directionalLightPositionRadii[i].w = 100f;
            }
        }

        Shader.SetGlobalVectorArray(directionalLightDirectionsID, directionalLightDirections);
        Shader.SetGlobalVectorArray(directionalLightPositionRadiiID, directionalLightPositionRadii);
        Shader.SetGlobalVectorArray(directionalLightColorsID, directionalLightColors);
        var pLight = PointLight.Lights;
        for (var j = 0; j < 1; j++)
        {
            if (j < pLight.Count && pLight[j].isActiveAndEnabled)
            {
                var pointLight = pLight[j];
                pointLightPositions[j] = pointLight.transform.position;
                pointLightColors[j] = (pointLight.Color * pointLight.Intensity).linear;
            }
            else
                pointLightColors[j] = new Color(0f, 0f, 0f, 0f);
        }

        Shader.SetGlobalVectorArray(pointLightPositionsID, pointLightPositions);
        Shader.SetGlobalVectorArray(pointLightColorsID, pointLightColors);
    }

    protected void OnDestroy() => ResetColors();

    private void ResetColors()
    {
        for (var i = 0; i < 5; i++) directionalLightColors[i] = new Color(0f, 0f, 0f, 0f);
        for (var j = 0; j < 1; j++) pointLightColors[j] = new Color(0f, 0f, 0f, 0f);

        Shader.SetGlobalVectorArray(directionalLightDirectionsID, directionalLightDirections);
        Shader.SetGlobalVectorArray(directionalLightColorsID, directionalLightColors);
        Shader.SetGlobalVectorArray(pointLightColorsID, pointLightColors);
    }
}
