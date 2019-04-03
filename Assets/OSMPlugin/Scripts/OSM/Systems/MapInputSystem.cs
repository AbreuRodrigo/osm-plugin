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
		[SerializeField]
		private GameObject _world;

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
		private int _zoomGapOnPinch = 3;
		[SerializeField]
		private bool _useWorldZoom;

		private void Start()
		{
			Input.multiTouchEnabled = true;

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
			if (_map.transform.position.y + _map._mapMinYByZoomLevel < _map.ScreenBoundaries.top)
			{
				_mapLimitVector = _map.transform.position;
				_mapLimitVector.y = _map.ScreenBoundaries.top - _map._mapMinYByZoomLevel;
				_map.transform.position = _mapLimitVector;
			}
			if (_map.transform.position.y - _map._mapMaxYByZoomLevel > _map.ScreenBoundaries.bottom)
			{
				_mapLimitVector = _map.transform.position;
				_mapLimitVector.y = _map.ScreenBoundaries.bottom + _map._mapMaxYByZoomLevel;
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

		public bool userPinchSimulatorForZoom;
		public bool useButtonsForZoom;

		private float _initialMapZoom;
		private float _zoomPercent;

		private bool _pinchBegun;
		private bool _pinchEnd;

		private Vector3 _initialScale;
		private Vector3 _targetScale;

		private bool _startPinching;
		private float _initialPinchDistance;

		private ZoomLevel _zoomLevel;
		private int _layerContainerScale;

		private float accumulator;
		private bool isZoomingInWithButton;
		private bool isZoomingOutWithButton;

		/*
		 *  ZOOM LEVELS
		 *  
		 *  zoom	scale	sum
		 *  3		x1		+0
		 *  4		x2		+1
		 *  5		x4		+2
		 *  6		x8		+3
		 *  7		x16		+4
		 *  8		x32		+5
		 *  9		x64		+6
		 *  10		x128	+7	
		 *  11		x256	+8
		 *  12		x512	+9
		 *  13		x1024	+10
		 *  14		x2048	+11
		 *  15		x4096	+12
		 *  16		x8192	+13
		 *  17		x16384	+14
		 *  18		x32768	+15
		 *  19		x65536	+16
		 */
		private void ExecuteZoomProcedures()
		{
			int zoomDiff = (int) _map.CurrentZoomLevel + _zoomGapOnPinch;

			if(zoomDiff > Consts.MAX_ZOOM_LEVEL)
			{
				_zoomGapOnPinch = Consts.MAX_ZOOM_LEVEL - (int)_map.CurrentZoomLevel;
			}

			_zoomLevel = new ZoomLevel(_zoomGapOnPinch);

			if (_isZooming == false && _pinchBegun)
			{
				StartZoom();

				_initialMapZoom = _map.CurrentZoomLevel;

				if (_useWorldZoom == true)
				{
					_initialScale = _world.transform.localScale;
				}
				else
				{
					_initialScale = _map.transform.localScale;
				}

				_targetScale = _initialScale * _zoomLevel.scale;

				_map.MoveCurrentLayerToContainer();
			}
			else if (_isZooming == true && _pinchEnd)
			{
				//TODO:  Work on the zoom fractions

				EndZoom();

				_layerContainerScale = (int) Mathf.Round(_map.LayerContainer.localScale.x);
				_layerContainerScale = ZoomSystem.GetSumByScale(_layerContainerScale);

				_map.MoveCurrentLayerToMap();

				StopLayerContainerScaling();

				_zoomLevel.ResetLevel(_layerContainerScale);
				_map.ExecuteZoomingWithPinch(_zoomLevel.sum, _zoomLevel.scale);

				_pinchBegun = false;
				_pinchEnd = false;
			}
			else if (_isZooming == true)
			{				
				float distance = DebugManager.Instance.GetDisanceBetweenTouches() - _initialPinchDistance;
				_zoomPercent = distance / _map.mapHorizontalLimitInUnits;

				if(_zoomPercent > 1.0f)
				{
					_zoomPercent = 1.0f;
				}

				if (_useWorldZoom == true)
				{
					Vector3 factor = Vector3.Lerp(_initialScale, _targetScale, _zoomPercent);
					_map.ApplyPinchZoom(factor.x);
				}
				else
				{
					_map.LayerContainer.localScale = Vector3.Lerp(_initialScale, _targetScale, _zoomPercent);
				}
			}
		}
						
		private void ProcessMapZoom()
		{
#if UNITY_EDITOR
			if (userPinchSimulatorForZoom == true)
			{
				if (Input.GetKeyDown(KeyCode.LeftControl))
				{
					_pinchBegun = true;
					_pinchEnd = false;

					StartLayerContainerScalingEditor(_mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, _mainCamera.transform.position.z * -1)));
				}
				else if (Input.GetKeyUp(KeyCode.LeftControl))
				{
					_pinchBegun = false;
					_pinchEnd = true;
				}

				if (_isZooming == true)
				{
					DebugManager.Instance.UpdateDebugTouchesEditor(_mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, _mainCamera.transform.position.z * -1)));
				}
			}
			else if (useButtonsForZoom == true)
			{
				DoZoomWithButtonsLogic();
			}
