/*     INFINITY CODE 2013-2019      */
/*   http://www.infinity-code.com   */

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// This class of buffer tile image. \n
/// <strong>Please do not use it if you do not know what you're doing.</strong> \n
/// Perform all operations with the map through other classes.
/// </summary>
public abstract class OnlineMapsTile
{
    #region Actions

    /// <summary>
    /// The event that occurs when all tiles are loaded.
    /// </summary>
    public static Action OnAllTilesLoaded;

    /// <summary>
    /// The event, which allows you to control the path of tile in Resources.
    /// </summary>
    public static Func<OnlineMapsTile, string> OnGetResourcesPath;

    /// <summary>
    /// The event which allows to intercept the replacement tokens in the url.\n
    /// Return the value, or null - if you do not want to modify the value.
    /// </summary>
    public static Func<OnlineMapsTile, string, string> OnReplaceURLToken;

    /// <summary>
    /// The event which allows to intercept the replacement tokens in the traffic url.\n
    /// Return the value, or null - if you do not want to modify the value.
    /// </summary>
    public static Func<OnlineMapsTile, string, string> OnReplaceTrafficURLToken;

    /// <summary>
    /// The event, which occurs after a successful download of the tile.
    /// </summary>
    public static Action<OnlineMapsTile> OnTileDownloaded;

    /// <summary>
    /// The event, which occurs after a successful download of the traffic texture.
    /// </summary>
    public static Action<OnlineMapsTile> OnTrafficDownloaded;

    /// <summary>
    /// This event is called when the tile is disposed.
    /// </summary>
    public Action<OnlineMapsTile> OnDisposed;

    #endregion

    #region Variables

    #region Static Fields

    public static object lockTiles = new object();

    protected static OnlineMaps map;

    private static Dictionary<ulong, OnlineMapsTile> _dtiles;
    private static List<OnlineMapsTile> _tiles;
    private static List<OnlineMapsTile> unusedTiles;

    #endregion

    #region Public Fields

    /// <summary>
    /// The coordinates of the bottom-right corner of the tile.
    /// </summary>
    public Vector2 bottomRight;

    /// <summary>
    /// This flag indicates whether the cache is checked for a tile.
    /// </summary>
    public bool cacheChecked;

    /// <summary>
    /// Drawing elements have been changed. Used for drawing as overlay.
    /// </summary>
    public bool drawingChanged;

    /// <summary>
    /// The coordinates of the center point of the tile.
    /// </summary>
    public Vector2 globalPosition;

    /// <summary>
    /// Tile loaded or there are parent tile colors
    /// </summary>
    public bool hasColors;

    /// <summary>
    /// Is map tile?
    /// </summary>
    public bool isMapTile;

    /// <summary>
    /// The unique tile key.
    /// </summary>
    public ulong key;

    /// <summary>
    /// Texture, which is used in the back overlay.
    /// </summary>
    public Texture2D overlayBackTexture;

    /// <summary>
    /// Back overlay transparency (0-1).
    /// </summary>
    public float overlayBackAlpha = 1;

    /// <summary>
    /// Texture, which is used in the front overlay.
    /// </summary>
    public Texture2D overlayFrontTexture;

    /// <summary>
    /// Front overlay transparency (0-1).
    /// </summary>
    public float overlayFrontAlpha = 1;

    /// <summary>
    /// Reference to parent tile.
    /// </summary>
    [NonSerialized]
    public OnlineMapsTile parent;

    /// <summary>
    /// Status of tile.
    /// </summary>
    public OnlineMapsTileStatus status = OnlineMapsTileStatus.none;

    /// <summary>
    /// Texture of tile.
    /// </summary>
    public Texture2D texture;

    /// <summary>
    /// The coordinates of the top-left corner of the tile.
    /// </summary>
    public Vector2 topLeft;

    /// <summary>
    /// Tile used by map
    /// </summary>
    public bool used = true;

    /// <summary>
    /// Instance of the texture loader.
    /// </summary>
    public OnlineMapsWWW www;

    /// <summary>
    /// Tile X.
    /// </summary>
    public readonly int x;

    /// <summary>
    /// Tile Y.
    /// </summary>
    public readonly int y;

    /// <summary>
    /// Tile zoom.
    /// </summary>
    public readonly int zoom;

    #endregion

    #region Private Fields

    protected string _url;
    protected byte[] data;

    private Dictionary<string, object> _customFields;
    private List<object> blockers;
    private OnlineMapsTile[] childs = new OnlineMapsTile[4];
    private bool hasChilds;

