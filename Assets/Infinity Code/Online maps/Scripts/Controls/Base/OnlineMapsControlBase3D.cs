/*     INFINITY CODE 2013-2019      */
/*   http://www.infinity-code.com   */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Class implements the basic functionality control of the 3D map.
/// </summary>
[Serializable]
[RequireComponent(typeof(OnlineMapsMarker3DManager))]
public abstract class OnlineMapsControlBase3D: OnlineMapsControlBase
{
    #region Variables

    /// <summary>
    /// The camera you are using to display the map.
    /// </summary>
    public Camera activeCamera;

    /// <summary>
    /// Mode of 2D markers. Bake in texture or Billboard.
    /// </summary>
    public OnlineMapsMarker2DMode marker2DMode = OnlineMapsMarker2DMode.flat;

    /// <summary>
    /// Size of billboard markers.
    /// </summary>
    public float marker2DSize = 100;

    public Vector3 originalPosition;
    public Vector3 originalScale;

    private Collider _cl;
    private OnlineMapsMarker3DDrawer _marker3DDrawer;
    private Renderer _renderer;

    #endregion

    #region Properties

    /// <summary>
    /// Singleton instance of OnlineMapsControlBase3D control.
    /// </summary>
    public new static OnlineMapsControlBase3D instance
    {
        get { return OnlineMapsControlBase.instance as OnlineMapsControlBase3D; }
    }

    /// <summary>
    /// Reference to the collider.
    /// </summary>
    public Collider cl
    {
        get
        {
            if (_cl == null) _cl = GetComponent<Collider>();
            return _cl;
        }
    }

    public OnlineMapsMarker3DDrawer marker3DDrawer
    {
        get { return _marker3DDrawer; }
        set
        {
            if (_marker3DDrawer != null) _marker3DDrawer.Dispose();
            _marker3DDrawer = value;
        }
    }

    /// <summary>
    /// Reference to the renderer.
    /// </summary>
    public Renderer rendererInstance
    {
        get
        {
            if (_renderer == null) _renderer = GetComponent<Renderer>();
            return _renderer;
        }
    }

    #endregion

    #region Methods

    protected override void AfterUpdate()
    {
        base.AfterUpdate();

        Vector2 inputPosition = GetInputPosition();

        if (map.showMarkerTooltip == OnlineMapsShowMarkerTooltip.onHover)
        {
            OnlineMapsMarkerInstanceBase markerInstance = GetBillboardMarkerFromScreen(inputPosition);
            if (markerInstance != null)
            {
                OnlineMapsTooltipDrawerBase.tooltip = markerInstance.marker.label;
                OnlineMapsTooltipDrawerBase.tooltipMarker = markerInstance.marker;
            }
        }
    }

    /// <summary>
    /// Gets billboard marker on the screen position.
    /// </summary>
    /// <param name="screenPosition">Screen position.</param>
    /// <returns>Marker instance or null.</returns>
    public OnlineMapsMarkerInstanceBase GetBillboardMarkerFromScreen(Vector2 screenPosition)
    {
        //TODO: Find a way to refactory this method
        RaycastHit hit;
        if (Physics.Raycast(activeCamera.ScreenPointToRay(screenPosition), out hit, OnlineMapsUtils.maxRaycastDistance))
        {
            return hit.collider.gameObject.GetComponent<OnlineMapsMarkerInstanceBase>();
        }
        return null;
    }

    public override IOnlineMapsInteractiveElement GetInteractiveElement(Vector2 screenPosition)
    {
        if (IsCursorOnUIElement(screenPosition)) return null;

        //TODO: Find a way to refactory this method
        RaycastHit hit;
        if (Physics.Raycast(activeCamera.ScreenPointToRay(screenPosition), out hit, OnlineMapsUtils.maxRaycastDistance))
        {
            OnlineMapsMarkerInstanceBase markerInstance = hit.collider.gameObject.GetComponent<OnlineMapsMarkerInstanceBase>();
            if (markerInstance != null) return markerInstance.marker;
        }

        OnlineMapsMarker marker = markerDrawer.GetMarkerFromScreen(screenPosition);
        if (marker != null) return marker;

        OnlineMapsDrawingElement drawingElement = map.GetDrawingElement(screenPosition);
        return drawingElement;
    }

