using System;

namespace OSM
{
	[Serializable]
	public struct LayerConfig
	{
		public ELayerType type;
		public int layerOrder;
		public int width;
		public int height;
	}
}