using System;

namespace OSM
{
	[Serializable]
	public struct Coordinates
	{
		public double latitude;
		public double longitude;

		public override string ToString()
		{
			return latitude + " " + longitude;
		}
	}
}