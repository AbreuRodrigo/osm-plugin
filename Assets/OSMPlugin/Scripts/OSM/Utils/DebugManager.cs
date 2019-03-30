using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OSM
{
	public class DebugManager : MonoBehaviourSingleton<DebugManager>
	{
		[Header("UI")]
		[SerializeField]
		private Text _zoomLevelUI;
		[SerializeField]
		private Text _pinchLevelUI;

		[Space(5)]
		[Header("Debug")]
		[SerializeField]
		private bool _debugScreenLimits;
		[SerializeField]
		private bool _showDebugTargetTile;
		[SerializeField]
		private bool _updateZoomLevel;
		[SerializeField]
		private bool _updatePinchLevel;
		[SerializeField]
		private bool _showLineMarkers;
		[SerializeField]
		private GameObject _screenLimitsMarker;

		[Header("Dependencies")]
		[SerializeField]
		private Map _map;
		[SerializeField]
		private Camera _mainCamera;
		[SerializeField]
		private Camera _debugCamera;
		[SerializeField]
		private GameObject _debugTouch1;
		[SerializeField]
		private GameObject _debugTouch2;
		[SerializeField]
		private GameObject _debugMiddlePoint;

		private Vector3 _initialDebugPosition;

		private Vector3 _lineMarker1;
		private Vector3 _lineMarker2;
		private Vector3 _lineMarker3;
		private Vector3 _lineMarker4;

		private void LateUpdate()
		{
			ShowDebugTargetTile();
			UpdateZoomLevel();
			ShowLineMarkers();
		}

		public void CreateDebugFeatures()
		{
			if (_debugScreenLimits == true && _screenLimitsMarker != null)
			{
				GameObject screenLimitMarker = Instantiate(_screenLimitsMarker, transform);
				screenLimitMarker.transform.position = new Vector3(_map.ScreenBoundaries.left, 0, 0);
				screenLimitMarker.transform.position = new Vector3(_map.ScreenBoundaries.left, _map.ScreenBoundaries.top, 0);
				_lineMarker1 = screenLimitMarker.transform.position;

				screenLimitMarker = Instantiate(_screenLimitsMarker, transform);
				screenLimitMarker.transform.position = new Vector3(0, _map.ScreenBoundaries.top, 0);
				screenLimitMarker.transform.position = new Vector3(_map.ScreenBoundaries.left, _map.ScreenBoundaries.bottom, 0);
				_lineMarker2 = screenLimitMarker.transform.position;

				screenLimitMarker = Instantiate(_screenLimitsMarker, transform);
				screenLimitMarker.transform.position = new Vector3(_map.ScreenBoundaries.right, 0, 0);
				screenLimitMarker.transform.position = new Vector3(_map.ScreenBoundaries.right, _map.ScreenBoundaries.top, 0);
				_lineMarker3 = screenLimitMarker.transform.position;

				screenLimitMarker = Instantiate(_screenLimitsMarker, transform);
				screenLimitMarker.transform.position = new Vector3(0, _map.ScreenBoundaries.bottom, 0);
				screenLimitMarker.transform.position = new Vector3(_map.ScreenBoundaries.right, _map.ScreenBoundaries.bottom, 0);
				_lineMarker4 = screenLimitMarker.transform.position;
			}
		}

		private void ShowDebugTargetTile()
		{
			if (_showDebugTargetTile == true)
			{
				Vector3 mp = Vector3.zero;

				foreach (Tile tile in _map.CurrentLayer.Tiles)
				{
					if (_map.CheckTileOnScreen(tile.transform.position))
					{
						mp = _mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, _mainCamera.transform.position.z * -1));

						if (tile.transform.position.x + Map.TILE_HALF_SIZE_IN_UNITS > mp.x && tile.transform.position.x - Map.TILE_HALF_SIZE_IN_UNITS < mp.x &&
							tile.transform.position.y + Map.TILE_HALF_SIZE_IN_UNITS > mp.y && tile.transform.position.y - Map.TILE_HALF_SIZE_IN_UNITS < mp.y)
						{
							tile._meshRenderer.material.color = Color.green;
						}
						else
						{
							tile._meshRenderer.material.color = Color.white;
						}
					}
				}

				Debug.DrawLine(Vector3.zero, mp);
			}
		}

		private void ShowLineMarkers()
		{
			if(_showLineMarkers)
			{
				Debug.DrawLine(_lineMarker1, _lineMarker3, Color.green);
				Debug.DrawLine(_lineMarker3, _lineMarker4, Color.green);
				Debug.DrawLine(_lineMarker4, _lineMarker2, Color.green);
				Debug.DrawLine(_lineMarker2, _lineMarker1, Color.green);
			}
		}

		public void UpdatePinchLevel(float pPinchLevel)
		{
			if (_updatePinchLevel == true && _pinchLevelUI != null)
			{
				_pinchLevelUI.text = "Pinch: " + pPinchLevel;
			}
		}

		public void UpdatePinchLevel(Vector2 pPinchDelta)
		{
			if (_updatePinchLevel == true && _pinchLevelUI != null)
			{
				_pinchLevelUI.text = "Pinch: " + pPinchDelta;
			}
		}

		public void InitializeDebugTouchesEditor(Vector3 pPosition)
		{
			_initialDebugPosition = pPosition;

			if (_debugTouch1 != null)
			{
				_debugTouch1.gameObject.SetActive(true);
				pPosition.z = _debugTouch1.transform.position.z;
				_debugTouch1.transform.position = pPosition;
			}

			if (_debugTouch2 != null)
			{
				_debugTouch2.gameObject.SetActive(true);
				pPosition.z = _debugTouch2.transform.position.z;
				_debugTouch2.transform.position = pPosition;
			}

			if(_debugMiddlePoint != null)
			{
				_debugMiddlePoint.gameObject.SetActive(true);
				_debugMiddlePoint.transform.position = pPosition;
			}
		}

		public void UpdateDebugTouchesEditor(Vector3 pPosition)
		{
			if (_debugTouch1 != null)
			{
				_debugTouch1.transform.position = _initialDebugPosition - (pPosition - _initialDebugPosition);
			}

			if (_debugTouch2 != null)
			{
				_debugTouch2.transform.position = pPosition;
			}
		}

		public void UpdatePoint(Vector3 point)
		{
			_debugTouch1.gameObject.SetActive(true);
			_debugTouch1.transform.position = point;
		}

		public void InitializeDebugTouchesMobile(Vector3 pTouch1, Vector3 pTouch2)
		{
			_initialDebugPosition = pTouch1 + (pTouch2 - pTouch1) * 0.5f; //A + (B - A) * percent

			if (_debugTouch1 != null)
			{
				_debugTouch1.gameObject.SetActive(true);
				pTouch1.z = _debugTouch1.transform.position.z;
				_debugTouch1.transform.position = pTouch1;
			}

			if (_debugTouch2 != null)
			{
				_debugTouch2.gameObject.SetActive(true);
				pTouch2.z = _debugTouch2.transform.position.z;
				_debugTouch2.transform.position = pTouch2;
			}

			if (_debugMiddlePoint != null)
			{
				_debugMiddlePoint.gameObject.SetActive(true);
				_debugMiddlePoint.transform.position = _initialDebugPosition;
			}
		}

		public void UpdateDebugTouchesMobile(Vector3 pTouch1, Vector3 pTouch2)
		{
			if (_debugTouch1 != null)
			{
				if(_debugTouch1.gameObject.activeInHierarchy == false)
				{
					_debugTouch1.gameObject.SetActive(true);
				}

				_debugTouch1.transform.position = pTouch1;
			}

			if (_debugTouch2 != null)
			{
				if (_debugTouch2.gameObject.activeInHierarchy == false)
				{
					_debugTouch2.gameObject.SetActive(true);
				}

				_debugTouch2.transform.position = pTouch2;
			}
		}

		public void DisableDebugTouch()
		{
			if (_debugTouch1 != null)
			{
				_debugTouch1.gameObject.SetActive(false);
			}
			if (_debugTouch2 != null)
			{
				_debugTouch2.gameObject.SetActive(false);
			}
			if (_debugMiddlePoint != null)
			{
				_debugMiddlePoint.gameObject.SetActive(false);
			}
		}

		public float GetDisanceBetweenTouches()
		{
			return Vector3.Distance(_debugTouch1.transform.position, _debugTouch2.transform.position);
		}

		private void UpdateZoomLevel()
		{
			if(_updateZoomLevel == true && _zoomLevelUI != null)
			{
				_zoomLevelUI.text = "Zoom: " + _map.CurrentZoomLevel;
			}
		}
	}
}