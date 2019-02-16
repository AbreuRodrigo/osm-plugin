using System;

namespace OSM
{
	[Serializable]
	public struct TileData
	{
		public int index;
		public int zoom;		
		public int x;
		public int y;
		public string name;
		public TileNeighbours neighbours;

		public TileData(int zoom, int x, int y) : this()
		{
			this.zoom = zoom;
			this.x = x;
			this.y = y;
			this.name = OSMGeoHelper.GetTileName(this);
			this.neighbours = OSMGeoHelper.GetTileNeighbourNames(this);
		}
	}
}