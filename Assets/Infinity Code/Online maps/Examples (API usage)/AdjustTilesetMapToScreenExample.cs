/*     INFINITY CODE 2013-2019      */
/*   http://www.infinity-code.com   */

using UnityEngine;

namespace InfinityCode.OnlineMapsExamples
{
    /// <summary>
    /// Example how to adjust the size of the tileset map to the screen size.
    /// </summary>
    [AddComponentMenu("Infinity Code/Online Maps/Examples (API Usage)/AdjustTilesetMapToScreenExample")]
    public class AdjustTilesetMapToScreenExample : MonoBehaviour
    {
        private int screenWidth;
        private int screenHeight;

        /// <summary>
        /// Adjust the map size
        /// </summary>
        private void ResizeMap()
        {
            screenWidth = Screen.width;
            screenHeight = Screen.height;

            int mapWidth = screenWidth / 256 * 256;
            int mapHeight = screenHeight / 256 * 256;
            if (screenWidth % 256 != 0) mapWidth += 256;
            if (screenHeight % 256 != 0) mapHeight += 256;
            OnlineMapsTileSetControl.instance.Resize(mapWidth, mapHeight);
            OnlineMapsCameraOrbit.instance.distance = screenHeight * 0.9f;
        }

        private void Start()
        {
            // Initial resizing
            ResizeMap();
        }

        private void Update()
        {
            // If the screen size changes, resize the map
            if (screenWidth != Screen.width || screenHeight != Screen.height) ResizeMap();
        }
    }
}
