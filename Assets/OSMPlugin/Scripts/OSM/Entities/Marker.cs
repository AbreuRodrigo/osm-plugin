using UnityEngine;

namespace OSM
{
	public class Marker : MonoBehaviour
	{
		[SerializeField]
		private int _index;
		[SerializeField]
		private Coordinates _geoCoordinate;
		[SerializeField]
		private SpriteRenderer _spriteRenderer;

		public int Index
		{
			get { return _index; }
			set { _index = value; }
		}

		public double Latitude
		{
			get { return _geoCoordinate.latitude; }
			set { _geoCoordinate.latitude = value; }
		}

		public double Longitude
		{
			get { return _geoCoordinate.longitude; }
			set { _geoCoordinate.longitude = value; }
		}

		public Coordinates GeoCoordinates
		{
			get { return _geoCoordinate; }
			set { _geoCoordinate = value; }
		}
	}
}