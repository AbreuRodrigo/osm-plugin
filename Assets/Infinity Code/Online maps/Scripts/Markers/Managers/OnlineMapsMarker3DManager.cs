/*     INFINITY CODE 2013-2019      */
/*   http://www.infinity-code.com   */

using System;
using UnityEngine;

/// <summary>
/// This component manages 3D markers.
/// </summary>
[Serializable]
[DisallowMultipleComponent]
public class OnlineMapsMarker3DManager : OnlineMapsMarkerManagerBase<OnlineMapsMarker3DManager, OnlineMapsMarker3D>
{
    /// <summary>
    /// Specifies whether to create a 3D marker by pressing N under the cursor.
    /// </summary>
    public bool allowAddMarker3DByN = true;

    /// <summary>
    /// Default 3D marker.
    /// </summary>
    public GameObject defaultPrefab;

    /// <summary>
    /// Scaling of 3D markers by default.
    /// </summary>
    public float defaultScale = 1;

    /// <summary>
    /// Create a new 3D marker
    /// </summary>
    /// <param name="longitude">Longitude</param>
    /// <param name="latitude">Latitude</param>
    /// <param name="prefab">Prefab</param>
    /// <returns>Instance of the marker</returns>
    public OnlineMapsMarker3D Create(double longitude, double latitude, GameObject prefab)
    {
        OnlineMapsMarker3D marker = _CreateItem(longitude, latitude);
        marker.prefab = prefab;
        OnlineMapsControlBase3D control = marker.control = OnlineMapsControlBase3D.instance;
        marker.scale = defaultScale;
        marker.Init(control.transform);
        Redraw();
        return marker;
    }

    /// <summary>
    /// Creates a new 3D marker from an existing GameObject in the scene.
    /// </summary>
    /// <param name="longitude">Longitude</param>
    /// <param name="latitude">Latitude</param>
    /// <param name="markerGameObject">GameObject in the scene</param>
    /// <returns>Instance of the marker</returns>
    public OnlineMapsMarker3D CreateFromExistGameObject(double longitude, double latitude, GameObject markerGameObject)
    {
        OnlineMapsMarker3D marker = _CreateItem(longitude, latitude);
        marker.prefab = marker.instance = markerGameObject;
        marker.control = OnlineMapsControlBase3D.instance;
        marker.scale = defaultScale;
        markerGameObject.AddComponent<OnlineMapsMarker3DInstance>().marker = marker;
        marker.inited = true;

        Update();

        if (marker.OnInitComplete != null) marker.OnInitComplete(marker);
        Redraw();
        return marker;
    }

    /// <summary>
    /// Create a new 3D marker
    /// </summary>
    /// <param name="location">Location of the marker (X - longitude, Y - latitude)</param>
    /// <param name="prefab">Prefab</param>
    /// <returns>Instance of the marker</returns>
    public static OnlineMapsMarker3D CreateItem(Vector2 location, GameObject prefab)
    {
        return instance.Create(location.x, location.y, prefab);
    }

    /// <summary>
    /// Create a new 3D marker
    /// </summary>
    /// <param name="lng">Longitude</param>
    /// <param name="lat">Latitude</param>
    /// <param name="prefab">Prefab</param>
    /// <returns>Instance of the marker</returns>
    public static OnlineMapsMarker3D CreateItem(double lng, double lat, GameObject prefab)
    {
        return instance.Create(lng, lat, prefab);
    }

    /// <summary>
    /// Creates a new 3D marker from an existing GameObject in the scene.
    /// </summary>
    /// <param name="longitude">Longitude</param>
    /// <param name="latitude">Latitude</param>
    /// <param name="markerGameObject">GameObject in the scene</param>
    /// <returns>Instance of the marker</returns>
    public static OnlineMapsMarker3D CreateItemFromExistGameObject(double longitude, double latitude, GameObject markerGameObject)
    {
        return instance.CreateFromExistGameObject(longitude, latitude, markerGameObject);
    }

    public override OnlineMapsSavableItem[] GetSavableItems()
    {
        if (savableItems != null) return savableItems;

        savableItems = new[]
        {
            new OnlineMapsSavableItem("markers3D", "3D Markers", SaveSettings)
            {
                priority = 90,
                loadCallback = LoadSettings
            }
        };

        return savableItems;
    }

    /// <summary>
    /// Load items and component settings from JSON
    /// </summary>
    /// <param name="json">JSON item</param>
    public void LoadSettings(OnlineMapsJSONItem json)
    {
        OnlineMapsJSONItem jitems = json["items"];
        RemoveAll();
        foreach (OnlineMapsJSONItem jitem in jitems)
        {
            OnlineMapsMarker3D marker = new OnlineMapsMarker3D();

            double mx = jitem.ChildValue<double>("longitude");
            double my = jitem.ChildValue<double>("latitude");

            marker.SetPosition(mx, my);

            marker.range = jitem.ChildValue<OnlineMapsRange>("range");
            marker.label = jitem.ChildValue<string>("label");
            marker.prefab = OnlineMapsUtils.GetObject(jitem.ChildValue<int>("prefab")) as GameObject;
            marker.rotationY = jitem.ChildValue<float>("rotationY");
            marker.scale = jitem.ChildValue<float>("scale");
            marker.enabled = jitem.ChildValue<bool>("enabled");
            Add(marker);
        }

        (json["settings"] as OnlineMapsJSONObject).DeserializeObject(this);
    }

    protected override OnlineMapsJSONItem SaveSettings()
    {
        OnlineMapsJSONItem jitem = base.SaveSettings();
        jitem["settings"].AppendObject(new
        {
            allowAddMarker3DByN,
            defaultPrefab = defaultPrefab != null? defaultPrefab.GetInstanceID(): -1,
            defaultScale
        });
        return jitem;
    }

    protected override void Update()
    {
        base.Update();

        if (allowAddMarker3DByN && Input.GetKeyUp(KeyCode.N))
        {
            OnlineMapsMarker3D marker3D = CreateItem(OnlineMapsControlBase.instance.GetCoords(), defaultPrefab);
            marker3D.scale = defaultScale;
        }
    }
}
 