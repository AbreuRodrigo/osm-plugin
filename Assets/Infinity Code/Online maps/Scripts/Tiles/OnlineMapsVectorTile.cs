/*     INFINITY CODE 2013-2019      */
/*   http://www.infinity-code.com   */

using UnityEngine;

/// <summary>
/// Vector tile. This class is not used in the current version and was created for future versions.
/// </summary>
public abstract class OnlineMapsVectorTile : OnlineMapsTile
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="x">Tile X</param>
    /// <param name="y">Tile Y</param>
    /// <param name="zoom">Tile zoom</param>
    /// <param name="map">Reference to the map</param>
    /// <param name="isMapTile">Will the tile be displayed on the map.</param>
    public OnlineMapsVectorTile(int x, int y, int zoom, OnlineMaps map, bool isMapTile = true) : base(x, y, zoom, map, isMapTile)
    {
    }
}