/*     INFINITY CODE 2013-2019      */
/*   http://www.infinity-code.com   */

/// <summary>
/// Implements elevation managers, which loads elevation data in one piece.
/// </summary>
/// <typeparam name="T">Type of elevation manager</typeparam>
public abstract class OnlineMapsSinglePartElevationManager<T> : OnlineMapsElevationManager<T>
    where T : OnlineMapsSinglePartElevationManager<T>
{
    protected bool _useElevation;
    protected float elevationRequestX1;
    protected float elevationRequestY1;
    protected float elevationRequestX2;
    protected float elevationRequestY2;
    protected float elevationRequestW;
    protected float elevationRequestH;
    protected short[,] elevationData;
    protected float elevationX1;
    protected float elevationY1;
    protected float elevationW;
    protected float elevationH;
    protected int elevationDataWidth;
    protected int elevationDataHeight;
    protected bool waitSetElevationData;

    public override bool hasData
    {
        get { return elevationData != null; }
    }

    protected override float GetElevationValue(double x, double z, float yScale, double tlx, double tly, double brx, double bry)
    {
        if (elevationData == null) return 0;

        float v = GetUnscaledElevationValue(x, z, tlx, tly, brx, bry);
        if (bottomMode == OnlineMapsElevationBottomMode.minValue) v -= minValue;
        return v * yScale * scale;
    }

    protected override float GetUnscaledElevationValue(double x, double z, double tlx, double tly, double brx, double bry)
    {
        if (elevationData == null) return 0;
        if (elevationDataWidth == 0 || elevationDataHeight == 0) return 0;
        if (elevationData.GetLength(0) != elevationDataWidth || elevationData.GetLength(1) != elevationDataHeight) return 0;

        x = x / -sizeInScene.x;
        z = z / sizeInScene.y;

        int ew = elevationDataWidth - 1;
        int eh = elevationDataHeight - 1;

        if (x < 0) x = 0;
        else if (x > 1) x = 1;

        if (z < 0) z = 0;
        else if (z > 1) z = 1;

        double cx = (brx - tlx) * x + tlx;
        double cz = (bry - tly) * z + tly;

        float rx = (float)((cx - elevationX1) / elevationW * ew);
        float ry = (float)((cz - elevationY1) / elevationH * eh);

        if (rx < 0) rx = 0;
        else if (rx > ew) rx = ew;

        if (ry < 0) ry = 0;
        else if (ry > eh) ry = eh;

        int x1 = (int)rx;
        int x2 = x1 + 1;
        int y1 = (int)ry;
        int y2 = y1 + 1;
        if (x2 > ew) x2 = ew;
        if (y2 > eh) y2 = eh;

        float p1 = (elevationData[x2, eh - y1] - elevationData[x1, eh - y1]) * (rx - x1) + elevationData[x1, eh - y1];
        float p2 = (elevationData[x2, eh - y2] - elevationData[x1, eh - y2]) * (rx - x1) + elevationData[x1, eh - y2];

        return (p2 - p1) * (ry - y1) + p1;
    }

    public override void SetElevationData(short[,] data)
    {
        elevationData = data;
        elevationX1 = elevationRequestX1;
        elevationY1 = elevationRequestY1;
        elevationW = elevationRequestW;
        elevationH = elevationRequestH;
        elevationDataWidth = data.GetLength(0);
        elevationDataHeight = data.GetLength(1);

        UpdateMinMax();

        waitSetElevationData = false;

        if (OnElevationUpdated != null) OnElevationUpdated();
        map.Redraw();
    }

    protected override void Update()
    {
        if (!zoomRange.InRange(map.floatZoom)) return;
        if (elevationBufferPosition == bufferPosition) return;

        RequestNewElevationData();
    }

    protected override void UpdateMinMax()
    {
        minValue = short.MaxValue;
        maxValue = short.MinValue;

        if (!hasData) return;

        int s1 = elevationData.GetLength(0);
        int s2 = elevationData.GetLength(1);

        for (int i = 0; i < s1; i++)
        {
            for (int j = 0; j < s2; j++)
            {
                short v = elevationData[i, j];
                if (v < minValue) minValue = v;
                if (v > maxValue) maxValue = v;
            }
        }
    }
}