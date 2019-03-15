/*     INFINITY CODE 2013-2019      */
/*   http://www.infinity-code.com   */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Manages map tiles
/// </summary>
public static class OnlineMapsTileManager
{
    /// <summary>
    /// The maximum number simultaneously downloading tiles.
    /// </summary>
    public static int maxTileDownloads = 5;

    /// <summary>
    /// The event occurs after generating buffer and before update control to preload tiles for tileset.
    /// </summary>
    public static Action OnPreloadTiles;

    public static Action<OnlineMapsTile> OnPrepareDownloadTile;

    /// <summary>
    /// An event that occurs when loading the tile. Allows you to intercept of loading tile, and load it yourself.
    /// </summary>
    public static Action<OnlineMapsTile> OnStartDownloadTile;

    /// <summary>
    /// This event is occurs when a tile is loaded.
    /// </summary>
    public static Action<OnlineMapsTile> OnTileLoaded;

    private static OnlineMaps map
    {
        get { return OnlineMaps.instance; }
    }

    private static void OnTileWWWComplete(OnlineMapsWWW www)
    {
        OnlineMapsTile tile = www["tile"] as OnlineMapsTile;

        if (tile == null) return;
        tile.LoadFromWWW(www);
    }

    public static void OnTrafficWWWComplete(OnlineMapsWWW www)
    {
        OnlineMapsRasterTile tile = www["tile"] as OnlineMapsRasterTile;

        if (tile == null) return;
        if (tile.trafficWWW == null || !tile.trafficWWW.isDone) return;

        if (tile.status == OnlineMapsTileStatus.disposed)
        {
            tile.trafficWWW = null;
            return;
        }

        if (!www.hasError)
        {
            if (map.control.resultIsTexture)
            {
                if (tile.OnLabelDownloadComplete()) map.buffer.ApplyTile(tile);
            }
            else if (tile.trafficWWW != null && map.traffic)
            {
                Texture2D trafficTexture = new Texture2D(256, 256, TextureFormat.ARGB32, false)
                {
                    wrapMode = TextureWrapMode.Clamp
                };
                if (map.useSoftwareJPEGDecoder) OnlineMapsRasterTile.LoadTexture(trafficTexture, www.bytes);
                else tile.trafficWWW.LoadImageIntoTexture(trafficTexture);
                tile.trafficTexture = trafficTexture;
            }

            if (OnlineMapsTile.OnTrafficDownloaded != null) OnlineMapsTile.OnTrafficDownloaded(tile);

            map.Redraw();
        }

        tile.trafficWWW = null;
    }

    public static void StartDownloading()
    {
        long startTick = DateTime.Now.Ticks;

        int countDownload = 0;
        OnlineMapsTile[] downloadTiles;
        int c = 0;

        lock (OnlineMapsTile.lockTiles)
        {
            List<OnlineMapsTile> tiles = OnlineMapsTile.tiles;
            for (int i = 0; i < tiles.Count; i++)
            {
                OnlineMapsTile tile = tiles[i];
                if (tile.status == OnlineMapsTileStatus.loading && tile.www != null)
                {
                    countDownload++;
                    if (countDownload >= maxTileDownloads) return;
                }
            }

            int needDownload = maxTileDownloads - countDownload;
            downloadTiles = new OnlineMapsTile[needDownload];

            for (int i = 0; i < tiles.Count; i++)
            {
                OnlineMapsTile tile = tiles[i];
                if (tile.status != OnlineMapsTileStatus.none) continue;

                if (c == 0)
                {
                    downloadTiles[0] = tile;
                    c++;
                }
                else
                {
                    int index = c;
                    int index2 = index - 1;

                    while (index2 >= 0)
                    {
                        if (downloadTiles[index2].zoom <= tile.zoom) break;

                        index2--;
                        index--;
                    }

                    if (index < needDownload)
                    {
                        for (int j = needDownload - 1; j > index; j--) downloadTiles[j] = downloadTiles[j - 1];
                        downloadTiles[index] = tile;
                        if (c < needDownload) c++;
                    }
                }
            }
        }

        for (int i = 0; i < c; i++)
        {
            if (DateTime.Now.Ticks - startTick > 20000) break;
            OnlineMapsTile tile = downloadTiles[i];

            countDownload++;
            if (countDownload > maxTileDownloads) break;

            if (OnPrepareDownloadTile != null) OnPrepareDownloadTile(tile);
            if (OnStartDownloadTile != null) OnStartDownloadTile(tile);
            else StartDownloadTile(tile);
        }
    }

    /// <summary>
    /// Starts dowloading of specified tile.
    /// </summary>
    /// <param name="tile">Tile to be downloaded.</param>
    public static void StartDownloadTile(OnlineMapsTile tile)
    {
        tile.status = OnlineMapsTileStatus.loading;
        map.StartCoroutine(StartDownloadTileAsync(tile));
    }

    private static IEnumerator StartDownloadTileAsync(OnlineMapsTile tile)
    {
        bool loadOnline = true;

        if (map.source != OnlineMapsSource.Online)
        {
            ResourceRequest resourceRequest = Resources.LoadAsync(tile.resourcesPath);
            yield return resourceRequest;
            Object tileTexture = resourceRequest.asset;

            if (tileTexture != null)
            {
                tileTexture = Object.Instantiate(tileTexture);
                if (map.control.resultIsTexture)
                {
                    (tile as OnlineMapsRasterTile).ApplyTexture(tileTexture as Texture2D);
                    map.buffer.ApplyTile(tile);
                }
                else
                {
                    tile.texture = tileTexture as Texture2D;
                    tile.status = OnlineMapsTileStatus.loaded;
                }
                tile.MarkLoaded();
                map.Redraw();
                loadOnline = false;
            }
            else if (map.source == OnlineMapsSource.Resources)
            {
                tile.status = OnlineMapsTileStatus.error;
                yield break;
            }
        }

        if (loadOnline)
        {
            if (tile.www != null)
            {
                Debug.Log("tile has www " + tile + "   " + tile.status);
                yield break;
            }

            tile.www = new OnlineMapsWWW(tile.url);
            tile.www["tile"] = tile;
            tile.www.OnComplete += OnTileWWWComplete;
            tile.status = OnlineMapsTileStatus.loading;
        }

        OnlineMapsRasterTile rTile = tile as OnlineMapsRasterTile;

        if (map.traffic && !string.IsNullOrEmpty(rTile.trafficURL))
        {
            rTile.trafficWWW = new OnlineMapsWWW(rTile.trafficURL);
            rTile.trafficWWW["tile"] = tile;
            rTile.trafficWWW.OnComplete += OnTrafficWWWComplete;
        }
    }
}