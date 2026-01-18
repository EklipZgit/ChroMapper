using System.Collections.Generic;
using UnityEngine;

public sealed class BloomFogObject : MonoBehaviour
{
    public static List<BloomFogObject> AllBloomFogLights = new();

    public float Length = 1f;
    public float Center = 1f;

    public float StartWidth = 1f;
    public float EndWidth = 1f;

    public float StartAlpha = 1f;
    public float EndAlpha = 1f;

    public float LightWidthMultiplier = 1f;
    public float IntensityMultiplier = 1f;

    public float BoostToWhite;
    public float LimitAlpha;
    public float MinAlpha;
    public float MaxAlpha = 1f;

    public bool UseCollision;

    public Transform CachedTransform;
    private Color color;

    private void OnEnable() => AllBloomFogLights.Add(this);

    private void OnDisable() => AllBloomFogLights.Remove(this);

    public void SetColor(Color col) => color = col;

    public void ApplyToQuad(int quadNum, BloomfogQuad[] quads, Matrix4x4 view, Matrix4x4 projection, float lineWidth)
    {
        // Get current quad
        ref var quad = ref quads[quadNum];

        if (color.a <= 0.001f)
        {
            ZeroQuad(ref quad);
            return;
        }

        // Calculate tube start/end based on center and length
        // TODO(Caeden): Collision
        var tubeLength = Length;
        var tubeStartLocalY = -tubeLength * Center;
        var tubeEndLocalY = tubeLength * (1f - Center);

        // Calculate endpoints in world space
        var localToWorld = CachedTransform.localToWorldMatrix;
        var tubeStartWorld = localToWorld.MultiplyPoint3x4(new(0f, tubeStartLocalY, 0f));
        var tubeEndWorld = localToWorld.MultiplyPoint3x4(new(0f, tubeEndLocalY, 0f));

        // Transform to view space
        var tubeStartView = view.MultiplyPoint3x4(tubeStartWorld);
        var tubeEndView = view.MultiplyPoint3x4(tubeEndWorld);

        // Transform to clip space
        var tubeStartClip = projection * new Vector4(tubeStartView.x, tubeStartView.y, tubeStartView.z, 1);
        var tubeEndClip = projection * new Vector4(tubeEndView.x, tubeEndView.y, tubeEndView.z, 1);

        #region Frustrum Culling

        // Left frustrum
        var startPointInsideFrustrum = tubeStartClip.x >= -tubeStartClip.w;
        var endPointInsideFrustrum = tubeEndClip.x >= -tubeEndClip.w;
        if (!startPointInsideFrustrum && !endPointInsideFrustrum)
        {
            ZeroQuad(ref quad);
            return;
        }

        if (startPointInsideFrustrum != endPointInsideFrustrum)
        {
            var leftFrustumClipInterpolation = (-tubeStartClip.w - tubeStartClip.x)
                / (tubeEndClip.x - tubeStartClip.x + tubeEndClip.w - tubeStartClip.w);
            ClipPoints(
                ref tubeStartClip,
                ref tubeEndClip,
                ref tubeStartView,
                ref tubeEndView,
                startPointInsideFrustrum,
                leftFrustumClipInterpolation);
        }

        // Right frustrum
        startPointInsideFrustrum = tubeStartClip.x <= tubeStartClip.w;
        endPointInsideFrustrum = tubeEndClip.x <= tubeEndClip.w;
        if (!startPointInsideFrustrum && !endPointInsideFrustrum)
        {
            ZeroQuad(ref quad);
            return;
        }

        if (startPointInsideFrustrum != endPointInsideFrustrum)
        {
            var rightFrustumClipInterpolation = (tubeStartClip.w - tubeStartClip.x)
                / (tubeEndClip.x - tubeStartClip.x - tubeEndClip.w + tubeStartClip.w);
            ClipPoints(
                ref tubeStartClip,
                ref tubeEndClip,
                ref tubeStartView,
                ref tubeEndView,
                startPointInsideFrustrum,
                rightFrustumClipInterpolation);
        }

        // Bottom frustrum
        startPointInsideFrustrum = tubeStartClip.y >= -tubeStartClip.w;
        endPointInsideFrustrum = tubeEndClip.y >= -tubeEndClip.w;
        if (!startPointInsideFrustrum && !endPointInsideFrustrum)
        {
            ZeroQuad(ref quad);
            return;
        }

        if (startPointInsideFrustrum != endPointInsideFrustrum)
        {
            var bottomFrustumClipInterpolation = (-tubeStartClip.w - tubeStartClip.y)
                / (tubeEndClip.y - tubeStartClip.y + tubeEndClip.w - tubeStartClip.w);
            ClipPoints(
                ref tubeStartClip,
                ref tubeEndClip,
                ref tubeStartView,
                ref tubeEndView,
                startPointInsideFrustrum,
                bottomFrustumClipInterpolation);
        }

        // Top frustrum
        startPointInsideFrustrum = tubeStartClip.y <= tubeStartClip.w;
        endPointInsideFrustrum = tubeEndClip.y <= tubeEndClip.w;
        if (!startPointInsideFrustrum && !endPointInsideFrustrum)
        {
            ZeroQuad(ref quad);
            return;
        }

        if (startPointInsideFrustrum != endPointInsideFrustrum)
        {
            var topFrustumClipInterpolation = (tubeStartClip.w - tubeStartClip.y)
                / (tubeEndClip.y - tubeStartClip.y - tubeEndClip.w + tubeStartClip.w);
            ClipPoints(
                ref tubeStartClip,
                ref tubeEndClip,
                ref tubeStartView,
                ref tubeEndView,
                startPointInsideFrustrum,
                topFrustumClipInterpolation);
        }

        // Far plane
        startPointInsideFrustrum = tubeStartClip.z <= tubeStartClip.w;
        endPointInsideFrustrum = tubeEndClip.z <= tubeEndClip.w;
        if (!startPointInsideFrustrum && !endPointInsideFrustrum)
        {
            ZeroQuad(ref quad);
            return;
        }

        if (startPointInsideFrustrum != endPointInsideFrustrum)
        {
            var farPlaneClipInterpolation = (tubeStartClip.w - tubeStartClip.z)
                / (tubeEndClip.z - tubeStartClip.z - tubeEndClip.w + tubeStartClip.w);
            ClipPoints(
                ref tubeStartClip,
                ref tubeEndClip,
                ref tubeStartView,
                ref tubeEndView,
                startPointInsideFrustrum,
                farPlaneClipInterpolation);
        }

        // Near plane (with small epsilon for precision)
        startPointInsideFrustrum = tubeStartClip.z >= -tubeStartClip.w - 0.0001f;
        endPointInsideFrustrum = tubeEndClip.z >= -tubeEndClip.w - 0.0001f;
        if (!startPointInsideFrustrum && !endPointInsideFrustrum)
        {
            ZeroQuad(ref quad);
            return;
        }

        if (startPointInsideFrustrum != endPointInsideFrustrum)
        {
            var nearPlaneClipInterpolation = (-tubeStartClip.w - tubeStartClip.z)
                / (tubeEndClip.z - tubeStartClip.z + tubeEndClip.w - tubeStartClip.w);
            ClipPoints(
                ref tubeStartClip,
                ref tubeEndClip,
                ref tubeStartView,
                ref tubeEndView,
                startPointInsideFrustrum,
                nearPlaneClipInterpolation);
        }

        #endregion

        // Convert to NDC space
        var tubeStartScreenX = (tubeStartClip.x / tubeStartClip.w * 0.5f) + 0.5f;
        var tubeStartScreenY = (tubeStartClip.y / tubeStartClip.w * 0.5f) + 0.5f;
        var tubeEndScreenX = (tubeEndClip.x / tubeEndClip.w * 0.5f) + 0.5f;
        var tubeEndScreenY = (tubeEndClip.y / tubeEndClip.w * 0.5f) + 0.5f;

        // Calculate screen space direction
        var screenDirX = tubeEndScreenX - tubeStartScreenX;
        var screenDirY = tubeEndScreenY - tubeStartScreenY;
        var screenDirLength = Mathf.Sqrt((screenDirX * screenDirX) + (screenDirY * screenDirY));

        // Prevent division by zero
        if (screenDirLength == 0) screenDirLength = 1E-06f;

        // Normalize direction
        screenDirX /= screenDirLength;
        screenDirY /= screenDirLength;

        // Apply anti-aliasing offset
        var screenOffsetX = screenDirX * (1f / 64);
        var screenOffsetY = screenDirY * (1f / 64);
        tubeEndScreenX += screenOffsetX;
        tubeEndScreenY += screenOffsetY;
        tubeStartScreenX -= screenOffsetX;
        tubeStartScreenY -= screenOffsetY;

        // Calculate perpendicular direction
        var effectiveLineWidth = lineWidth * LightWidthMultiplier;
        var perpX = -screenDirY * effectiveLineWidth;
        var perpY = screenDirX * effectiveLineWidth;

        // Calculate width offsets at endpoints
        // TODO(Caeden): Start/end widths
        var startWidthOffsetX = perpX * StartWidth;
        var startWidthOffsetY = perpY * StartWidth;
        var endWidthOffsetX = perpX * EndWidth;
        var endWidthOffsetY = perpY * EndWidth;

        // Calculate color components
        var boostedR = color.r + BoostToWhite;
        var boostedG = color.g + BoostToWhite;
        var boostedB = color.b + BoostToWhite;
        var finalAlpha = color.a * IntensityMultiplier;

        if (!Mathf.Approximately(LimitAlpha, 0f))
        {
            finalAlpha = Mathf.Clamp(finalAlpha, MinAlpha, MaxAlpha);
        }

        finalAlpha = Mathf.LinearToGammaSpace(finalAlpha);

        // Calculate vertex colors
        // TODO(Caeden): Collision
        var startColor = new Color(
            StartAlpha * boostedR,
            StartAlpha * boostedG,
            StartAlpha * boostedB,
            StartAlpha * finalAlpha);
        var endColor = new Color(
            EndAlpha * boostedR,
            EndAlpha * boostedG,
            EndAlpha * boostedB,
            EndAlpha * finalAlpha);

        // Fill quad data
        quad.Vertex0Position.x = tubeStartScreenX - startWidthOffsetX;
        quad.Vertex0Position.y = tubeStartScreenY - startWidthOffsetY;
        quad.Vertex0Position.z = 0;
        quad.Vertex0ViewPos = tubeStartView;
        quad.Vertex0Color = startColor;
        quad.Vertex0UV = new Vector3(0, 0, StartWidth);

        quad.Vertex1Position.x = tubeStartScreenX + startWidthOffsetX;
        quad.Vertex1Position.y = tubeStartScreenY + startWidthOffsetY;
        quad.Vertex1Position.z = 0;
        quad.Vertex1ViewPos = tubeStartView;
        quad.Vertex1Color = startColor;
        quad.Vertex1UV = new Vector3(StartWidth, 0, StartWidth);

        quad.Vertex2Position.x = tubeEndScreenX + endWidthOffsetX;
        quad.Vertex2Position.y = tubeEndScreenY + endWidthOffsetY;
        quad.Vertex2Position.z = 0;
        quad.Vertex2ViewPos = tubeEndView;
        quad.Vertex2Color = endColor;
        quad.Vertex2UV = new Vector3(EndWidth, 1, EndWidth);

        quad.Vertex3Position.x = tubeEndScreenX - endWidthOffsetX;
        quad.Vertex3Position.y = tubeEndScreenY - endWidthOffsetY;
        quad.Vertex3Position.z = 0;
        quad.Vertex3ViewPos = tubeEndView;
        quad.Vertex3Color = endColor;
        quad.Vertex3UV = new Vector3(0, 1, EndWidth);
    }

    /*private void OnDrawGizmosSelected()
    {
        Debug.Log(color);
    }*/

    private static void ZeroQuad(ref BloomfogQuad quad) => quad = default;

    // Clip the line segment against a single frustum plane
    private static void ClipPoints(
        ref Vector4 startClipPos,
        ref Vector4 endClipPos,
        ref Vector3 startViewPos,
        ref Vector3 endViewPos,
        bool startPointInsideFrustrum,
        float clipInterpolation)
    {
        if (startPointInsideFrustrum)
        {
            // Start point is inside, end point is outside - clip the end point
            endClipPos = Vector4.Lerp(startClipPos, endClipPos, clipInterpolation);
            endViewPos = Vector3.Lerp(startViewPos, endViewPos, clipInterpolation);
        }
        else
        {
            // End point is inside, start point is outside - clip the start point
            startClipPos = Vector4.Lerp(startClipPos, endClipPos, clipInterpolation);
            startViewPos = Vector3.Lerp(startViewPos, endViewPos, clipInterpolation);
        }
    }
}
