﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OSM
{
	public class Map : MonoBehaviour
	{
		#region Constants
		public const string CURRENT_LAYER = "CurrentLayer";
		public const string OTHER_LAYER = "OtherLayer";
		
		public const int MIN_ZOOM_LEVEL = 3;
		public const int MAX_ZOOM_LEVEL = 19;

		public const int TILE_SIZE_IN_PIXELS = 256;
		public const float TILE_SIZE_IN_UNITS = TILE_SIZE_IN_PIXELS * 0.01f;
		public const float TILE_HALF_SIZE_IN_UNITS = TILE_SIZE_IN_PIXELS * 0.005f;
		public const float TILE_QUARTER_SIZE_IN_UNITS = TILE_SIZE_IN_PIXELS * 0.0025f;
		public const string LAYER_BASE_NAME = "Layer";
		#endregion
				
		[Header("Layer Properties")]
		[SerializeField]
		private List<LayerConfig> layerConfigs;

		[Header("General Properties")]
		[SerializeField]
		private double _currentLatitude = 49.2674573;
		[SerializeField]
		private double _currentLongitude = -123.0930032;

		public float _mapMinYByZoomLevel;
		public float _mapMaxYByZoomLevel;

		[Header("Dependencies")]
		public Camera mainCamera;
		public GameObject _world;

		[SerializeField]
		[Range(MIN_ZOOM_LEVEL, MAX_ZOOM_LEVEL)]
		private int _currentZoomLevel = MIN_ZOOM_LEVEL;

		private float _previousZoomLevel = MIN_ZOOM_LEVEL;

		[Header("Prefabs")]
		[SerializeField]
		private Tile _tileTemplate;
		[SerializeField]
		private Layer _layerTemplate;

		public Layer CurrentLayer { get; private set; }
		public Layer OtherLayer { get; private set; }
		public Layer LayerReplica { get; private set; }

		public int NextZoomLevel { get; set; }

		private Layer _auxLayer = null;

		private float _tileValidationSeconds = 1f;
		private float _tileValidationCounter = 0;
		private float _fadingDurationForLayers = 0.5f;
		private float _scalingDurationForLayers = 0.5f;
		private float _scalingMoveSpeedForMarkers = 0.125f;

		public int _tileCycleLimit;

		public ScreenBoundaries ScreenBoundaries { get; private set; }

		public float mapHorizontalLimitInUnits;

		//Movement Detection
		private bool _isMovingLeft;
		private bool _isMovingRight;
		private bool _isMovingUp;
		private bool _isMovingDown;
		private bool _isStopped;

		public float TileSize { get; private set; }
		public float CurrentZoomLevel { get { return _currentZoomLevel; } }

		[SerializeField]
		private TileData _centerTileData;
		private Vector3 _helperVector3;
		private Vector3 _insideTilePosition;

		private bool _onZoomTransition;
		private float _zoomTransitionTimer;
		private Action _onCompleteZoomInTransition;

		private bool _isPlacingMarker;

		private float _tapCoolDown = 0.5f;
		private float _tapTiming;
		private int _tapCounter;

		//Vars
		private int _distX = 0;
		private int _distY = 0;
		private int _nextX = 0;
		private int _nextY = 0;
		private int _correctionX = 0;

		public float zoomMultiplier = 1;

		[SerializeField]
		private GameObject _layerContainer;
		public Transform LayerContainer
		{
			get
			{
				return _layerContainer.transform;
			}
		}

		private Vector3 _mapDeviationCorrection;

		private void Start()
		{
			ApplyPinchZoom(zoomMultiplier);

			if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
			{
				Application.targetFrameRate = 60;
			}

			StopMovements();
			InitializeMap();
			InitializeLayers();
			InitialZoom();

			CalculateScreenBoundaries();
			CheckCurrentLayerWithinScreenLimits(false);

			DebugManager.Instance.CreateDebugFeatures();			
		}

		private void LateUpdate()
		{
			if (_tileValidationCounter >= _tileValidationSeconds)
			{
				ValidateOnScreenTiles();
				_tileValidationCounter = 0;
			}

			_tileValidationCounter += Time.deltaTime;

			RunZoomInTransactionTimeLogic();				
			
			ProcessMarkerSpawningInput();

			if (_isPlacingMarker)
			{
				Vector3 point = mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, mainCamera.transform.position.z * -1));
				
				Tile tile = GetTileByVectorPointForMarker(point);

				float tileXTopLeft = tile.transform.position.x - TILE_HALF_SIZE_IN_UNITS;
				float tileYTopLeft = tile.transform.position.y + TILE_HALF_SIZE_IN_UNITS;

				Vector3 positionInTile = point - new Vector3(tileXTopLeft, tileYTopLeft, 0);
				positionInTile.y *= -1;

				positionInTile /= TILE_SIZE_IN_UNITS;

				Coordinates coords = OSMGeoHelper.TileToGeoPos(tile.TileData.x + positionInTile.x, tile.TileData.y + positionInTile.y, tile.TileData.zoom);

				Marker marker = MarkerManager.Instance.CreateMarkerFallingDown(point);
				marker.GeoCoordinates = coords;

				_isPlacingMarker = false;
			}
		}

		private void ProcessMarkerSpawningInput()
		{
#if UNITY_ANDROID

			if (Input.touchCount == 1)
			{
				if (Input.GetTouch(0).phase == TouchPhase.Began)
				{
					_tapCounter++;
				}
			}

			if (_tapCounter > 0)
			{
				_tapTiming += Time.deltaTime;
			}

			if (_tapCounter == 2)
			{
				if (_tapTiming <= _tapCoolDown)
				{
					_isPlacingMarker = true;
				}

				_tapCounter = 0;
				_tapTiming = 0;
			}
#endif
#if UNITY_EDITOR

			if (Input.GetMouseButtonUp(0))
			{
				_tapCounter++;
			}

			if (_tapCounter > 0)
			{
				_tapTiming += Time.deltaTime;
			}

			if (_tapCounter == 2)
			{
				if (_tapTiming <= _tapCoolDown)
				{
					_isPlacingMarker = true;
				}

				_tapCounter = 0;
				_tapTiming = 0;
			}
			else 
			{
				_isPlacingMarker = false;
			}
#endif
		}

		private void RunZoomInTransactionTimeLogic()
		{
			if (_onZoomTransition == true)
			{
				if (_zoomTransitionTimer > 0)
				{
					_zoomTransitionTimer -= Time.deltaTime;

					if (_zoomTransitionTimer <= 0)
					{
						_zoomTransitionTimer = 0;
						_onZoomTransition = false;
						_onCompleteZoomInTransition?.Invoke();
					}
				}
			}
		}

		private void InitializeMap()
		{
			//Tile size base should be multiplied because it will be used to organize the tiles side by side and it considers the tile bounds to do so
			TileSize = TILE_SIZE_IN_UNITS * zoomMultiplier;
		}

		private void InitializeLayers()
		{
			int layerIndex = 0;

			foreach (LayerConfig layerConfig in layerConfigs)
			{
				Layer layer = Instantiate(_layerTemplate, transform);
				layer.Index = layerIndex;
				layer.Config = layerConfig;
				layer.SetTileSize(TileSize);
				layer.CreateTilesByLayer(_tileTemplate, _currentZoomLevel);
				layer.OrganizeTilesAsGrid();

				if (CurrentLayer == null)
				{
					CurrentLayer = layer;
					CurrentLayer.gameObject.name = CURRENT_LAYER;
				}
				else if (OtherLayer == null)
				{
					OtherLayer = layer;
					OtherLayer.gameObject.name = OTHER_LAYER;
				}

				layerIndex++;
			}
		}

		private Tile GetCenterTileOnCurrentLayer(bool pUseExactCenter)
		{
			Tile centerTile = null;

			foreach (Tile tile in CurrentLayer.Tiles)
			{
				if (CheckTileOnScreen(tile.transform.position))
				{
					if (pUseExactCenter == true)
					{
						if (tile.transform.position.x - TILE_HALF_SIZE_IN_UNITS <= 0 && tile.transform.position.x + TILE_HALF_SIZE_IN_UNITS >= 0 &&
							tile.transform.position.y + TILE_HALF_SIZE_IN_UNITS >= 0 && tile.transform.position.y - TILE_HALF_SIZE_IN_UNITS <= 0)
						{
							return tile;
						}
					}
					else
					{
						if (tile.X % 2 == 0 && tile.Y % 2 == 0 &&
							tile.transform.position.x - TILE_SIZE_IN_UNITS <= 0 && tile.transform.position.x + TILE_SIZE_IN_UNITS >= 0 &&
							tile.transform.position.y + TILE_SIZE_IN_UNITS >= 0 && tile.transform.position.y - TILE_SIZE_IN_UNITS <= 0)
						{
							return tile;
						}
					}
				}
			}

			return centerTile;
		}

		private Tile GetCenterTileOnOtherLayer(bool pUseExactCenter)
		{
			Tile centerTile = null;

			foreach (Tile tile in OtherLayer.Tiles)
			{
				if (CheckTileOnScreen(tile.transform.position))
				{
					if (pUseExactCenter == true)
					{
						if (tile.transform.position.x - TILE_SIZE_IN_UNITS <= 0 && tile.transform.position.x + TILE_SIZE_IN_UNITS >= 0 &&
							tile.transform.position.y + TILE_SIZE_IN_UNITS >= 0 && tile.transform.position.y - TILE_SIZE_IN_UNITS <= 0)
						{
							return tile;
						}
					}
					else
					{
						if (tile.X % 2 == 0 && tile.Y % 2 == 0 &&
							tile.transform.position.x - TILE_SIZE_IN_UNITS <= 0 && tile.transform.position.x + TILE_SIZE_IN_UNITS >= 0 &&
							tile.transform.position.y + TILE_SIZE_IN_UNITS >= 0 && tile.transform.position.y - TILE_SIZE_IN_UNITS <= 0)
						{
							return tile;
						}
					}
				}
			}

			return centerTile;
		}

		//TODO: REVIEW
		private Tile GetCenterTileOnCurrentLayerByPinch()
		{
			float distance = -1;
			Tile centerTile = null;

			foreach (Tile tile in CurrentLayer.Tiles)
			{
				if(distance == -1)
				{
					distance = Vector3.Distance(tile.transform.position, Vector3.zero);
					centerTile = tile;
				}
				else if(Vector3.Distance(tile.transform.position, Vector3.zero) < distance)
				{
					distance = Vector3.Distance(tile.transform.position, Vector3.zero);
					centerTile = tile;
				}
			}

			return centerTile;
		}

		//TODO: REVIEW
		private Tile GetCenterTileOnOtherLayerByPinch()
		{
			float distance = -1;
			Tile centerTile = null;

			foreach (Tile tile in OtherLayer.Tiles)
			{
				if (distance == -1)
				{
					distance = Vector3.Distance(tile.transform.position, Vector3.zero);
					centerTile = tile;
				}
				else if (Vector3.Distance(tile.transform.position, Vector3.zero) < distance)
				{
					distance = Vector3.Distance(tile.transform.position, Vector3.zero);
					centerTile = tile;
				}
			}

			return centerTile;
		}

		//Multiplier is correct in this one already, it should only multiply the final calculated value for mapMinYByZoomLevel and mapMaxYByZoomLevel
		public void CalculateScreenBoundaries()
		{
			ScreenBoundaries = new ScreenBoundaries(mainCamera);

			_tileCycleLimit = (int)Mathf.Pow(2, _currentZoomLevel) - 1;

			_mapMinYByZoomLevel = (_centerTileData.y * TILE_SIZE_IN_UNITS + TILE_HALF_SIZE_IN_UNITS) * zoomMultiplier;
			_mapMaxYByZoomLevel = ((_tileCycleLimit - _centerTileData.y) * TILE_SIZE_IN_UNITS + TILE_HALF_SIZE_IN_UNITS) * zoomMultiplier;

			mapHorizontalLimitInUnits = (int)(ScreenBoundaries.right * 2.0f);
		}

		protected void ValidateOnScreenTiles()
		{
			foreach (Tile tile in CurrentLayer.Tiles)
			{
				if (CheckTileOnScreen(tile.transform.position) && tile.TextureUpToDate == false)
				{
					DoTileDownload(tile.TileData);
				}
			}
		}

		protected void ValidateAllTiles()
		{
			foreach (Tile tile in CurrentLayer.Tiles)
			{
				if (tile.TextureUpToDate == false)
				{
					DoTileDownload(tile.TileData);
				}
			}
		}

		protected bool ValidateZoomLimits()
		{
			if (_currentZoomLevel < MIN_ZOOM_LEVEL)
			{
				_currentZoomLevel = MIN_ZOOM_LEVEL;

				return false;
			}

			if (_currentZoomLevel > MAX_ZOOM_LEVEL)
			{
				_currentZoomLevel = MAX_ZOOM_LEVEL;

				return false;
			}

			return true;
		}

		public void ZoomIn()
		{
			ExecuteZooming(1);
		}

		public void ZoomOut()
		{
			ExecuteZooming(-1);
		}
		
		public void InitialZoom()
		{
			if (_currentZoomLevel < MIN_ZOOM_LEVEL)
			{
				_currentZoomLevel = MIN_ZOOM_LEVEL;
			}

			if (_currentZoomLevel > MAX_ZOOM_LEVEL)
			{
				_currentZoomLevel = MAX_ZOOM_LEVEL;
			}

			NextZoomLevel = _currentZoomLevel;

			DefineCenterTileOnCurrentLayer();
			DownloadInitialTiles();
		}

		//TODO TEMP: Keep reviewing later, probably this method will replace all the other zoom control methods
		public void ApplyPinchZoom(float pZoomScale)
		{
			int intVal = (int)pZoomScale;
			float fraction = pZoomScale - intVal;

			/*if (fraction > 0.5f)
			{
				intVal++;
			}*/

			Debug.Log("New CurrentZoomLevel is " + intVal);

			zoomMultiplier = pZoomScale;
			_world.transform.localScale = new Vector3(zoomMultiplier, zoomMultiplier, 1);

			CalculateScreenBoundaries();
		}

		public void ExecuteZooming(int pZoomLevel = 0)
		{
			_currentZoomLevel += pZoomLevel;

			if (ValidateZoomLimits() == false)
			{
				return;
			}

			if (pZoomLevel > 0)
			{
				DoZoomIn();				
			}
			else if (pZoomLevel < 0)
			{
				DoZoomOut();
			}
		}

		private void DoZoomIn()
		{
			OtherLayer.FadeOut(0);

			StartReferencingLayers();
			ResetLayerContainerPosition();
			ResetOtherLayerPosition();
			MoveCurrentLayerToContainer();

			UpdateMarkersOnZoom(2);

			TweenManager.Instance.ScaleTo(_layerContainer.gameObject, _layerContainer.transform.localScale * 2, _scalingDurationForLayers, TweenType.ExponentialOut, true, null, () =>
			{
				CurrentLayer.FadeOut(_fadingDurationForLayers, () =>
				{
					MoveOtherLayerToMap();
					ResetOtherLayerScale();
				});

				ResetMapPosition();
				ResetOtherLayerPosition();
				OtherLayer.OrganizeTilesAsGrid();

				SwapLayers();

				ReferenceTilesBetweenLayersOnZoomIn();

				transform.position = _mapDeviationCorrection;

				CalculateScreenBoundaries();

				ReplicateOtherLayer();
			});
		}

