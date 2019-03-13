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

		private void LateUpdate()
		{
			ShowDebugTargetTile();
			UpdateZoomLevel();
		}

		public void CreateDebugFeatures()
		{
			if (_debugScreenLimits == true && _screenLimitsMarker != null)
			{
				GameObject screenLimitMarker = Instantiate(_screenLimitsMarker, transform);
				screenLimitMarker.transform.position = new Vector3(_map._screenBoundaries.left, 0, 0);
				screenLimitMarker.transform.position = new Vector3(_map._screenBoundaries.left, _map._screenBoundaries.top, 0);

				screenLimitMarker = Instantiate(_screenLimitsMarker, transform);
				screenLimitMarker.transform.position = new Vector3(0, _map._screenBoundaries.top, 0);
				screenLimitMarker.transform.position = new Vector3(_map._screenBoundaries.left, _map._screenBoundaries.bottom, 0);

				screenLimitMarker = Instantiate(_screenLimitsMarker, transform);
				screenLimitMarker.transform.position = new Vector3(_map._screenBoundaries.right, 0, 0);
				screenLimitMarker.transform.position = new Vector3(_map._screenBoundaries.right, _map._screenBoundaries.top, 0);

				screenLimitMarker = Instantiate(_screenLimitsMarker, transform);
				screenLimitMarker.transform.position = new Vector3(0, _map._screenBoundaries.bottom, 0);
				screenLimitMarker.transform.position = new Vector3(_map._screenBoundaries.right, _map._screenBoundaries.bottom, 0);
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

		public void InitializeDebugTouches(Vector3 pPosition)
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

		public void UpdateDebugTouches(Vector3 pPosition)
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