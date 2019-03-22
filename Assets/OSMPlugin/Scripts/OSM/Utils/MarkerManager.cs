using System;
using System.Collections.Generic;
using UnityEngine;

namespace OSM
{
	public class MarkerManager : MonoBehaviourSingleton<MarkerManager>
	{
		private const string MARKER_NAME = "Marker";

		[SerializeField]
		private Marker _markerPrefab;
		[SerializeField]
		private Camera _mainCamera;
		[SerializeField]
		private RectTransform _canvas;
		[SerializeField]
		private List<Marker> _markers;

		public List<Marker> Markers { get { return _markers; } }
				
		private int _markerIndexCounter;
				
		public Marker CreateMarkerFallingDown(Vector3 pPoint, Action pOnComplete = null)
		{
			Vector3 start = new Vector3(pPoint.x, _mainCamera.ScreenToWorldPoint(new Vector3(0, Screen.height, _mainCamera.transform.position.z * -1)).y * 3, 0);

			Marker marker = CreateMarker(start, false);

			TweenManager.Instance.FallDownAndSquish(marker.gameObject, 0.25f, WorldToCanvasPosition(pPoint), () =>
			{
				marker.Active = true;
				pOnComplete?.Invoke();
			});

			return marker;
		}

		public Marker CreateMarker(Vector3 pPoint, bool pAutoActivate = true)
		{
			_markerIndexCounter++;

			Marker marker = Instantiate(_markerPrefab, WorldToCanvasPosition(pPoint), Quaternion.identity, transform);
			marker.Index = _markerIndexCounter;
			marker.name = MARKER_NAME + _markerIndexCounter;

			_markers.Add(marker);

			marker.Active = pAutoActivate;

			return marker;
		}

		public Vector2 WorldToCanvasPosition(Vector3 pPosition)
		{
			Vector2 temp = _mainCamera.WorldToViewportPoint(pPosition);

			temp.x *= _canvas.sizeDelta.x;
			temp.y *= _canvas.sizeDelta.y;

			return temp;
		}

		public Vector3 CanvasToWorldPosition(Vector3 pPosition)
		{
			pPosition.x /= _canvas.sizeDelta.x;
			pPosition.y /= _canvas.sizeDelta.y;

			Vector2 temp = _mainCamera.ViewportToWorldPoint(pPosition);

			return temp;
		}
	}
}