    public override Vector2 GetScreenPosition(double lng, double lat)
    {
        double px, py;
        GetPosition(lng, lat, out px, out py);
        px /= map.width;
        py /= map.height;

        Bounds bounds = cl.bounds;
        Vector3 worldPos = new Vector3(
            (float)(bounds.max.x - bounds.size.x * px),
            bounds.min.y,
            (float)(bounds.min.z + bounds.size.z * py)
        );

        Camera cam = activeCamera ?? Camera.main;
        return cam.WorldToScreenPoint(worldPos);
    }

    protected override void OnDestroyLate()
    {
        base.OnDestroyLate();

        marker3DDrawer = null;
    }

    protected override void OnEnableLate()
    {
        base.OnEnableLate();

        OnlineMapsMarker3DManager.Init();
        marker3DDrawer = new OnlineMapsMarker3DDrawer(this);
        if (activeCamera == null) activeCamera = Camera.main;
    }

    protected override OnlineMapsJSONItem SaveSettings()
    {
        OnlineMapsJSONItem json = base.SaveSettings();
        json.AppendObject(new
        {
            marker2DMode,
            marker2DSize,
            activeCamera
        });

        return json;
    }

    private void Start()
    {
        if (OnlineMapsMarker3DManager.instance != null)
        {
            foreach (OnlineMapsMarker3D marker in OnlineMapsMarker3DManager.instance.Where(m => !m.inited))
            {
                marker.control = this;
                marker.Init(transform);
            }
            if (OnUpdate3DMarkers != null) OnUpdate3DMarkers();
        }
    }

    /// <summary>
    /// Updates the current control.
    /// </summary>
    public virtual void UpdateControl()
    {
        if (OnDrawMarkers != null) OnDrawMarkers();
        if (OnUpdate3DMarkers != null) OnUpdate3DMarkers();
    }

    #endregion

    #region Obsolete

    [Obsolete]
    public Func<double, double, GameObject, OnlineMapsMarker3D> OnAddMarker3D;

    [Obsolete("Use OnlineMapsCameraOrbit.OnCameraControl")]
    public Action OnCameraControl
    {
        get { return OnlineMapsCameraOrbit.instance.OnCameraControl; }
        set { OnlineMapsCameraOrbit.instance.OnCameraControl = value; }
    }

    [Obsolete]
    public Action<OnlineMapsMarker3D> OnMarker3DAdded;

    [Obsolete]
    public Predicate<OnlineMapsMarker3D> OnRemoveMarker3D;

    [Obsolete]
    public Predicate<int> OnRemoveMarker3DAt;

    public Action OnUpdate3DMarkers;

    [Obsolete("Use OnlineMapsCameraOrbit")]
    public bool allowCameraControl
    {
        get { return OnlineMapsCameraOrbit.instance != null && OnlineMapsCameraOrbit.instance.enabled; }
        set
        {
            if (OnlineMapsCameraOrbit.instance != null) OnlineMapsCameraOrbit.instance.enabled = value;
            else gameObject.AddComponent<OnlineMapsCameraOrbit>();
        }
    }

    [Obsolete("Use OnlineMapsCameraOrbit.adjustTo")]
    public OnlineMapsCameraAdjust cameraAdjustTo
    {
        get { return OnlineMapsCameraOrbit.instance.adjustTo; }
        set { OnlineMapsCameraOrbit.instance.adjustTo = value; }
    }

    [Obsolete("Use OnlineMapsCameraOrbit.distance")]
    public float cameraDistance
    {
        get { return OnlineMapsCameraOrbit.instance.distance; }
        set { OnlineMapsCameraOrbit.instance.distance = value; }
    }

    [Obsolete("Use OnlineMapsCameraOrbit.rotation")]
    public Vector2 cameraRotation
    {
        get { return OnlineMapsCameraOrbit.instance.rotation; }
        set { OnlineMapsCameraOrbit.instance.rotation = value; }
    }

    [Obsolete("Use OnlineMapsCameraOrbit.speed")]
    public Vector2 cameraSpeed
    {
        get { return OnlineMapsCameraOrbit.instance.speed; }
        set { OnlineMapsCameraOrbit.instance.speed = value; }
    }

    [Obsolete("Use OnlineMapsCameraOrbit.maxRotationX")]
    public float maxCameraRotationX
    {
        get { return OnlineMapsCameraOrbit.instance.maxRotationX; }
        set { OnlineMapsCameraOrbit.instance.maxRotationX = value; }
    }

