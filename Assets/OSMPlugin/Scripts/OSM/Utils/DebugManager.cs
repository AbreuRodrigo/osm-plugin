using UnityEngine;
using UnityEngine.UI;

namespace OSM
{
	public class DebugManager : MonoBehaviourSingleton<DebugManager>
	{
		[Header("UI")]
		[SerializeField]
		private Text _zoomLevelUI;

		[Space(5)]
		[Header("Debug")]
		[SerializeField]
		private bool _debugScreenLimits;
		[SerializeField]
		private bool _showDebugTargetTile;
		[SerializeField]
		private bool _updateZoomLevel;
		[SerializeField]
		private GameObject _screenLimitsMarker;

		[Header("Dependencies")]
		[SerializeField]
		private Map _map;
		[SerializeField]
		private Camera mainCamera;
		
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
						mp = mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, mainCamera.transform.position.z * -1));

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

		private void UpdateZoomLevel()
		{
			if(_updateZoomLevel == true)
			{
				_zoomLevelUI.text = "Zoom: " + _map.CurrentZoomLevel;
			}
		}
	}
}