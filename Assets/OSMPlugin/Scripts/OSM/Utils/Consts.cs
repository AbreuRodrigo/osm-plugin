namespace OSM
{
	public class Consts
	{
		public const string LAYER_BASE_NAME = "Layer";
		public const string CURRENT_LAYER = "CurrentLayer";
		public const string OTHER_LAYER = "OtherLayer";
		public const string MARKER_NAME = "Marker";

		public const int MIN_ZOOM_LEVEL = 3;
		public const int MAX_ZOOM_LEVEL = 19;
		public const int TILE_SIZE_IN_PIXELS = 256;
		public const int TARGET_FRAME_RATE = 60;

		public const float TILE_SIZE_IN_UNITS = TILE_SIZE_IN_PIXELS * 0.01f;
		public const float TILE_HALF_SIZE_IN_UNITS = TILE_SIZE_IN_PIXELS * 0.005f;
		public const float TILE_QUARTER_SIZE_IN_UNITS = TILE_SIZE_IN_PIXELS * 0.0025f;		
		public const float SCALING_SPEED_FOR_MARKERS = 0.125f;
	}
}
