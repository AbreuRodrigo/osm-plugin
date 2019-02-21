using UnityEngine;

namespace OSM
{
	public class MapMovementSystem : MonoBehaviour
	{
		[SerializeField]
		private Map _map;
		[SerializeField]
		private Camera _mainCamera;

		private bool _isPressed;
		private Vector3 _clickDistance;
		private Vector3 _pointerClickTouch;

		private void Update()
		{
			if(_isPressed == false && Input.GetMouseButtonDown(0))
			{
				_isPressed = true;
				_clickDistance = _mainCamera.ScreenToWorldPoint(new Vector3(-Input.mousePosition.x, _mainCamera.transform.position.y, _mainCamera.transform.position.z)) - _map.transform.position;
			}
			else if(_isPressed == true && Input.GetMouseButtonUp(0))
			{
				_isPressed = false;
			}

			if(_isPressed == true)
			{
				_map.CheckCurrentLayerWithinScreenLimits();
				_map.transform.position = _mainCamera.ScreenToWorldPoint(new Vector3(-Input.mousePosition.x, _mainCamera.transform.position.y, _mainCamera.transform.position.z)) - _clickDistance;
			}
		}
	}
}