    [Obsolete("Use OnlineMapsMarker3DManager")]
    public OnlineMapsMarker3D[] markers3D
    {
        get { return OnlineMapsMarker3DManager.instance.ToArray(); }
        set { OnlineMapsMarker3DManager.SetItems(value); }
    }

    [Obsolete("Use OnlineMapsMarker3DManager.CreateItem")]
    public OnlineMapsMarker3D AddMarker3D(Vector2 markerPosition, GameObject prefab)
    {
        return AddMarker3D(markerPosition.x, markerPosition.y, prefab);
    }

    [Obsolete("Use OnlineMapsMarker3DManager.CreateItem")]
    public OnlineMapsMarker3D AddMarker3D(double markerLng, double markerLat, GameObject prefab)
    {
        OnlineMapsMarker3D marker;

        if (OnAddMarker3D != null)
        {
            marker = OnAddMarker3D(markerLng, markerLat, prefab);
            if (marker != null) return marker;
        }

        marker = OnlineMapsMarker3DManager.CreateItem(markerLng, markerLat, prefab);

        if (OnMarker3DAdded != null) OnMarker3DAdded(marker);

        return marker;
    }

    [Obsolete("Use OnlineMapsMarker3DManager.AddItem")]
    public OnlineMapsMarker3D AddMarker3D(OnlineMapsMarker3D marker)
    {
        OnlineMapsMarker3DManager.AddItem(marker);

        if (OnMarker3DAdded != null) OnMarker3DAdded(marker);
        return marker;
    }

    [Obsolete("Use OnlineMapsElevationManagerBase.GetBestElevationYScale")]
    public float GetBestElevationYScale(Vector2 topLeftPosition, Vector2 bottomRightPosition)
    {
        return OnlineMapsElevationManagerBase.GetBestElevationYScale(topLeftPosition.x, topLeftPosition.y, bottomRightPosition.x, bottomRightPosition.y);
    }

    [Obsolete("Use OnlineMapsElevationManagerBase.GetBestElevationYScale")]
    public float GetBestElevationYScale(double tlx, double tly, double brx, double bry)
    {
        return OnlineMapsElevationManagerBase.GetBestElevationYScale(tlx, tly, brx, bry);
    }

    [Obsolete("Use OnlineMapsElevationManagerBase.GetElevation")]
    public float GetElevationValue(double x, double z, float yScale, Vector2 topLeftPosition, Vector2 bottomRightPosition)
    {
        return OnlineMapsElevationManagerBase.GetElevation(x, z, yScale, topLeftPosition.x, topLeftPosition.y, bottomRightPosition.x, bottomRightPosition.y);
    }

    [Obsolete("Use OnlineMapsElevationManagerBase.GetElevation")]
    public float GetElevationValue(double x, double z, float yScale, double tlx, double tly, double brx, double bry)
    {
        return OnlineMapsElevationManagerBase.GetElevation(x, z, yScale, tlx, tly, brx, bry);
    }

    [Obsolete("Use OnlineMapsMarker3DManager.RemoveAllItems")]
    public void RemoveAllMarker3D()
    {
        OnlineMapsMarker3DManager.RemoveAllItems();
    }

    [Obsolete("Use OnlineMapsMarker3DManager.RemoveItem")]
    public void RemoveMarker3D(OnlineMapsMarker3D marker)
    {
        OnlineMapsMarker3DManager.RemoveItem(marker);
    }

    [Obsolete("Use OnlineMapsMarker3DManager.RemoveItemAt")]
    public void RemoveMarker3DAt(int markerIndex)
    {
        if (OnRemoveMarker3DAt != null && OnRemoveMarker3DAt(markerIndex)) return;

        if (markerIndex < 0 || markerIndex >= OnlineMapsMarker3DManager.CountItems) return;

        OnlineMapsMarker3D marker = OnlineMapsMarker3DManager.instance[markerIndex];
        if (marker.instance != null) OnlineMapsUtils.Destroy(marker.instance);
        marker.Dispose();

        OnlineMapsMarker3DManager.RemoveItemAt(markerIndex);
    }

    [Obsolete]
    public void RemoveMarkers3DByTag(params string[] tags)
    {
        if (tags.Length == 0) return;

        OnlineMapsMarker3DManager.RemoveAllItems(m =>
        {
            if (m.tags == null || m.tags.Count == 0) return false;
            for (int j = 0; j < tags.Length; j++) if (m.tags.Contains(tags[j])) return true;
            return false;
        });
    }

    #endregion
}