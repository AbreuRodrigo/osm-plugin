using UnityEngine;

namespace OSM
{
	public class MapMovementSystem : MonoBehaviour
	{
		[Header("Inertia Settings")]
		[SerializeField]
		private bool _useInertia;
		[SerializeField]
		private float _threshouldStartInertia = 1f;
		[SerializeField]
		private float _inertiaDuration = 1;
		[SerializeField]
		private float _inertiaSpeedMultiplier = 0.1f;

		private bool _isOnInertia;
		private Vector3 _inertiaDirection;
		private float _totalInertiaTime;
		private Vector3 _lastPointBeforeReleasing;
		private Vector3 _releasePoint;
		private float _durationAccumulator = 0;		

		[Header("Dependencies")]
		[SerializeField]
		private Map _map;
		[SerializeField]
		private Camera _mainCamera;

		private bool _isPressed;
		private Vector3 _clickDistance;
		private Vector3 _pointerClickTouch;

		private Vector3 _previousMapPosition = Vector3.zero;
		private Vector3 _mapLimitVector;

		private void LateUpdate()
		{
			if (_isPressed == false && Input.GetMouseButtonDown(0))
			{
				_isPressed = true;
				_clickDistance = _mainCamera.ScreenToWorldPoint(
					new Vector3(-Input.mousePosition.x, -Input.mousePosition.y, _mainCamera.transform.position.z)) - _map.transform.position;

				if (_useInertia == true)
				{
					_isOnInertia = false;
				}
			}
			else if (_isPressed == true && Input.GetMouseButtonUp(0))
			{
				_isPressed = false;

				_releasePoint = _mainCamera.ScreenToWorldPoint(
					new Vector3(-Input.mousePosition.x, -Input.mousePosition.y, _mainCamera.transform.position.z));

				if (_useInertia)
				{					
					_totalInertiaTime = Time.time - _totalInertiaTime;

					if(_totalInertiaTime <= _threshouldStartInertia)
					{
						_isOnInertia = true;
						_inertiaDirection = _releasePoint - _lastPointBeforeReleasing;
					}
					else
					{
						_inertiaDirection = Vector3.zero;
					}

					_durationAccumulator = 0;
				}
			}

			if (_isPressed == true)
			{
				_map.CheckCurrentLayerWithinScreenLimits();

				_lastPointBeforeReleasing = _mainCamera.ScreenToWorldPoint(
					new Vector3(-Input.mousePosition.x, -Input.mousePosition.y, _mainCamera.transform.position.z));

				_map.transform.position = _lastPointBeforeReleasing - _clickDistance;

				if (_useInertia == true)
				{
					_totalInertiaTime = Time.time;
				}
			}

			if(_useInertia == true && _isOnInertia == true && _isPressed == false)
			{
				_map.transform.position += Vector3.Lerp(_inertiaDirection * _inertiaSpeedMultiplier, Vector3.zero, _durationAccumulator);
				_durationAccumulator += Time.smoothDeltaTime;

				if(_durationAccumulator >= _inertiaDuration)
				{
					_durationAccumulator = 0;
					_isOnInertia = false;
				}

				_map.CheckCurrentLayerWithinScreenLimits();
			}

			UpdateMapPositioningSystem();

			/*if (_map.transform.position.x + _map._mapMaxXByZoomLevel < _map._screenBoundaries.right)
			{
				_mapLimitVector.x = -_map._mapMaxXByZoomLevel + _map.ScreenSize.x;
				_mapLimitVector.y = _map.transform.position.y;
				_mapLimitVector.z = _map.transform.position.z;
				_map.transform.position = _mapLimitVector;
			}*/

			/*if (_map.transform.position.x + _map._mapMinXByZoomLevel > _map._screenBoundaries.left)
			{
				_mapLimitVector.x = -_map._mapMinXByZoomLevel + _map.ScreenSize.x;
				_mapLimitVector.y = _map.transform.position.y;
				_mapLimitVector.z = _map.transform.position.z;
				_map.transform.position = _mapLimitVector;
			}*/
		}

		private void UpdateMapPositioningSystem()
		{
			if (_previousMapPosition != Vector3.zero)
			{
				if (_map.transform.position.x == _previousMapPosition.x &&
					_map.transform.position.y == _previousMapPosition.y)
				{
					_map.StopMovements();
				}
				else
				{
					//Horizontal Moviments
					if (_map.transform.position.x > _previousMapPosition.x)
					{
						_map.SetMovingRight();
					}
					else if (_map.transform.position.x < _previousMapPosition.x)
					{
						_map.SetMovingLeft();
					}

					//Vertical Moviments
					if (_map.transform.position.y > _previousMapPosition.y)
					{
						_map.SetMovingUp();
					}
					else if(_map.transform.position.y < _previousMapPosition.y)
					{
						_map.SetMovingDown();
					}
				}
			}

			_previousMapPosition = _map.transform.position;
		}
	}
}