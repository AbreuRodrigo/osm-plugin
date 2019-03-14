using UnityEngine;
using InputControls;

namespace OSM
{
	public class MapInputSystem : MonoBehaviourSingleton<MapInputSystem>
	{
		[Header("Inertia Settings")]
		[SerializeField]
		private bool _useInertia;
		[SerializeField]
		private bool _userVerticalMoving;
		[SerializeField]
		private bool _userHorizontalMoving;
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

		[Header("Zooming Controls")]
		[SerializeField]
		private float _zoomSpeed;
		[SerializeField]
		private bool _isZooming = false;
		[SerializeField]
		[Range(Map.MIN_ZOOM_LEVEL, Map.MAX_ZOOM_LEVEL)]
		private float _zoomDebugTransition;

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
			ProcessMapZoom();

			if (_isZooming == false)
			{
				ProcessMapPan();
				UpdateInertiaOverMap();
				UpdateMapPositioningSystem();
				UpdateMapPositionLimits();
			}
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

		#region Pan Control

		private void ProcessMapPan()
		{
			if (_isPressed == false && Input.GetMouseButtonDown(0))
			{
				StopInertia();

				_isPressed = true;

				if (_userHorizontalMoving == true)
				{
					_helperVector.x = -Input.mousePosition.x;
				}

				if (_userVerticalMoving == true)
				{
					_helperVector.y = -Input.mousePosition.y;
				}

				_helperVector.z = _mainCamera.transform.position.z;

				_clickDistance = _mainCamera.ScreenToWorldPoint(_helperVector) - _map.transform.position;
			}

			if (_isPressed == true && Input.GetMouseButtonUp(0))
			{
				_isPressed = false;

				if (_userHorizontalMoving == true)
				{
					_helperVector.x = -Input.mousePosition.x;
				}

				if (_userVerticalMoving == true)
				{
					_helperVector.y = -Input.mousePosition.y;
				}

				_helperVector.z = _mainCamera.transform.position.z;
				_releasePoint = _mainCamera.ScreenToWorldPoint(_helperVector);

				StartInertia();

				_map.CheckCurrentLayerWithinScreenLimits();
			}

			if (_isPressed == true)
			{
				_map.CheckCurrentLayerWithinScreenLimits();

				if (_userHorizontalMoving == true)
				{
					_helperVector.x = -Input.mousePosition.x;
				}

				if (_userVerticalMoving == true)
				{
					_helperVector.y = -Input.mousePosition.y;
				}

				_helperVector.z = _mainCamera.transform.position.z;
				_lastPointBeforeReleasing = _mainCamera.ScreenToWorldPoint(_helperVector);
				_map.transform.position = _lastPointBeforeReleasing - _clickDistance;
			}
		}

		#endregion

		#region Zoom Control

		private void StartZoom()
		{
			_isZooming = true;
		}

		private void EndZoom()
		{
			_isZooming = false;
		}

		private float _initialMapZoom;
		private int _initialDistance;
		private bool _definedDistance;

		private void ProcessMapZoom()
		{
#if UNITY_EDITOR

			/*if (_isZooming == false && Input.mouseScrollDelta.y != 0)
			{
				StartZoom();

				if (Input.mouseScrollDelta.y == 1)
				{
					//_map.PrepareZoomIn(_zoomSpeed, FinishZooming);
				}
				else if (Input.mouseScrollDelta.y == -1)
				{
					//_map.PrepareZoomOut(_zoomSpeed, FinishZooming);
				}
			}*/

			if(_isZooming == false && Input.GetKeyDown(KeyCode.LeftControl))
			{
				StartZoom();

				_initialMapZoom = _map.CurrentZoomLevel;

				_map.MoveCurrentLayerToContainer();
				_map.StartLayerContainerScaling(_mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, _mainCamera.transform.position.z * -1)));
			}
			else if (_isZooming == true && Input.GetKeyUp(KeyCode.LeftControl))
			{
				EndZoom();

				_map.MoveCurrentLayerToMap();
				_map.StopLayerContainerScaling();

				_definedDistance = false;
			}
			else if(_isZooming == true)
			{
				DebugManager.Instance.UpdateDebugTouches(_mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, _mainCamera.transform.position.z * -1)));

				int distance = DebugManager.Instance.GetDisanceBetweenTouches();

				if(_definedDistance == false)
				{
					_definedDistance = true;
					_initialDistance = distance;
				}

				float diff = distance / _map.mapHorizontalLimitInUnits;

				Vector3 scale = Vector3.one + new Vector3(diff, diff, 0);
				_map.LayerContainer.localScale = scale;
			}

#endif

#if !UNITY_EDITOR && UNITY_ANDROID

			if (Input.touchCount == 2)
			{
				StartZoom();

				Touch t1 = Input.GetTouch(0);
				Touch t2 = Input.GetTouch(1);

				float d = Vector2.Distance(t2.position, t1.position);

				DebugManager.Instance.UpdatePinchLevel(d);

				//Move Current to layer container

				//Do scaling on the layerContainer
			}
			else if(_isZooming == true && Input.touchCount < 2)
			{
				EndZoom();
			}
#endif
		}

		#endregion
	}
}