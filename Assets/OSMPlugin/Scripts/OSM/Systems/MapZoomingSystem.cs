using UnityEngine;

namespace OSM
{
	public class MapZoomingSystem : MonoBehaviourSingleton<MapZoomingSystem>
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
#if UNITY_ANDROID || UNITY_IOS

			if(Input.touchCount == 1)
			{
				//MonoTouch
			}
			else if(Input.touchCount == 2)
			{
				//DualTouch
			}
#endif
		}

		private void FinishZooming()
		{
			_isZooming = false;
		}
	}
}