using System;
using System.Collections.Generic;
using UnityEngine;

namespace OSM
{
	public class MarkerManager : MonoBehaviourSingleton<MarkerManager>
	{
		[SerializeField]
		private Marker _markerPrefab;
		[SerializeField]
		private Camera _mainCamera;
		[SerializeField]
		private RectTransform _canvas;
		[SerializeField]
		private List<Marker> _markers;
		[SerializeField]
		private Map _map;

		private bool _isPlacingMarker;
		private Vector3 _insideTilePosition;
		private float _tapCoolDown = 0.5f;
		private float _tapTiming;
		private int _tapCounter;

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
			marker.name = Consts.MARKER_NAME + _markerIndexCounter;

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

		public Vector3 CanvasToWorldPosition(RectTransform rect, Vector3 pPosition)
		{
			Vector3 worldPoint = Vector3.zero;

			RectTransformUtility.ScreenPointToWorldPointInRectangle(rect, pPosition, _mainCamera, out worldPoint);

			return worldPoint;
		}

		private void StartMarkersPlacement()
		{
			_isPlacingMarker = true;
		}

		private void StopMarkersPlacement()
		{
			_isPlacingMarker = false;
		}

		public void CheckMarkersPlacement()
		{
			if (_isPlacingMarker == true)
			{
				Vector3 point = _mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, _mainCamera.transform.position.z * -1));

				Tile tile = _map.GetTileByVectorPoint(point);

				float tileXTopLeft = tile.transform.position.x - Consts.TILE_HALF_SIZE_IN_UNITS;
				float tileYTopLeft = tile.transform.position.y + Consts.TILE_HALF_SIZE_IN_UNITS;

				Vector3 positionInTile = point - new Vector3(tileXTopLeft, tileYTopLeft, 0);
				positionInTile.y *= -1;

				positionInTile /= Consts.TILE_SIZE_IN_UNITS;

				Coordinates coords = OSMGeoHelper.TileToGeoPos(tile.TileData.x + positionInTile.x, tile.TileData.y + positionInTile.y, tile.TileData.zoom);

				Marker marker = CreateMarkerFallingDown(point);
				marker.GeoCoordinates = coords;

				StopMarkersPlacement();
			}
		}


		public void ProcessMarkerSpawningInput()
		{
#if UNITY_ANDROID

			if (Input.touchCount == 1)
			{
				if (Input.GetTouch(0).phase == TouchPhase.Began)
				{
					_tapCounter++;
				}
			}

			if (_tapCounter > 0)
			{
				_tapTiming += Time.deltaTime;
			}

			if (_tapCounter == 2)
			{
				if (_tapTiming <= _tapCoolDown)
				{
					StartMarkersPlacement();
				}

				_tapCounter = 0;
				_tapTiming = 0;
			}
#endif
#if UNITY_EDITOR

			if (Input.GetMouseButtonUp(0))
			{
				_tapCounter++;
			}

			if (_tapCounter > 0)
			{
				_tapTiming += Time.deltaTime;
			}

			if (_tapCounter == 2)
			{
				if (_tapTiming <= _tapCoolDown)
				{
					StartMarkersPlacement();
				}

				_tapCounter = 0;
				_tapTiming = 0;
			}
			else
			{
				StopMarkersPlacement();
			}
#endif
		}

		//TODO: Do no update everytime we move the map
		public void UpdateMarkers(int pCurrentZoomLevel)
		{
			foreach (Marker marker in Markers)
			{
				if (marker.Active)
				{
					Point3Double tileP = OSMGeoHelper.GeoToTilePosDouble(marker.Latitude, marker.Longitude, pCurrentZoomLevel);

					int z = tileP.zoomLevel;
					int x = (int)tileP.x;
					int y = (int)tileP.y;

					_insideTilePosition.x = (float)tileP.x - x;
					_insideTilePosition.y = (float)tileP.y - y;

					Tile tile = _map.GetTileByPoint3(x, y);

					if (tile != null)
					{
						if (marker.Visible == false)
						{
							marker.FadeIn();
						}

						_insideTilePosition.x = tile.transform.position.x - Consts.TILE_HALF_SIZE_IN_UNITS + Consts.TILE_SIZE_IN_UNITS * _insideTilePosition.x;
						_insideTilePosition.y = tile.transform.position.y + Consts.TILE_HALF_SIZE_IN_UNITS - Consts.TILE_SIZE_IN_UNITS * _insideTilePosition.y;

						marker.transform.position = WorldToCanvasPosition(_insideTilePosition);
					}
					else if (marker.Visible == true &&
						(marker.transform.position.x + 0.1f < 0 || marker.transform.position.x - 0.1f > Screen.width ||
						 marker.transform.position.y + 0.1f < 0 || marker.transform.position.y - 0.1f > Screen.height))
					{
						marker.FadeOut();
					}
				}
			}
		}

		public void UpdateMarkersOnZoom(float pMultiplier)
		{
			foreach (Marker m in Markers)
			{
				Vector3 worldPoint = CanvasToWorldPosition(m.Image.rectTransform, m.transform.position) * pMultiplier;
				TweenManager.Instance.Move(m.gameObject, WorldToCanvasPosition(worldPoint), Consts.SCALING_SPEED_FOR_MARKERS);
			}
		}
	}
}