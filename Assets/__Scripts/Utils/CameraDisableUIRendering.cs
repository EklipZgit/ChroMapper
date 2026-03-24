using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class CameraDisableUIRendering : MonoBehaviour
{
    private static readonly FieldInfo willRenderCanvases = typeof(Canvas).GetField("willRenderCanvases", BindingFlags.NonPublic | BindingFlags.Static);
    private static readonly FieldInfo layoutQueue = typeof(CanvasUpdateRegistry).GetField("m_LayoutRebuildQueue", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo graphicsQueue = typeof(CanvasUpdateRegistry).GetField("m_GraphicRebuildQueue", BindingFlags.NonPublic | BindingFlags.Instance);

    private static IList<ICanvasElement> layoutList;
    private static IList<ICanvasElement> graphicsList;

    private object canvasHackObject;

    private void OnPreRender()
    {
        // Clear willRenderCanvases and all layout/graphics queues
        canvasHackObject = willRenderCanvases.GetValue(null);
        willRenderCanvases.SetValue(null, null);

        layoutList ??= (IList<ICanvasElement>)layoutQueue.GetValue(CanvasUpdateRegistry.instance);
        layoutList.Clear();

        graphicsList ??= (IList<ICanvasElement>)graphicsQueue.GetValue(CanvasUpdateRegistry.instance);
        graphicsList.Clear();
    }

    private void OnPostRender()
    {
        // Restore the willRenderCanvases delegate so that canvases will render again after this method is called
        willRenderCanvases.SetValue(null, canvasHackObject);
    }
}
