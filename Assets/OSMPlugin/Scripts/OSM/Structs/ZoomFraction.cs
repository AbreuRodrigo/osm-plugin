namespace OSM
{
	public struct ZoomFraction
	{
		public int zoomLevel;
		public float zoomScale;
		public float objectScale;
		public float scaleFraction;

		public ZoomFraction(int zoomLevel, float zoomScale, float objectScale)
		{
			this.zoomLevel = zoomLevel;
			this.zoomScale = zoomScale;
			this.objectScale = objectScale;
			this.scaleFraction = objectScale / zoomScale;
		}
	}
}