    #endregion

    #endregion

    #region Properties

    /// <summary>
    /// Gets / sets custom fields value by key
    /// </summary>
    /// <param name="key">Custom field key</param>
    /// <returns>Custom field value</returns>
    public object this[string key]
    {
        get
        {
            object val;
            return customFields.TryGetValue(key, out val) ? val : null;
        }
        set { customFields[key] = value; }
    }

    /// <summary>
    /// Gets customFields dictionary
    /// </summary>
    public Dictionary<string, object> customFields
    {
        get
        {
            if (_customFields == null) _customFields = new Dictionary<string, object>();
            return _customFields;
        }
    }

    /// <summary>
    /// Path in Resources, from where the tile will be loaded.
    /// </summary>
    public string resourcesPath
    {
        get
        {
            if (OnGetResourcesPath != null) return OnGetResourcesPath(this);
            return Regex.Replace(map.resourcesPath, @"{\w+}", CustomProviderReplaceToken);
        }
    }

    /// <summary>
    /// List of all tiles.
    /// </summary>
    public static List<OnlineMapsTile> tiles
    {
        get
        {
            if (_tiles == null) _tiles = new List<OnlineMapsTile>();
            return _tiles;
        }
        set { _tiles = value; }
    }

    /// <summary>
    /// Dictionary of all tiles
    /// </summary>
    public static Dictionary<ulong, OnlineMapsTile> dTiles
    {
        get
        {
            if (_dtiles == null) _dtiles = new Dictionary<ulong, OnlineMapsTile>();
            return _dtiles;
        }
    }

    /// <summary>
    /// The tile is blocked?
    /// </summary>
    public bool isBlocked
    {
        get { return blockers != null && blockers.Count > 0; }
    }

    /// <summary>
    /// URL from which will be downloaded texture.
    /// </summary>
    public abstract string url
    {
        get;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="x">Tile X</param>
    /// <param name="y">Tile Y</param>
    /// <param name="zoom">Tile zoom</param>
    /// <param name="map">Reference to the map</param>
    /// <param name="isMapTile">Should this tile be displayed on the map?</param>
    public OnlineMapsTile(int x, int y, int zoom, OnlineMaps map, bool isMapTile = true)
    {
        if (unusedTiles == null) unusedTiles = new List<OnlineMapsTile>();

        int maxX = 1 << zoom;
        if (x < 0) x += maxX;
        else if (x >= maxX) x -= maxX;

        this.x = x;
        this.y = y;
        this.zoom = zoom;

        OnlineMapsTile.map = map;
        this.isMapTile = isMapTile;

        double tlx, tly, brx, bry;
        map.projection.TileToCoordinates(x, y, zoom, out tlx, out tly);
        map.projection.TileToCoordinates(x + 1, y + 1, zoom, out brx, out bry);
        topLeft = new Vector2((float)tlx, (float)tly);
        bottomRight = new Vector2((float)brx, (float)bry);

        globalPosition = Vector2.Lerp(topLeft, bottomRight, 0.5f);
        key = GetTileKey(zoom, x, y);

        if (isMapTile)
        {
            tiles.Add(this);
            if (dTiles.ContainsKey(key)) dTiles[key] = this;
            else dTiles.Add(key, this);
        }
    }

    /// <summary>
    /// Blocks the tile from disposing.
    /// </summary>
    /// <param name="blocker">The object that prohibited the disposing.</param>
    public void Block(object blocker)
    {
        if (blockers == null) blockers = new List<object>();
        blockers.Add(blocker);
    }

    protected string CustomProviderReplaceToken(Match match)
    {
        string v = match.Value.ToLower().Trim('{', '}');

        if (OnReplaceURLToken != null)
        {
            string ret = OnReplaceURLToken(this, v);
            if (ret != null) return ret;
        }

        if (v == "zoom") return zoom.ToString();
        if (v == "x") return x.ToString();
        if (v == "y") return y.ToString();
        if (v == "quad") return OnlineMapsUtils.TileToQuadKey(x, y, zoom);
        return v;
    }

    /// <summary>
    /// Dispose of tile.
    /// </summary>
    public void Dispose()
    {
        if (status == OnlineMapsTileStatus.disposed) return;
        status = OnlineMapsTileStatus.disposed;

        lock (lockTiles)
        {
            unusedTiles.Add(this);
        }

        if (_dtiles.ContainsKey(key)) _dtiles.Remove(key);

        if (OnDisposed != null) OnDisposed(this);
    }

    protected virtual void Destroy()
    {
        lock (lockTiles)
        {
            tiles.Remove(this);
        }

        if (texture != null) OnlineMapsUtils.Destroy(texture);
        if (overlayBackTexture != null) OnlineMapsUtils.Destroy(overlayBackTexture);
        if (overlayFrontTexture != null) OnlineMapsUtils.Destroy(overlayFrontTexture);

        texture = null;
        overlayBackTexture = null;
        overlayFrontTexture = null;
        _customFields = null;

        _url = null;
        data = null;
        blockers = null;

        if (hasChilds) foreach (OnlineMapsTile child in childs) if (child != null) child.parent = null;
        if (parent != null)
        {
            if (parent.childs != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (parent.childs[i] == this)
                    {
                        parent.childs[i] = null;
                        break;
                    }
                }
            }
        }
        parent = null;
        childs = null;
        hasChilds = false;
        hasColors = false;

        OnDisposed = null;
    }

