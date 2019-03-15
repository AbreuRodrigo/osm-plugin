/*     INFINITY CODE 2013-2019      */
/*   http://www.infinity-code.com   */

using System;
using UnityEngine;

/// <summary>
/// Implements the use of elevation data from ArcGIS
/// </summary>
[OnlineMapsPlugin("ArcGIS Elevations", typeof(OnlineMapsControlBaseDynamicMesh), "Elevations")]
public class OnlineMapsArcGISElevationManager : OnlineMapsSinglePartElevationManager<OnlineMapsArcGISElevationManager>
{
    private OnlineMapsWWW elevationRequest;

    public override void CancelCurrentElevationRequest()
    {
        waitSetElevationData = false;
        elevationRequest = null;
    }

    public override void RequestNewElevationData()
    {
        base.RequestNewElevationData();

        if (elevationRequest != null || waitSetElevationData) return;

        elevationBufferPosition = bufferPosition;

        const int s = OnlineMapsUtils.tileSize;
        int countX = map.width / s + 2;
        int countY = map.height / s + 2;

        double sx, sy, ex, ey;
        map.projection.TileToCoordinates(bufferPosition.x, bufferPosition.y, map.zoom, out sx, out sy);
        map.projection.TileToCoordinates(bufferPosition.x + countX, bufferPosition.y + countY, map.zoom, out ex, out ey);

        elevationRequestX1 = (float)sx;
        elevationRequestY1 = (float)sy;
        elevationRequestX2 = (float)ex;
        elevationRequestY2 = (float)ey;
        elevationRequestW = elevationRequestX2 - elevationRequestX1;
        elevationRequestH = elevationRequestY2 - elevationRequestY1;

        if (OnGetElevation == null)
        {
            StartDownloadElevation(sx, sy, ex, ey);
        }
        else
        {
            waitSetElevationData = true;
            OnGetElevation(sx, sy, ex, ey);
        }

        if (OnElevationRequested != null) OnElevationRequested();
    }

    private void OnElevationRequestComplete(OnlineMapsWWW www)
    {
        elevationRequest = null;
        if (www.hasError)
        {
            Debug.Log(www.error);
            return;
        }

        string response = www.text;

        const int elevationDataResolution = 32;

        try
        {
            bool isFirstResponse = false;
            if (elevationData == null)
            {
                elevationData = new short[elevationDataResolution, elevationDataResolution];
                isFirstResponse = true;
            }

            int dataIndex = response.IndexOf("\"data\":[");
            if (dataIndex == -1)
            {
                if (isFirstResponse)
                {
                    elevationX1 = elevationRequestX1;
                    elevationY1 = elevationRequestY1;
                    elevationW = elevationRequestW;
                    elevationH = elevationRequestH;
                    elevationDataWidth = elevationDataResolution;
                    elevationDataHeight = elevationDataResolution;
                }
                Debug.LogWarning(response);
                if (OnElevationFails != null) OnElevationFails(response);

                return;
            }
            dataIndex += 8;

            int index = 0;
            int v = 0;
            bool isNegative = false;

            for (int i = dataIndex; i < response.Length; i++)
            {
                char c = response[i];
                if (c == ',')
                {
                    int x = index % 32;
                    int y = 31 - index / 32;
                    if (isNegative) v = -v;
                    elevationData[x, y] = (short)v;
                    v = 0;
                    isNegative = false;
                    index++;
                }
                else if (c == '-') isNegative = true;
                else if (c > 47 && c < 58) v = v * 10 + (c - 48);
                else break;
            }

            elevationX1 = elevationRequestX1;
            elevationY1 = elevationRequestY1;
            elevationW = elevationRequestW;
            elevationH = elevationRequestH;
            elevationDataWidth = elevationDataResolution;
            elevationDataHeight = elevationDataResolution;

            UpdateMinMax();
            if (OnElevationUpdated != null) OnElevationUpdated();

            control.UpdateControl();
        }
        catch (Exception exception)
        {
            Debug.Log(exception.Message);
            if (OnElevationFails != null) OnElevationFails(exception.Message);
        }
        map.Redraw();
    }

    /// <summary>
    /// Starts downloading elevation data for the specified area.
    /// </summary>
    /// <param name="sx">Left longitude</param>
    /// <param name="sy">Top latitude</param>
    /// <param name="ex">Right longitude</param>
    /// <param name="ey">Bottom latitude</param>
    public void StartDownloadElevation(double sx, double sy, double ex, double ey)
    {
        string url = "http://sampleserver4.arcgisonline.com/ArcGIS/rest/services/Elevation/ESRI_Elevation_World/MapServer/exts/ElevationsSOE/ElevationLayers/1/GetElevationData?f=json&Extent={%22spatialReference%22:{%22wkid%22:4326},%22ymin%22:" + ey + ",%22ymax%22:" + sy + ",%22xmin%22:" + sx + ",%22xmax%22:" + ex + "}&Rows=32&Columns=32";
        elevationRequest = new OnlineMapsWWW(url);
        elevationRequest.OnComplete += OnElevationRequestComplete;
    }
}