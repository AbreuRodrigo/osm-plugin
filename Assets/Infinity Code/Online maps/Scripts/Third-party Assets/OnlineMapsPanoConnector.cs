/*     INFINITY CODE 2013-2019      */
/*   http://www.infinity-code.com   */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UPANO
using InfinityCode.uPano.Controls;
using InfinityCode.uPano.Plugins;
using InfinityCode.uPano.Renderers;
using InfinityCode.uPano.Requests;
#endif

/// <summary>
/// Plugin for displaying Google Street View panoramas using uPano.
/// </summary>
public class OnlineMapsPanoConnector : MonoBehaviour
{
#if UPANO
    /// <summary>
    /// Google API key
    /// </summary>
    public string googleApiKey = "";

    /// <summary>
    /// The minimum zoom for which overlay will be displayed.
    /// </summary>
    public int minZoom = 10;

    /// <summary>
    /// Prefab of UI button to close the panorama
    /// </summary>
    [Header("Panorama close settings")]
    public GameObject closeButtonPrefab;

    /// <summary>
    /// Hot key to close the panorama
    /// </summary>
    public KeyCode closeByKeyCode = KeyCode.Escape;

    /// <summary>
    /// Radius of the panorama sphere
    /// </summary>
    [Header("Panorama mesh settings")]
    public int radius = 10;

    /// <summary>
    /// Number of segments of the panorama sphere
    /// </summary>
    public int segments = 32;

    private OnlineMaps map;
    private Dictionary<ulong, Texture2D> overlays;
    private SphericalPanoRenderer panoRenderer;
    private GameObject closeButtonInstance;

    private void OnCloseButtonClick()
    {
        Destroy(panoRenderer.gameObject);
        panoRenderer = null;

        Destroy(closeButtonInstance);
        closeButtonInstance = null;
    }

    private void OnDisable()
    {
        foreach (OnlineMapsTile tile in OnlineMapsTile.tiles) tile.overlayFrontTexture = null;

        if (map != null) map.Redraw();
    }

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(googleApiKey))
        {
            Debug.LogWarning("Please specify Online Maps Pano Connector/Google Api Key!!!");
            Destroy(this);
            return;
        }

        if (overlays == null) overlays = new Dictionary<ulong, Texture2D>();

        foreach (KeyValuePair<ulong, Texture2D> pair in overlays)
        {
            if (!OnlineMapsTile.dTiles.ContainsKey(pair.Key)) continue;
            OnlineMapsTile.dTiles[pair.Key].overlayFrontTexture = pair.Value;
        }

        if (map != null) map.Redraw();
    }

    private void OnGoogleStreetViewSuccess(GoogleStreetViewRequest request)
    {
        if (panoRenderer == null)
        {
            panoRenderer = SphericalPanoRenderer.CreateSphere(request.texture, radius, segments);
            panoRenderer.shader = Shader.Find("Unlit/Texture");
            panoRenderer.gameObject.AddComponent<KeyboardControl>();
            panoRenderer.gameObject.AddComponent<MouseControl>();
            panoRenderer.gameObject.AddComponent<Limits>();

            if (closeButtonPrefab != null && closeButtonInstance == null)
            {
                Canvas canvas = InfinityCode.uPano.CanvasUtils.GetCanvas();
                if (canvas != null)
                {
                    closeButtonInstance = Instantiate(closeButtonPrefab);
                    closeButtonInstance.transform.SetParent(canvas.transform, false);
                    closeButtonInstance.GetComponentInChildren<Button>().onClick.AddListener(OnCloseButtonClick);
                }
            }
        }
        else
        {
            panoRenderer.texture = request.texture;
        }
    }

    private void OnMapClick()
    {
        if (!enabled) return;

        double lng, lat;
        OnlineMapsControlBase.instance.GetCoords(out lng, out lat);
        GoogleStreetViewRequest request = new GoogleStreetViewRequest(googleApiKey, lng, lat, 3);
        request.OnSuccess += OnGoogleStreetViewSuccess;
    }

    private void OnStartDownloadTile(OnlineMapsTile tile)
    {
        OnlineMapsTileManager.StartDownloadTile(tile);
        StartDownloadOverlay(tile);
    }

    private void OnTileDisposed(OnlineMapsTile tile)
    {
        if (!overlays.ContainsKey(tile.key)) return;
        if (overlays[tile.key] != null) OnlineMapsUtils.Destroy(overlays[tile.key]);
        overlays.Remove(tile.key);
    }

    private void Start()
    {
        map = OnlineMaps.instance;
        
        if (OnlineMapsCache.instance != null)
        {
            OnlineMapsCache.instance.OnStartDownloadTile += OnStartDownloadTile;
            OnlineMapsCache.instance.OnLoadedFromCache += StartDownloadOverlay;
        }
        else
        {
            OnlineMapsTileManager.OnStartDownloadTile += OnStartDownloadTile;
        }

        map.control.OnMapClick += OnMapClick;
    }

    private void StartDownloadOverlay(OnlineMapsTile tile)
    {
        if (tile.zoom < minZoom) return;

        string url = string.Format("https://maps.google.com/maps/vt?pb=!1m5!1m4!1i{0}!2i{1}!3i{2}!4i256!2m8!1e2!2ssvv!4m2!1scb_client!2sapiv3!4m2!1scc!2s*211m3*211e3*212b1*213e2*211m3*211e2*212b1*213e2!3m5!3sUS!12m1!1e40!12m1!1e18", tile.zoom, tile.x, tile.y);
        OnlineMapsWWW www = new OnlineMapsWWW(url);
        www.OnComplete += delegate
        {
            if (tile.status == OnlineMapsTileStatus.disposed) return;

            Texture2D texture = new Texture2D(256, 256, TextureFormat.ARGB32, map.control.mipmapForTiles);
            www.LoadImageIntoTexture(texture);
            texture.wrapMode = TextureWrapMode.Clamp;
            overlays.Add(tile.key, texture);
            tile.OnDisposed += OnTileDisposed;

            if (enabled)
            {
                tile.overlayFrontTexture = texture;
                map.Redraw();
            }
        };
    }

    private void Update()
    {
        if (Input.GetKeyDown(closeByKeyCode))
        {
            if (panoRenderer != null)
            {
                Destroy(panoRenderer.gameObject);
                panoRenderer = null;
            }

            if (closeButtonInstance != null)
            {
                Destroy(closeButtonInstance);
                closeButtonInstance = null;
            }
        }
    }
#endif
}