    public virtual void DownloadComplete()
    {
        
    }

    /// <summary>
    /// Gets a tile for zoom, x, y.
    /// </summary>
    /// <param name="zoom">Tile zoom</param>
    /// <param name="x">Tile X</param>
    /// <param name="y">Tile Y</param>
    /// <returns>Tile or null</returns>
    public static OnlineMapsTile GetTile(int zoom, int x, int y)
    {
        ulong key = GetTileKey(zoom, x, y);
        if (dTiles.ContainsKey(key))
        {
            OnlineMapsTile tile = dTiles[key];
            if (tile.status != OnlineMapsTileStatus.disposed) return tile;
        }
        return null;
    }

    /// <summary>
    /// Gets a tile for zoom, x, y.
    /// </summary>
    /// <param name="zoom">Tile zoom</param>
    /// <param name="x">Tile X</param>
    /// <param name="y">Tile Y</param>
    /// <param name="tile">Tile</param>
    /// <returns>True - success, false - otherwise</returns>
    public static bool GetTile(int zoom, int x, int y, out OnlineMapsTile tile)
    {
        tile = null;
        ulong key = GetTileKey(zoom, x, y);
        OnlineMapsTile t;
        if (dTiles.TryGetValue(key, out t))
        {
            if (t.status != OnlineMapsTileStatus.disposed)
            {
                tile = t;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Gets a tile key for zoom, x, y.
    /// </summary>
    /// <param name="zoom">Tile zoom</param>
    /// <param name="x">Tile X</param>
    /// <param name="y">Tile Y</param>
    /// <returns>Tile key</returns>
    public static ulong GetTileKey(int zoom, int x, int y)
    {
        return ((ulong)zoom << 58) + ((ulong)x << 29) + (ulong)y;
    }

    /// <summary>
    /// Gets rect of the tile.
    /// </summary>
    /// <returns>Rect of the tile.</returns>
    public Rect GetRect()
    {
        return new Rect(topLeft.x, topLeft.y, bottomRight.x - topLeft.x, bottomRight.y - topLeft.y);
    }

    /// <summary>
    /// Checks whether the tile at the specified coordinates.
    /// </summary>
    /// <param name="tl">Coordinates of top-left corner.</param>
    /// <param name="br">Coordinates of bottom-right corner.</param>
    /// <returns>True - if the tile at the specified coordinates, False - if not.</returns>
    public bool InScreen(Vector2 tl, Vector2 br)
    {
        if (bottomRight.x < tl.x) return false;
        if (topLeft.x > br.x) return false;
        if (topLeft.y < br.y) return false;
        if (bottomRight.y > tl.y) return false;
        return true;
    }

    /// <summary>
    /// Load a tile from a WWW object
    /// </summary>
    /// <param name="www">WWW object</param>
    public void LoadFromWWW(OnlineMapsWWW www)
    {
        if (status == OnlineMapsTileStatus.disposed)
        {
            this.www = null;
            return;
        }

        if (!www.hasError && www.bytesDownloaded > 0) LoadTileFromWWW(www);
        else MarkError();

        this.www = null;
    }

    protected abstract void LoadTileFromWWW(OnlineMapsWWW www);

    /// <summary>
    /// Mark that the tile has an error
    /// </summary>
    public void MarkError()
    {
        status = OnlineMapsTileStatus.error;
    }

    /// <summary>
    /// Marks the tile loaded.
    /// </summary>
    public void MarkLoaded()
    {
        if (OnlineMapsTileManager.OnTileLoaded != null) OnlineMapsTileManager.OnTileLoaded(this);

        if (OnAllTilesLoaded == null) return;

        foreach (OnlineMapsTile tile in tiles)
        {
            if (tile.status != OnlineMapsTileStatus.loaded && tile.status != OnlineMapsTileStatus.error) return;
        }

        OnAllTilesLoaded();
    }

    private void SetChild(OnlineMapsTile tile)
    {
        if (childs == null) return;
        int cx = tile.x % 2;
        int cy = tile.y % 2;
        childs[cx * 2 + cy] = tile;
        hasChilds = true;
    }

    /// <summary>
    /// Set parent tile
    /// </summary>
    /// <param name="tile"></param>
    public void SetParent(OnlineMapsTile tile)
    {
        parent = tile;
        parent.SetChild(this);
    }

    public override string ToString()
    {
        return zoom + "x" + x + "x" + y;
    }

    /// <summary>
    /// Remove an object that prevents the tile from disposing.
    /// </summary>
    /// <param name="blocker">The object that prohibited the disposing.</param>
    public void Unblock(object blocker)
    {
        if (blockers == null) return;
        blockers.Remove(blocker);
    }

    public static void UnloadUnusedTiles()
    {
        if (unusedTiles == null) return;

        lock (lockTiles)
        {
            for (int i = 0; i < unusedTiles.Count; i++) unusedTiles[i].Destroy();
            unusedTiles.Clear();
        }
    }

    #endregion

    #region Obsolete

    [Obsolete("Use OnlineMapsRasterTile.colors")]
    public Color32[] colors
    {
        get { return (this as OnlineMapsRasterTile).colors; }
    }

    [Obsolete("Use OnlineMapsTile.customFields")]
    public object customData
    {
        get
        {
            if (!customFields.ContainsKey("__customData")) return null;
            return customFields["__customData"];
        }
        set { customFields["__customData"] = value; }
    }

    [Obsolete("Use OnlineMapsRasterTile.emptyColorTexture")]
    public static Texture2D emptyColorTexture
    {
        get { return OnlineMapsRasterTile.emptyColorTexture; }
        set { OnlineMapsRasterTile.emptyColorTexture = value; }
    }

    /// <summary>
    /// Labels is used in tile?
    /// </summary>
    [Obsolete("Use OnlineMapsRasterTile.labels")]
    public bool labels
    {
        get { return (this as OnlineMapsRasterTile).labels; }
        set { (this as OnlineMapsRasterTile).labels = value; }
    }

    /// <summary>
    /// Language is used in tile?
    /// </summary>
    [Obsolete("Use OnlineMapsRasterTile.language")]
    public string language
    {
        get { return (this as OnlineMapsRasterTile).language; }
        set { (this as OnlineMapsRasterTile).language = value; }
    }

    /// <summary>
    /// Instance of map type
    /// </summary>
    [Obsolete("Use OnlineMapsRasterTile.mapType")]
    public OnlineMapsProvider.MapType mapType
    {
        get { return (this as OnlineMapsRasterTile).mapType; }
        set { (this as OnlineMapsRasterTile).mapType = value; }
    }

    [Obsolete("Use OnlineMapsRasterTile.trafficURL")]
    public string trafficURL
    {
        get { return (this as OnlineMapsRasterTile).trafficURL; }
        set { (this as OnlineMapsRasterTile).trafficURL = value; }
    }

    [Obsolete("Use OnlineMapsRasterTile.trafficWWW")]
    public OnlineMapsWWW trafficWWW
    {
        get { return (this as OnlineMapsRasterTile).trafficWWW; }
        set { (this as OnlineMapsRasterTile).trafficWWW = value; }
    }

    [Obsolete("Use OnlineMapsRasterTile.ApplyTexture")]
    public void ApplyTexture(Texture2D texture)
    {
        (this as OnlineMapsRasterTile).ApplyTexture(texture);
    }

    [Obsolete("Use OnlineMapsRasterTile.CheckTextureSize")]
    public void CheckTextureSize(Texture2D texture)
    {
        (this as OnlineMapsRasterTile).CheckTextureSize(texture);
    }

    [Obsolete("Use OnlineMapsRasterTile.LoadTexture")]
    public static void LoadTexture(Texture2D texture, byte[] bytes)
    {
        OnlineMapsRasterTile.LoadTexture(texture, bytes);
    }

    [Obsolete("Use OnlineMapsTile.DownloadComplete")]
    public void OnDownloadComplete()
    {

    }

    [Obsolete("Use OnlineMapsTile.MarkError")]
    public void OnDownloadError()
    {
        MarkError();
    }

    #endregion
}