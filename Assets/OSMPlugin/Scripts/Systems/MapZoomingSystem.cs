using UnityEngine;

namespace OSM
{
	public class MapZoomingSystem : MonoBehaviour
	{
		[SerializeField]
		private Map _map;
		[SerializeField]
		private float _zoomSpeed;
		[SerializeField]
		private bool _isZooming = false;
		
		private void LateUpdate()
		{
#if UNITY_EDITOR

			if (_isZooming == false && Input.mouseScrollDelta.y != 0)
			{
				_isZooming = true;

				_map.UpdateTargetCoordinateBasedInTile();

				if (Input.mouseScrollDelta.y == 1)
				{
					_map.PrepareZoomIn(_zoomSpeed, FinishZooming);
				}
				else if (Input.mouseScrollDelta.y == -1)
				{
					_map.PrepareZoomOut(_zoomSpeed, FinishZooming);
				}
			}
#endif
		}

		private void FinishZooming()
		{
			_isZooming = false;
		}
	}
}