#region TEMP
		public void ExecuteZoomingWithPinch(int pZoomLevel = 0, int pZoomScale = 1)
		{
			_currentZoomLevel += pZoomLevel;

			if (ValidateZoomLimits() == false)
			{
				return;
			}

			if (pZoomLevel > 0)
			{
				DoZoomIn2(pZoomScale);
			}
			else if (pZoomLevel < 0)
			{
				DoZoomOut();
			}
		}

		private void DoZoomIn2(int pZoomScale = 1)
		{			
			OtherLayer.FadeOut(0);

			StartReferencingLayers();
			ResetLayerContainerPosition();
			ResetOtherLayerPosition();
			MoveCurrentLayerToContainer();

			ResetMapPosition();
			OtherLayer.OrganizeTilesAsGrid();

			SwapLayers();
						
			ReferenceTilesBetweenLayersOnZoomInByPinch(pZoomScale);

			transform.position = _mapDeviationCorrection;

			CalculateScreenBoundaries();
					
			PrepareZoomInTransition(()=> 
			{
				MoveOtherLayerToMap();
				ReplicateOtherLayer();
				ResetOtherLayerScale();
				CheckCurrentLayerWithinScreenLimits(false);
			});						
		}
#endregion

		private void DoZoomOut()
		{
			OtherLayer.FadeOut(0);

			StartReferencingLayers();
			ResetLayerContainerPosition();
			ResetOtherLayerPosition();
			MoveCurrentLayerToContainer();

			UpdateMarkersOnZoom(0.5f);

			TweenManager.Instance.ScaleTo(_layerContainer.gameObject, _layerContainer.transform.localScale / 2, _scalingDurationForLayers, TweenType.ExponentialOut, true, null, () => 
			{
				CurrentLayer.FadeOut(_fadingDurationForLayers, () =>
				{
					MoveOtherLayerToMap();
					ResetOtherLayerScale();
				});

				ResetMapPosition();
				ResetOtherLayerPosition();
				OtherLayer.OrganizeTilesAsGrid();

				SwapLayers();

				ReferenceTilesBetweenLayersOnZoomOut();

				transform.position = _mapDeviationCorrection;

				CalculateScreenBoundaries();

				ReplicateOtherLayer();
			});
		}

		private void PrepareZoomInTransition(Action pOnComplete)
		{
			_onZoomTransition = true;
			_zoomTransitionTimer = 1;
			_onCompleteZoomInTransition = pOnComplete;
		}

		private void UpdateZoomLevel()
		{
			_currentZoomLevel = NextZoomLevel;
		}

		public void SetMovingLeft()
		{
			_isMovingLeft = true;
			_isMovingRight = false;
			_isStopped = false;
		}

		public void SetMovingRight()
		{
			_isMovingRight = true;
			_isMovingLeft = false;
			_isStopped = false;
		}

		public void SetMovingUp()
		{
			_isMovingUp = true;
			_isMovingDown = false;
			_isStopped = false;
		}

		public void SetMovingDown()
		{
			_isMovingDown = true;
			_isMovingUp = false;
			_isStopped = false;
		}

		public void StopMovements()
		{
			_isMovingLeft = false;
			_isMovingRight = false;
			_isMovingDown = false;
			_isMovingUp = false;
			_isStopped = true;
		}

		private void DefineCenterTileOnCurrentLayer()
		{
			CurrentLayer.DefineCenterTile(_currentZoomLevel, _currentLatitude, _currentLongitude);
			_centerTileData = CurrentLayer.CenterTile.TileData;
		}

		private void PrepareTileDataBeforeDownload(Tile tile)
		{
			//Position of the tile in the world space - the current position of the map in the space, multiplied by the size in units to have proportionally the size of the tile taken in consideration
			_distX = Mathf.RoundToInt((tile.transform.position.x - transform.position.x) / (TILE_SIZE_IN_UNITS * zoomMultiplier));
			_distY = Mathf.RoundToInt((tile.transform.position.y - transform.position.y) / (TILE_SIZE_IN_UNITS * zoomMultiplier));
						
			_nextX = _centerTileData.x + _distX;
			_nextY = _centerTileData.y - _distY;

			if (_nextX > _tileCycleLimit)
			{
				_correctionX = _nextX / (_tileCycleLimit + 1);
				_nextX = _nextX - (_tileCycleLimit + 1) * _correctionX;
			}
			else if (_nextX < 0)
			{
				_nextX = (_nextX * -1) - 1;//Inversion
				_correctionX = _nextX / (_tileCycleLimit + 1);//Correction
				_nextX = _nextX - (_tileCycleLimit + 1) * _correctionX;//Formula
				_nextX = _tileCycleLimit - _nextX;
			}

			tile.TileData = new TileData(tile.TileData.index, _currentZoomLevel, _nextX, _nextY);

			DoTileDownload(tile.TileData);
		}

		private void DoTileDownload(TileData pTileData)
		{
			if (pTileData.x < 0 || pTileData.y < 0)
			{
				CurrentLayer.SetTexture(pTileData.index, null);
			}
			else
			{
				TileDownloadManager.Instance.DownloadTileImage(pTileData.name, (Texture2D texture) =>
				{
					CurrentLayer.SetTexture(pTileData.index, texture);
				});
			}
		}

		public bool CheckTileOnScreen(Vector3 tilePosition, float size = TILE_HALF_SIZE_IN_UNITS)
		{
			size *= zoomMultiplier;

			return tilePosition.x + size >= ScreenBoundaries.left &&
				   tilePosition.x - size <= ScreenBoundaries.right &&
				   tilePosition.y + size >= ScreenBoundaries.bottom &&
				   tilePosition.y - size <= ScreenBoundaries.top;
		}

		private void DownloadInitialTiles()
		{
			if (CurrentLayer != null)
			{
				int x = 0;
				int y = 0;

				foreach (Tile tile in CurrentLayer.Tiles)
				{
					x = _centerTileData.x + (int)(tile.transform.position.x / (TILE_SIZE_IN_UNITS * zoomMultiplier));//tile x position for tile size scale
					y = _centerTileData.y + (int)(tile.transform.position.y / (TILE_SIZE_IN_UNITS * zoomMultiplier)) * -1;//tile y position for tile size scale

					tile.TileData = new TileData(tile.TileData.index, _currentZoomLevel, x, y);

					DoTileDownload(tile.TileData);
				}
			}
		}

		private void SwapLayers()
		{
			if (CurrentLayer != null && OtherLayer != null)
			{
				_auxLayer = CurrentLayer;
				CurrentLayer = OtherLayer;
				CurrentLayer.gameObject.name = CURRENT_LAYER;

				OtherLayer = _auxLayer;
				OtherLayer.gameObject.name = OTHER_LAYER;

				CurrentLayer.ChangeToFrontLayer();
				OtherLayer.ChangeToBackLayer();

				_auxLayer = null;
			}
		}

		//TODO: Review later and remove the other one once this one is working properly
		private void ReferenceTilesBetweenLayersOnZoomInByPinch(int pZoomScale)
		{
			Tile otherLayerMainTile = GetCenterTileOnOtherLayerByPinch();
			Tile currentLayerMainTile = GetCenterTileOnCurrentLayerByPinch();

			if (otherLayerMainTile == null || currentLayerMainTile == null)
			{
				return;
			}

			//By this time other is already the one in focus, because the layers were already swaped, meaning that this was the current before that
			Vector3 topLeftOtherTile = otherLayerMainTile.transform.position;
			topLeftOtherTile.x -= TILE_HALF_SIZE_IN_UNITS * pZoomScale;
			topLeftOtherTile.y += TILE_HALF_SIZE_IN_UNITS * pZoomScale;
						
			//This is the one that should be referenced by the other above
			Vector3 topLeftCurrentTile = currentLayerMainTile.transform.position;
			topLeftCurrentTile.x -= TILE_HALF_SIZE_IN_UNITS;
			topLeftCurrentTile.y += TILE_HALF_SIZE_IN_UNITS;

			_mapDeviationCorrection.x = topLeftOtherTile.x - topLeftCurrentTile.x + transform.position.x;
			_mapDeviationCorrection.y = topLeftOtherTile.y - topLeftCurrentTile.y + transform.position.y;

			currentLayerMainTile.TileData = new TileData(currentLayerMainTile.TileData.index, _currentZoomLevel, otherLayerMainTile.TileData.x * pZoomScale, otherLayerMainTile.TileData.y * pZoomScale);
			_centerTileData = currentLayerMainTile.TileData;

			int x = 0, y = 0;

			foreach (Tile tile in CurrentLayer.Tiles)
			{
				x = (int)((tile.transform.localPosition.x - currentLayerMainTile.transform.localPosition.x) / TILE_SIZE_IN_UNITS);
				y = (int)((tile.transform.localPosition.y - currentLayerMainTile.transform.localPosition.y) / TILE_SIZE_IN_UNITS) * -1;

				tile.TileData = new TileData(tile.TileData.index, _currentZoomLevel, _centerTileData.x + x, _centerTileData.y + y);

				if (CheckTileOnScreen(tile.transform.position))
				{
					DoTileDownload(tile.TileData);
				}				
			}
		}

		private void ReferenceTilesBetweenLayersOnZoomIn()
		{
			Tile otherLayerCenterTile = GetCenterTileOnOtherLayer(true);
			Tile currentLayerCenterTile = GetCenterTileOnCurrentLayer(true);

			if (otherLayerCenterTile == null || currentLayerCenterTile == null)
			{
				return;
			}

			Vector3 topLeftOtherTile = otherLayerCenterTile.transform.position;
			topLeftOtherTile.x -= TILE_SIZE_IN_UNITS;
			topLeftOtherTile.y += TILE_SIZE_IN_UNITS;

			Vector3 topLeftCurrentTile = currentLayerCenterTile.transform.position;
			topLeftCurrentTile.x -= TILE_HALF_SIZE_IN_UNITS;
			topLeftCurrentTile.y += TILE_HALF_SIZE_IN_UNITS;

			_mapDeviationCorrection.x = topLeftOtherTile.x - topLeftCurrentTile.x;
			_mapDeviationCorrection.y = topLeftOtherTile.y - topLeftCurrentTile.y;

			currentLayerCenterTile.TileData = new TileData(currentLayerCenterTile.TileData.index, _currentZoomLevel, otherLayerCenterTile.TileData.x * 2, otherLayerCenterTile.TileData.y * 2);
			_centerTileData = currentLayerCenterTile.TileData;

			int x = 0, y = 0;

			foreach (Tile tile in CurrentLayer.Tiles)
			{
				x = (int)((tile.transform.localPosition.x - currentLayerCenterTile.transform.localPosition.x) / TILE_SIZE_IN_UNITS);
				y = (int)((tile.transform.localPosition.y - currentLayerCenterTile.transform.localPosition.y) / TILE_SIZE_IN_UNITS) * -1;

				tile.TileData = new TileData(tile.TileData.index, _currentZoomLevel, _centerTileData.x + x, _centerTileData.y + y);

				if (CheckTileOnScreen(tile.transform.position))
				{
					DoTileDownload(tile.TileData);
				}
			}
		}

		private void ReferenceTilesBetweenLayersOnZoomOut()
		{
			Tile otherLayerCenterTile = GetCenterTileOnOtherLayer(false);
			Tile currentLayerCenterTile = GetCenterTileOnCurrentLayer(true);

			if (otherLayerCenterTile == null || currentLayerCenterTile == null)
			{
				return;
			}

			Vector3 topLeftOtherTile = otherLayerCenterTile.transform.position;
			topLeftOtherTile.x -= TILE_QUARTER_SIZE_IN_UNITS;
			topLeftOtherTile.y += TILE_QUARTER_SIZE_IN_UNITS;
			topLeftOtherTile *= -1;

			Vector3 topLeftCurrentTile = currentLayerCenterTile.transform.position;
			topLeftCurrentTile.x -= TILE_HALF_SIZE_IN_UNITS;
			topLeftCurrentTile.y += TILE_HALF_SIZE_IN_UNITS;
						
			_mapDeviationCorrection.x = transform.position.x - (topLeftOtherTile.x + topLeftCurrentTile.x);
			_mapDeviationCorrection.y = transform.position.y - (topLeftOtherTile.y + topLeftCurrentTile.y);

			currentLayerCenterTile.TileData = new TileData(currentLayerCenterTile.Index, _currentZoomLevel, otherLayerCenterTile.TileData.x / 2, otherLayerCenterTile.TileData.y / 2);
			_centerTileData = currentLayerCenterTile.TileData;

			int x = 0, y = 0;

			foreach(Tile tile in CurrentLayer.Tiles)
			{
				x = (int)((tile.transform.localPosition.x - currentLayerCenterTile.transform.localPosition.x) / TILE_SIZE_IN_UNITS);
				y = (int)((tile.transform.localPosition.y - currentLayerCenterTile.transform.localPosition.y) / TILE_SIZE_IN_UNITS) * -1;

				tile.TileData = new TileData(tile.TileData.index, _currentZoomLevel, _centerTileData.x + x, _centerTileData.y + y);

				if (CheckTileOnScreen(tile.transform.position))
				{
					DoTileDownload(tile.TileData);
				}
			}
		}

		public void StartReferencingLayers()
		{
			int total = OtherLayer.Tiles.Count;

			for (int i = 0; i < total; i++)
			{
				OtherLayer.Tiles[i].transform.position = CurrentLayer.Tiles[i].transform.position;
			}						
		}

		public void CheckCurrentLayerWithinScreenLimits(bool pConsiderMovement = true)
		{
			foreach (Tile tile in CurrentLayer.Tiles)
			{
				if ((_isMovingLeft || pConsiderMovement == false) && tile.transform.position.x + TILE_HALF_SIZE_IN_UNITS * zoomMultiplier < ScreenBoundaries.left)
				{
					_helperVector3.x = CurrentLayer.Config.width * TILE_SIZE_IN_UNITS;
					_helperVector3.y = 0;
					_helperVector3.z = 0;

					tile.transform.localPosition += _helperVector3;
					PrepareTileDataBeforeDownload(tile);
				}

				if ((_isMovingRight || pConsiderMovement == false) && tile.transform.position.x - TILE_HALF_SIZE_IN_UNITS * zoomMultiplier > ScreenBoundaries.right)
				{
					_helperVector3.x = CurrentLayer.Config.width * TILE_SIZE_IN_UNITS;
					_helperVector3.y = 0;
					_helperVector3.z = 0;

					tile.transform.localPosition -= _helperVector3;
					PrepareTileDataBeforeDownload(tile);					
				}

				if ((_isMovingUp || pConsiderMovement == false) && tile.transform.position.y - TILE_HALF_SIZE_IN_UNITS * zoomMultiplier > ScreenBoundaries.top)
				{
					_helperVector3.x = 0;
					_helperVector3.y = CurrentLayer.Config.height * TILE_SIZE_IN_UNITS;
					_helperVector3.z = 0;

					tile.transform.localPosition -= _helperVector3;
					PrepareTileDataBeforeDownload(tile);
				}

				if ((_isMovingDown || pConsiderMovement == false) && tile.transform.position.y + TILE_HALF_SIZE_IN_UNITS * zoomMultiplier < ScreenBoundaries.bottom)
				{
					_helperVector3.x = 0;
					_helperVector3.y = CurrentLayer.Config.height * TILE_SIZE_IN_UNITS;
					_helperVector3.z = 0;

					tile.transform.localPosition += _helperVector3;
					PrepareTileDataBeforeDownload(tile);
				}
			}

			if (_isStopped == false && pConsiderMovement == true)
			{
				UpdateMarkers();
			}
		}

		private Tile GetTileByVectorPointForMarker(Vector3 pPoint)
		{
			foreach (Tile tile in CurrentLayer.Tiles)
			{
				if (CheckTileOnScreen(tile.transform.position))
				{
					if (tile.transform.position.x + TILE_HALF_SIZE_IN_UNITS > pPoint.x && tile.transform.position.x - TILE_HALF_SIZE_IN_UNITS < pPoint.x &&
						tile.transform.position.y + TILE_HALF_SIZE_IN_UNITS > pPoint.y && tile.transform.position.y - TILE_HALF_SIZE_IN_UNITS < pPoint.y)
					{
						return tile;
					}
				}
			}

			return null;
		}

		private Tile GetTileByPoint3(int x, int y)
		{
			foreach (Tile tile in CurrentLayer.Tiles)
			{
				if (tile.TileData.x == x && tile.TileData.x == x &&
					tile.TileData.y == y && tile.TileData.y == y)
				{
					return tile;
				}
			}

			return null;
		}

		//TODO: Do no update everytime we move the map
		private void UpdateMarkers()
		{
			foreach(Marker marker in MarkerManager.Instance.Markers)
			{
				if (marker.Active)
				{
					Point3Double tileP = OSMGeoHelper.GeoToTilePosDouble(marker.Latitude, marker.Longitude, _currentZoomLevel);

					int z = tileP.zoomLevel;
					int x = (int) tileP.x;
					int y = (int) tileP.y;

					_insideTilePosition.x = (float) tileP.x - x;
					_insideTilePosition.y = (float) tileP.y - y;

					Tile tile = GetTileByPoint3(x, y);						

					if (tile != null)
					{
						if(marker.Visible == false)
						{
							marker.FadeIn();
						}

						_insideTilePosition.x = tile.transform.position.x - TILE_HALF_SIZE_IN_UNITS + TILE_SIZE_IN_UNITS * _insideTilePosition.x;
						_insideTilePosition.y = tile.transform.position.y + TILE_HALF_SIZE_IN_UNITS - TILE_SIZE_IN_UNITS * _insideTilePosition.y;

						marker.transform.position = MarkerManager.Instance.WorldToCanvasPosition(_insideTilePosition);
					}
					else if(marker.Visible == true && 
						(marker.transform.position.x + 0.1f < 0 || marker.transform.position.x - 0.1f > Screen.width ||
						 marker.transform.position.y + 0.1f < 0 || marker.transform.position.y - 0.1f > Screen.height))
					{
						marker.FadeOut();
					}
				}
			}
		}

		private void UpdateMarkersOnZoom(float pMultiplier)
		{
			foreach(Marker m in MarkerManager.Instance.Markers)
			{
				Vector3 worldPoint = MarkerManager.Instance.CanvasToWorldPosition(m.Image.rectTransform, m.transform.position) * pMultiplier;
				TweenManager.Instance.Move(m.gameObject, MarkerManager.Instance.WorldToCanvasPosition(worldPoint), _scalingMoveSpeedForMarkers);
			}
		}

		public void ResetLayerContainerPosition()
		{
			if (_layerContainer != null)
			{
				_layerContainer.transform.localScale = Vector3.one;
			}
		}

		public void ResetOtherLayerPosition()
		{
			if(OtherLayer != null)
			{
				OtherLayer.transform.position = Vector3.zero;
			}			
		}

		public void ResetOtherLayerScale()
		{
			if (OtherLayer != null)
			{
				OtherLayer.transform.localScale = Vector3.one;
			}
		}

		public void ResetMapPosition()
		{
			transform.position = Vector3.zero;
		}

		public void MoveCurrentLayerToContainer()
		{
			if(CurrentLayer != null && _layerContainer != null)
			{
				CurrentLayer.transform.SetParent(_layerContainer.transform);
			}			
		}

		public void MoveCurrentLayerToMap()
		{
			if (CurrentLayer != null)
			{
				CurrentLayer.transform.SetParent(transform);
			}
		}

		private void MoveOtherLayerToContainer()
		{
			if (OtherLayer != null && _layerContainer != null)
			{
				OtherLayer.transform.SetParent(_layerContainer.transform);
			}
		}

		private void MoveOtherLayerToMap()
		{
			if (OtherLayer != null)
			{
				OtherLayer.transform.SetParent(transform);
			}
		}

		private void ReplicateOtherLayer()
		{
			if (LayerReplica != null)
			{
				Destroy(LayerReplica.gameObject);
			}

			LayerReplica = Instantiate(OtherLayer.gameObject, transform).GetComponent<Layer>();
			LayerReplica.ChangeToMiddleLayer();

			CurrentLayer.ChangeToFrontLayer();
			OtherLayer.ChangeToBackLayer();
		}

		private void ReplicateCurrentLayer()
		{
			if (LayerReplica != null)
			{
				Destroy(LayerReplica.gameObject);
			}

			LayerReplica = Instantiate(CurrentLayer.gameObject, transform).GetComponent<Layer>();

			CurrentLayer.ChangeToFrontLayer();
			LayerReplica.ChangeToMiddleLayer();
			OtherLayer.ChangeToBackLayer();
		}
	}
}