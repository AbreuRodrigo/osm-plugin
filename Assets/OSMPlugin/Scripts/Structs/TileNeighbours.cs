using System;

namespace OSM
{
	[Serializable]
	public struct TileNeighbours
	{
		public string northNeighbour;
		public string northEastNeighbour;
		public string eastNeighbour;
		public string southEastNeighbour;
		public string southNeighbour;
		public string southWestNeighbour;
		public string westNeighbour;
		public string northWestNeighbour;
	}
}