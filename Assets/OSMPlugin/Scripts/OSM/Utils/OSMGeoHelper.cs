﻿using System;

namespace OSM
{
	public class OSMGeoHelper
	{		
		public static Point3Int GeoToTilePos(double pLatitude, double pLongiture, int pZoom)
		{
			Point3Int p;
			p.x = (int)((pLongiture + 180.0) / 360.0 * (1 << pZoom));
			p.y = (int)((1.0 - Math.Log(Math.Tan(pLatitude * Math.PI / 180.0) +
				1.0 / Math.Cos(pLatitude * Math.PI / 180.0)) / Math.PI) / 2.0 * (1 << pZoom));

			p.zoomLevel = pZoom;

			return p;
		}

		public static Point3Double GeoToTilePosDouble(double pLatitude, double pLongiture, int pZoom)
		{
			Point3Double p;
			p.x = (pLongiture + 180.0) / 360.0 * (1 << pZoom);
			p.y = (1.0 - Math.Log(Math.Tan(pLatitude * Math.PI / 180.0) +
				1.0 / Math.Cos(pLatitude * Math.PI / 180.0)) / Math.PI) / 2.0 * (1 << pZoom);

			p.zoomLevel = pZoom;

			return p;
		}

		public static Coordinates TileToGeoPos(double pTileX, double pTileY, int pZoom)
		{
			Coordinates coords;
			double n = Math.PI - ((2.0 * Math.PI * pTileY) / Math.Pow(2.0, pZoom));

			coords.longitude = (float)((pTileX / Math.Pow(2.0, pZoom) * 360.0) - 180.0);
			coords.latitude = (float)(180.0 / Math.PI * Math.Atan(Math.Sinh(n)));

			return coords;
		}

		public static void TileToGeoPos(double pTileX, double pTileY, int pZoom, out double pLatitude, out double pLongitude)
		{
			Coordinates coords;
			double n = Math.PI - ((2.0 * Math.PI * pTileY) / Math.Pow(2.0, pZoom));

			pLongitude = (float)((pTileX / Math.Pow(2.0, pZoom) * 360.0) - 180.0);
			pLatitude = (float)(180.0 / Math.PI * Math.Atan(Math.Sinh(n)));
		}

		public static TileData GetTileData(int pZoom, double pLatitude, double pLongitude)
		{			
			Point3Int tilePoint = GeoToTilePos(pLatitude, pLongitude, pZoom);
			return GetTileData(tilePoint.zoomLevel, tilePoint.x, tilePoint.y);
		}

		public static TileData GetTileData(int pZoom, int pX, int pY)
		{
			return new TileData(pZoom, pX, pY);
		}

		public static string GetTileName(int pZoom, int pX, int pY)
		{
			return string.Format("{0}/{1}/{2}.png", pZoom, pX, pY);
		}				
	}
}