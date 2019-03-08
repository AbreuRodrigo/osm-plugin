using UnityEngine;

namespace OSM
{
	public class MapMovementSystem : MonoBehaviour
	{
		[Header("Inertia Settings")]
		[SerializeField]
		private bool _useInertia;
		[SerializeField]
		private float _inertiaDuration = 1;
		[SerializeField]
		private float _inertiaSpeedMultiplier = 0.1f;
		[SerializeField]
		private float _inertiaSpeedMultiplierMobile = 0.1f;

		private bool _isOnInertia;
		private Vector3 _inertiaDirection;
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
		private Vector3 _helperVector;

		private void Start()
		{
			if (Application.platform == RuntimePlatform.Android ||
				Application.platform == RuntimePlatform.IPhonePlayer)
			{
				_inertiaSpeedMultiplier = _inertiaSpeedMultiplierMobile;
			}
		}

		private void LateUpdate()
		{
			if (_isPressed == false && Input.GetMouseButtonDown(0))
			{
				StopInertia();

				_isPressed = true;
				_helperVector.x = -Input.mousePosition.x;
				_helperVector.y = -Input.mousePosition.y;
				_helperVector.z = _mainCamera.transform.position.z;

				_clickDistance = _mainCamera.ScreenToWorldPoint(_helperVector) - _map.transform.position;								
			}
			
			if (_isPressed == true && Input.GetMouseButtonUp(0))
			{
				_isPressed = false;

				_helperVector.x = -Input.mousePosition.x;
				_helperVector.y = -Input.mousePosition.y;
				_helperVector.z = _mainCamera.transform.position.z;
				_releasePoint = _mainCamera.ScreenToWorldPoint(_helperVector);

				StartInertia();

				_map.CheckCurrentLayerWithinScreenLimits();
			}

			if (_isPressed == true)
			{
				_map.CheckCurrentLayerWithinScreenLimits();

				_helperVector.x = -Input.mousePosition.x;
				_helperVector.y = -Input.mousePosition.y;
				_helperVector.z = _mainCamera.transform.position.z;
				_lastPointBeforeReleasing = _mainCamera.ScreenToWorldPoint(_helperVector);
				_map.transform.position = _lastPointBeforeReleasing - _clickDistance;
			}

			UpdateInertiaOverMap();
			UpdateMapPositioningSystem();
			UpdateMapPositionLimits();
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
					else if (_map.transform.position.y < _previousMapPosition.y)
					{
						_map.SetMovingDown();
					}
				}
			}

			_previousMapPosition = _map.transform.position;
		}

		private void UpdateMapPositionLimits()
		{
			if (_map.transform.position.y + _map._mapMinYByZoomLevel < _map._screenBoundaries.top)
			{
				_mapLimitVector = _map.transform.position;
				_mapLimitVector.y = _map._screenBoundaries.top - _map._mapMinYByZoomLevel;
				_map.transform.position = _mapLimitVector;
			}
			if (_map.transform.position.y - _map._mapMaxYByZoomLevel > _map._screenBoundaries.bottom)
			{
				_mapLimitVector = _map.transform.position;
				_mapLimitVector.y = _map._screenBoundaries.bottom + _map._mapMaxYByZoomLevel;
				_map.transform.position = _mapLimitVector;
			}
		}


		#region Inertia
		private void UpdateInertiaOverMap()
		{
			if (_useInertia == true && _isOnInertia == true && _isPressed == false)
			{
				_map.transform.position += Vector3.Lerp(_inertiaDirection * _inertiaSpeedMultiplier, Vector3.zero, _durationAccumulator);
				_durationAccumulator += Time.smoothDeltaTime;

				if (_durationAccumulator >= _inertiaDuration)
				{
					_durationAccumulator = 0;
					_isOnInertia = false;
				}

				_map.CheckCurrentLayerWithinScreenLimits();
			}
		}

		private void StartInertia()
		{
			if (_useInertia)
			{
				_isOnInertia = true;
				_inertiaDirection = _releasePoint - _lastPointBeforeReleasing;

				_durationAccumulator = 0;
			}
		}

		private void StopInertia()
		{
			if (_useInertia == true)
			{
				_isOnInertia = false;
			}
		}

		#endregion
	}
}