#endif
#if !UNITY_EDITOR && UNITY_ANDROID

			if(Input.touchCount > 0)
			{
				Vector3 p1 = Vector3.zero, p2 = Vector3.zero;

				if (Input.touchCount == 2)
				{
					Touch t1 = Input.GetTouch(0);
					Touch t2 = Input.GetTouch(1);

					_pinchBegun = true;
					_pinchEnd = false;

					p1 = _mainCamera.ScreenToWorldPoint(new Vector3(t1.position.x, t1.position.y, _mainCamera.transform.position.z * -1));
					p2 = _mainCamera.ScreenToWorldPoint(new Vector3(t2.position.x, t2.position.y, _mainCamera.transform.position.z * -1));

					//When began, will define the pinch intitial distance
					if (t1.phase == TouchPhase.Began && t2.phase == TouchPhase.Began)
					{
						_initialPinchDistance = Vector3.Distance(p1, p2);
					}
					
					//When released the touches will stop the pinch function and reset the intial pinch distance
					if (t1.phase == TouchPhase.Ended || t2.phase == TouchPhase.Ended)
					{
						_pinchBegun = false;
						_pinchEnd = true;

						_initialPinchDistance = 0;
					}
					else//Otherwise when not end will keep the container scaling
					{
						StartLayerContainerScalingMobile(p1, p2);
					}
				}

				if(_isZooming == true)
				{
					DebugManager.Instance.UpdateDebugTouchesMobile(p1, p2);
				}
			}
#endif

			ExecuteZoomProcedures();
		}

		#endregion

		/// <summary>
		/// TEMP - LAYER CONTAINER STUFF 
		/// </summary>

		private bool _isScaling;

		public void StartLayerContainerScalingEditor(Vector3 pInitPosition)
		{
			if (_isScaling == false)
			{
				_isScaling = true;
				DebugManager.Instance.InitializeDebugTouchesEditor(pInitPosition);
			}
		}

		public void StartLayerContainerScalingMobile(Vector3 pTouch1, Vector3 pTouch2)
		{
			if (_isScaling == false)
			{
				_isScaling = true;
				DebugManager.Instance.InitializeDebugTouchesMobile(pTouch1, pTouch2);
			}
		}

		public void StopLayerContainerScaling()
		{
			if ( _isScaling == true)
			{
				_isScaling = false;
				DebugManager.Instance.DisableDebugTouch();
			}
		}

		private void DoZoomWithButtonsLogic()
		{
			if (Input.GetKeyDown(KeyCode.S))
			{
				isZoomingInWithButton = true;
			}
			else if (Input.GetKeyUp(KeyCode.S))
			{
				isZoomingInWithButton = false;
			}

			if (Input.GetKeyDown(KeyCode.Z))
			{
				isZoomingOutWithButton = true;
			}
			else if (Input.GetKeyUp(KeyCode.Z))
			{
				isZoomingOutWithButton = false;
			}

			if (isZoomingInWithButton == true)
			{
				accumulator += 0.01f;
			}
			if (isZoomingOutWithButton == true)
			{
				accumulator -= 0.01f;
			}

			if (accumulator > Consts.MAX_ZOOM_LEVEL)
			{
				accumulator = Consts.MAX_ZOOM_LEVEL;
			}
			if(accumulator < Consts.MIN_ZOOM_LEVEL)
			{
				accumulator = Consts.MIN_ZOOM_LEVEL;
			}

			if(_world != null && _useWorldZoom == true && accumulator >= Consts.MIN_ZOOM_LEVEL && 
			  (isZoomingInWithButton == true || isZoomingOutWithButton == true))
			{
				_map.ApplyPinchZoom(accumulator - (Consts.MIN_ZOOM_LEVEL - 1));
			}
		}
	}
}