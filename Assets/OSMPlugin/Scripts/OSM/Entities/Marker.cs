using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace OSM
{
	public class Marker : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		[SerializeField]
		private bool _active;
		[SerializeField]
		private int _index;
		[SerializeField]
		private Coordinates _geoCoordinate;

		[SerializeField]
		private Image _image;
		[SerializeField]
		private TextMeshProUGUI _text;
		[SerializeField]
		private string _stringText;
				
		public int Index
		{
			get { return _index; }
			set { _index = value; }
		}

		public bool Active
		{
			get { return _active; }
			set { _active = value; }
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

		public Image Image
		{
			get { return _image; }
			set { _image = value; }
		}

		public void SetText(string pText)
		{
			if(_text != null)
			{
				_stringText = pText;
				_text.text = _stringText;
			}
		}

		public void FadeIn()
		{
			Color c = _image.color;

			TweenManager.Instance.ValueTransition(0, 1, 0.25f, TweenType.Linear, true, null, (float v) => {
				c.a = v;
				_image.color = c;
			});
		}

		public void FadeOut()
		{
			Color c = _image.color;

			TweenManager.Instance.ValueTransition(1, 0, 0.25f, TweenType.Linear, true, null, (float v) => {
				c.a = v;
				_image.color = c;
			});
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			if (_text != null)
			{
				SetText("latitude: " + Latitude + "\n" + "longitude: " + Longitude);
				_text.gameObject.SetActive(true);
			}
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			if (_text != null)
			{
				_text.gameObject.SetActive(false);
			}
		}
	}
}