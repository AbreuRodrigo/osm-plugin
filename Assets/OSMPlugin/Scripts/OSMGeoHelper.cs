using System;

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

			coords.latitude = (float)((pTileX / Math.Pow(2.0, pZoom) * 360.0) - 180.0);
			coords.longitude = (float)(180.0 / Math.PI * Math.Atan(Math.Sinh(n)));

			return coords;
		}

		public static TileData GetTileData(int pZoom, double pLatitude, double pLongitude)
		{			
			Point3 tilePoint = WorldToTilePos(pLatitude, pLongitude, pZoom);
			TileData tileData = new TileData(tilePoint.zoomLevel, tilePoint.x, tilePoint.y);
			return tileData;
		}

		public static string GetTileName(TileData pTileData)
		{
			return GetTileName(pTileData.zoom, pTileData.x, pTileData.y);
		}

		public static string GetTileName(int pZoom, int pX, int pY)
		{
			return string.Format("{0}/{1}/{2}.png", pZoom, pX, pY);
		}

		public static TileNeighbours GetTileNeighbourNames(TileData pTileData)
		{
			TileNeighbours neighbours = new TileNeighbours();

			neighbours.northNeighbour = GetTileName(pTileData.zoom, pTileData.x, pTileData.y - 1);
			neighbours.northEastNeighbour = GetTileName(pTileData.zoom, pTileData.x + 1, pTileData.y - 1);
			neighbours.eastNeighbour = GetTileName(pTileData.zoom, pTileData.x + 1, pTileData.y);
			neighbours.southEastNeighbour = GetTileName(pTileData.zoom, pTileData.x + 1, pTileData.y + 1);
			neighbours.southNeighbour = GetTileName(pTileData.zoom, pTileData.x, pTileData.y + 1);
			neighbours.southWestNeighbour = GetTileName(pTileData.zoom, pTileData.x - 1, pTileData.y + 1);
			neighbours.westNeighbour = GetTileName(pTileData.zoom, pTileData.x - 1, pTileData.y);
			neighbours.northWestNeighbour = GetTileName(pTileData.zoom, pTileData.x - 1, pTileData.y - 1);

			return neighbours;
		}
	}
}