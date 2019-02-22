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

		public TileData(int zoom, int x, int y) : this()
		{
			this.zoom = zoom;
			this.x = x;
			this.y = y;
			this.name = OSMGeoHelper.GetTileName(this);
		}

		public TileData(int index, int zoom, int x, int y, string name) : this(zoom, x, y)
		{
			this.index = index;
			this.name = name;
		}

		public TileData(int index, int zoom, int x, int y) : this(zoom, x, y)
		{
			this.index = index;
			this.name = OSMGeoHelper.GetTileName(zoom, x, y);
		}
	}
}