/*     INFINITY CODE 2013-2019      */
/*   http://www.infinity-code.com   */

using System;
using UnityEngine;

#if TOUCHSCRIPT
using TouchScript.Gestures.TransformGestures;
#endif

[OnlineMapsPlugin("TouchScript Connector", typeof(OnlineMapsControlBase))]
public class OnlineMapsTouchScriptConnector : MonoBehaviour
{
#if TOUCHSCRIPT
    public ScreenTransformGesture gesture;

    private OnlineMapsControlBase control;
    private OnlineMapsCameraOrbit cameraOrbit;
    private Vector2 speed;

    private void Start()
    {
        control = OnlineMapsControlBase.instance;
        control.allowZoom = false;

        cameraOrbit = GetComponent<OnlineMapsCameraOrbit>();

        if (cameraOrbit != null)
        {
            speed = cameraOrbit.speed;
            cameraOrbit.speed = Vector2.zero;
        }

        gesture.Transformed += GestureOnTransformed;
    }

    private void GestureOnTransformed(object sender, EventArgs eventArgs)
    {
        if (gesture.NumPointers != 2) return;
        control.isMapDrag = false;
        float deltaScale = gesture.DeltaScale - 1;

        if (control.zoomMode == OnlineMapsZoomMode.center) OnlineMaps.instance.floatZoom += deltaScale;
        else control.ZoomOnPoint(deltaScale, gesture.ScreenPosition);

        if (cameraOrbit != null)
        {
            cameraOrbit.rotation.x += gesture.DeltaPosition.y * speed.x;
            cameraOrbit.rotation.y += gesture.DeltaRotation * speed.y;
        }
    }
#endif
}