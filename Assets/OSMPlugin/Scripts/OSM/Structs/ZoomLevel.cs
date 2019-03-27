using System;

namespace OSM
{
	public struct ZoomLevel
	{
		public int scale;
		public int sum;

		public ZoomLevel(int sum)
		{
			this.sum = sum;
			this.scale = (int) Math.Pow(2, sum);
		}
	}
}