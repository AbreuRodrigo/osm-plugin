using System;
using System.Collections.Generic;
using UnityEngine;

namespace OSM
{
	public class MarkerManager : MonoBehaviourSingleton<MarkerManager>
	{
		private const string MARKER_NAME = "Marker";
		private const string MARKERS_CONTAINER = "MarkersContainer";

		[SerializeField]
		private Marker _markerPrefab;
		[SerializeField]
		private Camera _mainCamera;
		[SerializeField]
		private List<Marker> _markers;

		public List<Marker> Markers { get { return _markers; } }

		private GameObject _markersLayer;
		
		private int _markerIndexCounter;

		public GameObject CreateMarkersLayer()
		{
			_markersLayer = new GameObject(MARKERS_CONTAINER);
			return _markersLayer;
		}

		public Marker CreateMarkerFallingDown(Vector3 pPoint, Action pOnComplete = null)
		{
			Vector3 start = new Vector3(pPoint.x, _mainCamera.ScreenToWorldPoint(new Vector3(0, Screen.height, _mainCamera.transform.position.z * -1)).y * 3, 0);

			Marker marker = CreateMarker(start);

			TweenManager.Instance.FallDownAndSquish(marker.gameObject, 0.25f, pPoint, pOnComplete);

			return marker;
		}

		public Marker CreateMarker(Vector3 pPoint)
		{
			_markerIndexCounter++;

			Marker marker = Instantiate(_markerPrefab, pPoint, Quaternion.identity, _markersLayer.transform);
			marker.Index = _markerIndexCounter;
			marker.name = MARKER_NAME + _markerIndexCounter;
			marker.transform.SetParent(_markersLayer.transform);

			_markers.Add(marker);

			return marker;
		}
	}
}