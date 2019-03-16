using System;
using UnityEngine;

namespace OSM
{
	public class OSMGeoHelper
	{		
		public static Point3 WorldToTilePos(double pLatitude, double pLongiture, int pZoom)
		{
			Point3 p;
			p.x = (int)((pLongiture + 180.0) / 360.0 * (1 << pZoom));
			p.y = (int)((1.0 - Math.Log(Math.Tan(pLatitude * Math.PI / 180.0) +
				1.0 / Math.Cos(pLatitude * Math.PI / 180.0)) / Math.PI) / 2.0 * (1 << pZoom));

			p.zoomLevel = pZoom;

			return p;
		}

		public static Coordinates TileToWorldPos(double pTileX, double pTileY, int pZoom)
		{
			Coordinates coords;
			double n = Math.PI - ((2.0 * Math.PI * pTileY) / Math.Pow(2.0, pZoom));

			coords.longitude = (float)((pTileX / Math.Pow(2.0, pZoom) * 360.0) - 180.0);
			coords.latitude = (float)(180.0 / Math.PI * Math.Atan(Math.Sinh(n)));

			return coords;
		}

		public static void TileToWorldPos(double pTileX, double pTileY, int pZoom, out double pLatitude, out double pLongitude)
		{
			Coordinates coords;
			double n = Math.PI - ((2.0 * Math.PI * pTileY) / Math.Pow(2.0, pZoom));

			pLongitude = (float)((pTileX / Math.Pow(2.0, pZoom) * 360.0) - 180.0);
			pLatitude = (float)(180.0 / Math.PI * Math.Atan(Math.Sinh(n)));
		}

		public static TileData GetTileData(int pZoom, double pLatitude, double pLongitude)
		{			
			Point3 tilePoint = WorldToTilePos(pLatitude, pLongitude, pZoom);
			return GetTileData(tilePoint.zoomLevel, tilePoint.x, tilePoint.y);
		}

		public static bool GetCoordsByWorldPosition(float pPointClickX, float pPointClickY, int pZoomLevel, Tile pTargetTile)
		{
			/*Vector3 boundsSize = new Vector3(sizeInScene.x, 0, sizeInScene.y);
			boundsSize.Scale(transform.lossyScale);
			Vector3 size = new Vector3(0, 0, sizeInScene.y * transform.lossyScale.z) - Quaternion.Inverse(transform.rotation) * (position - transform.position);

			size.x = size.x / boundsSize.x;
			size.z = size.z / boundsSize.z;

			Vector2 r = new Vector3(size.x - 0.5f, size.z - 0.5f);

			float zoomCoof = map.zoomCoof;
			int countX = map.buffer.renderState.width / OnlineMapsUtils.tileSize;
			int countY = map.buffer.renderState.height / OnlineMapsUtils.tileSize;

			double px, py;

			map.GetTilePosition(out px, out py);
			px += countX * r.x * zoomCoof;
			py -= countY * r.y * zoomCoof;
			TileToWorldPos(px, py, map.zoom, out lng, out lat);*/



			return true;
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