using System;

namespace OSM
{
	[Serializable]
	public struct LayerConfig
	{
		public ELayerType type;
		public int totalTiles;
		public int layerOrder;
	}
}