using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OSM
{
	public class Map : MonoBehaviour
	{
		[Header("Map Zooming")]
		[SerializeField]
		[Range(Consts.MIN_ZOOM_LEVEL, Consts.MAX_ZOOM_LEVEL)]
		private int _currentZoomLevel = Consts.MIN_ZOOM_LEVEL;
		[SerializeField]
		private int _previousZoomLevel;
		[SerializeField]
		private float _currentMapScale = 1;

		[Header("Layer Properties")]
		[SerializeField]
		private List<LayerConfig> layerConfigs;

		[Header("General Properties")]
		[SerializeField]
		private double _initialLatitude = 49.2674573;
		[SerializeField]
		private double _initialLongitude = -123.0930032;

		public float _mapMinYByZoomLevel;
		public float _mapMaxYByZoomLevel;

		[Header("Dependencies")]
		public Camera mainCamera;
		[SerializeField]
		private GameObject _layerContainer;
		public Transform LayerContainer
		{
			get
			{
				return _layerContainer.transform;
			}
		}
		
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
		private float _fadingDurationForLayers = 1f;
		private float _scalingDurationForLayers = 0.5f;
		
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

		public int calculatedNextZoomLevel;

		[SerializeField]
		private TileData _referenceTileData;
		private Vector3 _helperVector3;
		
		private bool _onZoomTransition;
		private float _zoomTransitionTimer;
		private Action _onCompleteZoomInTransition;

		//Vars
		private int _distX = 0;
		private int _distY = 0;
		private int _nextX = 0;
		private int _nextY = 0;
		private int _correctionX = 0;

		[SerializeField]
		private float _zoomMultiplicationFactor = 1;

		private Vector3 _mapDeviationCorrection;

		public Vector3 ProgressiveLayerScale { get; set; }

		public bool tempDoFullZoomCycle;
		public bool dontResetOtherLayer;

		private Tile _otherLayerMainTile;
		private Tile _currentLayerMainTile;

		[SerializeField]
		private bool _debugUseTileColorGuide;

		private void Start()
		{
			ApplyPinchZoom(_zoomMultiplicationFactor);

			if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
			{
				Application.targetFrameRate = Consts.TARGET_FRAME_RATE;
			}

			ForceStopMovements();
			InitializeMap();
			InitializeLayers();
			InitialZoom();

			CalculateScreenBoundaries();
			//CheckCurrentLayerWithinScreenLimits(false);
			CalculateTilesPositionAndUpdate();

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

			MarkerManager.Instance.ProcessMarkerSpawningInput();
			MarkerManager.Instance.CheckMarkersPlacement();
		}

		private void RunZoomInTransactionTimeLogic()
		{
			if (_onZoomTransition == true && _zoomTransitionTimer > 0)
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

		private void InitializeMap()
		{
			//Tile size base should be multiplied because it will be used to organize the tiles side by side and it considers the tile bounds to do so
			TileSize = Consts.TILE_SIZE_IN_UNITS * _zoomMultiplicationFactor;
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
					CurrentLayer.gameObject.name = Consts.CURRENT_LAYER;
				}
				else if (OtherLayer == null)
				{
					OtherLayer = layer;
					OtherLayer.gameObject.name = Consts.OTHER_LAYER;
				}

				layerIndex++;
			}
		}

		private Tile GetCenterTileOnCurrentLayer(bool pUseExactCenter)
		{
			return GetCenterTileOnLayer(CurrentLayer, pUseExactCenter);
		}

		private Tile GetCenterTileOnOtherLayer(bool pUseExactCenter)
		{
			return GetCenterTileOnLayer(OtherLayer, pUseExactCenter);
		}	
		
		private Tile GetCenterTileOnLayer(Layer pLayer, bool pUseExactCenter)
		{
			Tile centerTile = null;

			foreach (Tile tile in pLayer.Tiles)
			{
				if (CheckTileOnScreen(tile.transform.position))
				{
					if (pUseExactCenter == true)
					{
						if (tile.transform.position.x - Consts.TILE_SIZE_IN_UNITS <= 0 && tile.transform.position.x + Consts.TILE_SIZE_IN_UNITS >= 0 &&
							tile.transform.position.y + Consts.TILE_SIZE_IN_UNITS >= 0 && tile.transform.position.y - Consts.TILE_SIZE_IN_UNITS <= 0)
						{
							return tile;
						}
					}
					else
					{
						if (tile.X % 2 == 0 && tile.Y % 2 == 0 &&
							tile.transform.position.x - Consts.TILE_SIZE_IN_UNITS <= 0 && tile.transform.position.x + Consts.TILE_SIZE_IN_UNITS >= 0 &&
							tile.transform.position.y + Consts.TILE_SIZE_IN_UNITS >= 0 && tile.transform.position.y - Consts.TILE_SIZE_IN_UNITS <= 0)
						{
							return tile;
						}
					}
				}
			}

			return centerTile;
		}

		private Tile GetCenterTileOnCurrentLayerByPinch()
		{
			return GetCenterTileOnLayerByPinch(CurrentLayer);
		}

		private Tile GetCenterTileOnOtherLayerByPinch()
		{
			return GetCenterTileOnLayerByPinch(OtherLayer);
		}

		private Tile GetCenterTileOnLayerByPinch(Layer pLayer)
		{
			float distance = -1;
			Tile centerTile = null;

			foreach (Tile tile in pLayer.Tiles)
			{
				if (distance == -1)
				{
					distance = Vector3.Distance(tile.transform.position, Vector3.zero);
					centerTile = tile;
				}
				if (Vector3.Distance(tile.transform.position, Vector3.zero) < distance)
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

			_tileCycleLimit = (int) Mathf.Pow(2, _currentZoomLevel) - 1;

			_mapMinYByZoomLevel = (_referenceTileData.y * Consts.TILE_SIZE_IN_UNITS + Consts.TILE_HALF_SIZE_IN_UNITS) * CurrentLayer.MapScaleFraction;
			_mapMaxYByZoomLevel = ((_tileCycleLimit - _referenceTileData.y) * Consts.TILE_SIZE_IN_UNITS + Consts.TILE_HALF_SIZE_IN_UNITS) * CurrentLayer.MapScaleFraction;

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
				//if (tile.TextureUpToDate == false)
				{
					DoTileDownload(tile.TileData);
				}
			}
		}

		protected bool ValidateZoomLimits()
		{
			if (_currentZoomLevel < Consts.MIN_ZOOM_LEVEL)
			{
				_currentZoomLevel = Consts.MIN_ZOOM_LEVEL;

				return false;
			}

			if (_currentZoomLevel > Consts.MAX_ZOOM_LEVEL)
			{
				_currentZoomLevel = Consts.MAX_ZOOM_LEVEL;

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
			if (_currentZoomLevel < Consts.MIN_ZOOM_LEVEL)
			{
				_currentZoomLevel = Consts.MIN_ZOOM_LEVEL;
			}

			if (_currentZoomLevel > Consts.MAX_ZOOM_LEVEL)
			{
				_currentZoomLevel = Consts.MAX_ZOOM_LEVEL;
			}

			NextZoomLevel = _currentZoomLevel;

			DefineCenterTileOnCurrentLayer();
			//DownloadInitialTiles();
		}

		//TODO TEMP: Keep reviewing later, probably this method will replace all the other zoom control methods
		public void ApplyPinchZoom(float pZoomScale)
		{
			calculatedNextZoomLevel = (int) pZoomScale;
			float fraction = pZoomScale - calculatedNextZoomLevel;

			calculatedNextZoomLevel += 2;

			if (fraction >= 0.5f)
			{
				calculatedNextZoomLevel++;
			}

			_zoomMultiplicationFactor = pZoomScale;

			//CalculateScreenBoundaries();
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
				DoZoom(Consts.ONE_LEVEL_ZOOM_IN, ReferenceTilesBetweenLayersOnZoomIn);
			}
			else if (pZoomLevel < 0)
			{
				DoZoom(Consts.ONE_LEVEL_ZOOM_OUT, ReferenceTilesBetweenLayersOnZoomOut);
			}
		}

		private void DoZoom(float pZoomMultiplier, Action<float> pReferenceTilesAction)
		{
			OtherLayer.FadeOut(0);

			//ResetOtherLayerPosition();
			MoveCurrentLayerToContainer();

			MarkerManager.Instance.UpdateMarkersOnZoom(pZoomMultiplier);

			TweenManager.Instance.ScaleTo(_layerContainer.gameObject, _layerContainer.transform.localScale * pZoomMultiplier, _scalingDurationForLayers, TweenType.ExponentialOut, true, null, () =>
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

				pReferenceTilesAction?.Invoke(1);
								
				transform.position = _mapDeviationCorrection;

				CalculateScreenBoundaries();
			});
		}

		public void ExecuteZoomingWithPinch(ZoomLevel pZoomLevel)
		{
			float pScaleFraction = LayerContainer.localScale.x;

			_previousZoomLevel = _currentZoomLevel;
			_currentZoomLevel += pZoomLevel.sum;

			if (ValidateZoomLimits() == false)
			{
				return;
			}

			if (pZoomLevel.sum > 0 || (pZoomLevel.sum == 0 && pScaleFraction > 1))
			{
				DoZoomWithPinch(pZoomLevel.sum, pZoomLevel.scale, pScaleFraction);
			}
			else if (pZoomLevel.sum < 0)
			{
				DoZoom(Consts.ONE_LEVEL_ZOOM_OUT, ReferenceTilesBetweenLayersOnZoomOut);
			}
		}

		private void DoZoomWithPinch(int pZoomLevel = 3, int pZoomScale = 1, float pObjectScale = 1)
		{
			ZoomFraction zoomFraction = ZoomSystem.GetZoomScaleByObjectScale(CurrentLayer.transform.localScale.x);

			Debug.Log("<color=#0F0>newZoomLevel: " + zoomFraction.zoomLevel + " scale: " + zoomFraction.zoomScale + " objectScale: " + zoomFraction.objectScale + " fraction: " + zoomFraction.scaleFraction + " finalScale: " + zoomFraction.zoomScale + "</color>");
			Debug.Log("<color=#D90>sum: " + pZoomLevel + " scale: " + pZoomScale + " fraction: " + pObjectScale + " finalScale: " + zoomFraction.zoomScale + "</color>");

			_currentMapScale = zoomFraction.zoomScale;

			if (zoomFraction.zoomLevel > _currentZoomLevel)
			{
				_currentZoomLevel = zoomFraction.zoomLevel;
			}

			OtherLayer.FadeOut();

			ResetOtherLayerScale();
			ResetLayerContainerScale();
			ResetOtherLayerPosition();
			ResetLayerContainerPosition();

			MoveCurrentLayerToContainer();

			ResetMapPosition();			

			OtherLayer.OrganizeTilesAsGrid();

			if (tempDoFullZoomCycle == true)//Delete tempDoFullZoomCycle later
			{
				SwapLayers();

				CurrentLayer.RecalculateMapScaling( OtherLayer.transform.localScale.x / zoomFraction.zoomScale );
				OtherLayer.MapScaleFraction = 1;

				ReferenceTilesBetweenLayersOnZoomInByPinch(zoomFraction, () => 
				{
					transform.position = _mapDeviationCorrection;

					CalculateScreenBoundaries();

					PrepareZoomInTransition(() =>
					{
						MoveOtherLayerToMap();
						//ResetOtherLayerScale();
						//CheckCurrentLayerWithinScreenLimits(false);
						CalculateTilesPositionAndUpdate();
					});
				});
			}
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

		public void ForceStopMovements()
		{
			_isMovingLeft = false;
			_isMovingRight = false;
			_isMovingDown = false;
			_isMovingUp = false;
			_isStopped = true;
		}

		private void DefineCenterTileOnCurrentLayer()
		{
			CurrentLayer.DefineCenterTile(_currentZoomLevel, _initialLatitude, _initialLongitude);
			_referenceTileData = CurrentLayer.CenterTile.TileData;
		}

		private void PrepareTileDataBeforeDownload(Tile tile)
		{
			//Position of the tile in the world space - the current position of the map in the space, multiplied by the size in units to have proportionally the size of the tile taken in consideration
			_distX = Mathf.RoundToInt((tile.transform.position.x - transform.position.x) / (Consts.TILE_SIZE_IN_UNITS * CurrentLayer.MapScaleFraction));
			_distY = Mathf.RoundToInt((tile.transform.position.y - transform.position.y) / (Consts.TILE_SIZE_IN_UNITS * CurrentLayer.MapScaleFraction));
						
			_nextX = _referenceTileData.x + _distX;
			_nextY = _referenceTileData.y - _distY;

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

			if (tile.TextureUpToDate == false)
			{
				DoTileDownload(tile.TileData);
			}
		}

		private void DoTileDownload(TileData pTileData, float delay = 0)
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
				}, delay);
			}
		}

		private void DoTileDownloadForLayer(Layer layer, TileData pTileData, float delay = 0)
		{
			if (pTileData.x < 0 || pTileData.y < 0)
			{
				layer.SetTexture(pTileData.index, null);
			}
			else
			{
				TileDownloadManager.Instance.DownloadTileImage(pTileData.name, (Texture2D texture) =>
				{
					layer.SetTexture(pTileData.index, texture);
				}, delay);
			}
		}

		public bool CheckTileOnScreen(Vector3 tilePosition, float size = Consts.TILE_HALF_SIZE_IN_UNITS)
		{
			size *= CurrentLayer.MapScaleFraction;

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
					x = _referenceTileData.x + (int)(tile.transform.position.x / (Consts.TILE_SIZE_IN_UNITS * CurrentLayer.MapScaleFraction));//tile x position for tile size scale
					y = _referenceTileData.y + (int)(tile.transform.position.y / (Consts.TILE_SIZE_IN_UNITS * CurrentLayer.MapScaleFraction)) * -1;//tile y position for tile size scale

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
				CurrentLayer.gameObject.name = Consts.CURRENT_LAYER;

				OtherLayer = _auxLayer;
				OtherLayer.gameObject.name = Consts.OTHER_LAYER;

				CurrentLayer.ChangeToFrontLayer();
				OtherLayer.ChangeToBackLayer();

				_auxLayer = null;
			}
		}

		//TODO: Review later and remove the other one once this one is working properly
		private void ReferenceTilesBetweenLayersOnZoomInByPinch(ZoomFraction pZoomFraction, Action pOnCompelte = null)
		{
			if (_debugUseTileColorGuide)
			{
				if (_otherLayerMainTile != null)
					_otherLayerMainTile._meshRenderer.material.color = Color.white;
				
				if (_currentLayerMainTile != null)
					_currentLayerMainTile._meshRenderer.material.color = Color.white;
			}

			_otherLayerMainTile = GetCenterTileOnOtherLayerByPinch();
			_currentLayerMainTile = GetCenterTileOnCurrentLayerByPinch();
						
			//By this time other is already the one in focus, because the layers were already swaped, meaning that this was the current before that
			Vector3 topLeftOtherTile = _otherLayerMainTile.transform.position;
			topLeftOtherTile.x -= Consts.TILE_HALF_SIZE_IN_UNITS * OtherLayer.transform.localScale.x;
			topLeftOtherTile.y += Consts.TILE_HALF_SIZE_IN_UNITS * OtherLayer.transform.localScale.x;

			//This is the one that should be referenced by the other above
			Vector3 topLeftCurrentTile = _currentLayerMainTile.transform.position;
			topLeftCurrentTile.x -= Consts.TILE_HALF_SIZE_IN_UNITS * CurrentLayer.MapScaleFraction;
			topLeftCurrentTile.y += Consts.TILE_HALF_SIZE_IN_UNITS * CurrentLayer.MapScaleFraction;

			_mapDeviationCorrection.x = topLeftOtherTile.x - topLeftCurrentTile.x + transform.position.x;
			_mapDeviationCorrection.y = topLeftOtherTile.y - topLeftCurrentTile.y + transform.position.y;

			int zoomInterval = (int) pZoomFraction.zoomScale;

			_referenceTileData = new TileData(_currentLayerMainTile.TileData.index, _currentZoomLevel, _otherLayerMainTile.TileData.x * zoomInterval, _otherLayerMainTile.TileData.y * zoomInterval);
			_currentLayerMainTile.TileData = _referenceTileData;
						
			int x = 0, y = 0;

			foreach (Tile tile in CurrentLayer.Tiles)
			{
				if (!tile.Equals(_referenceTileData))
				{
					x = (int)((tile.transform.localPosition.x - _currentLayerMainTile.transform.localPosition.x) / Consts.TILE_SIZE_IN_UNITS);
					y = (int)((tile.transform.localPosition.y - _currentLayerMainTile.transform.localPosition.y) / Consts.TILE_SIZE_IN_UNITS) * -1;

					tile.TileData = new TileData(tile.TileData.index, _currentZoomLevel, _referenceTileData.x + x, _referenceTileData.y + y);

					if (CheckTileOnScreen(tile.transform.position))
					{
						DoTileDownload(tile.TileData);
					}
				}
			}

			if (_debugUseTileColorGuide)
			{
				_otherLayerMainTile._meshRenderer.material.color = new Color(0.9f, 0.9f, 0.9f, 1);
				_currentLayerMainTile._meshRenderer.material.color = new Color(0.89f, 0.69f, 0.62f, 1);//227,177,157
			}

			pOnCompelte?.Invoke();
		}

		private void ReferenceTilesBetweenLayersOnZoomIn(float pScale)
		{
			Tile otherLayerCenterTile = GetCenterTileOnOtherLayer(true);
			Tile currentLayerCenterTile = GetCenterTileOnCurrentLayer(true);

			if (otherLayerCenterTile == null || currentLayerCenterTile == null)
			{
				return;
			}

			CurrentLayer.transform.localScale = new Vector3(pScale, pScale, 1);

			Vector3 topLeftOtherTile = otherLayerCenterTile.transform.position;
			topLeftOtherTile.x -= Consts.TILE_SIZE_IN_UNITS * pScale;
			topLeftOtherTile.y += Consts.TILE_SIZE_IN_UNITS * pScale;

			Vector3 topLeftCurrentTile = currentLayerCenterTile.transform.position;
			topLeftCurrentTile.x -= Consts.TILE_HALF_SIZE_IN_UNITS * pScale;
			topLeftCurrentTile.y += Consts.TILE_HALF_SIZE_IN_UNITS * pScale;

			_mapDeviationCorrection.x = topLeftOtherTile.x - topLeftCurrentTile.x + transform.position.x;
			_mapDeviationCorrection.y = topLeftOtherTile.y - topLeftCurrentTile.y + transform.position.y;

			//Multiplied by 2 because the tile proportion is always exactly (x * 2, y * 2) - Defining the center tile x and y 
			currentLayerCenterTile.TileData = new TileData(currentLayerCenterTile.TileData.index, _currentZoomLevel, otherLayerCenterTile.TileData.x * 2, otherLayerCenterTile.TileData.y * 2);
			_referenceTileData = currentLayerCenterTile.TileData;

			int x = 0, y = 0;

			foreach (Tile tile in CurrentLayer.Tiles)
			{
				x = (int)((tile.transform.localPosition.x - currentLayerCenterTile.transform.localPosition.x) / Consts.TILE_SIZE_IN_UNITS);
				y = (int)((tile.transform.localPosition.y - currentLayerCenterTile.transform.localPosition.y) / Consts.TILE_SIZE_IN_UNITS) * -1;

				tile.TileData = new TileData(tile.TileData.index, _currentZoomLevel, _referenceTileData.x + x, _referenceTileData.y + y);

				if (CheckTileOnScreen(tile.transform.position))
				{
					DoTileDownload(tile.TileData);
				}
			}
		}

		private void ReferenceTilesBetweenLayersOnZoomOut(float pScale)
		{
			Tile otherLayerCenterTile = GetCenterTileOnOtherLayer(false);
			Tile currentLayerCenterTile = GetCenterTileOnCurrentLayer(true);

			if (otherLayerCenterTile == null || currentLayerCenterTile == null)
			{
				return;
			}

			Vector3 topLeftOtherTile = otherLayerCenterTile.transform.position;
			topLeftOtherTile.x -= Consts.TILE_QUARTER_SIZE_IN_UNITS;
			topLeftOtherTile.y += Consts.TILE_QUARTER_SIZE_IN_UNITS;
			topLeftOtherTile *= -1;

			Vector3 topLeftCurrentTile = currentLayerCenterTile.transform.position;
			topLeftCurrentTile.x -= Consts.TILE_HALF_SIZE_IN_UNITS;
			topLeftCurrentTile.y += Consts.TILE_HALF_SIZE_IN_UNITS;
						
			_mapDeviationCorrection.x = transform.position.x - (topLeftOtherTile.x + topLeftCurrentTile.x);
			_mapDeviationCorrection.y = transform.position.y - (topLeftOtherTile.y + topLeftCurrentTile.y);

			currentLayerCenterTile.TileData = new TileData(currentLayerCenterTile.Index, _currentZoomLevel, otherLayerCenterTile.TileData.x / 2, otherLayerCenterTile.TileData.y / 2);
			_referenceTileData = currentLayerCenterTile.TileData;

			int x = 0, y = 0;

			foreach(Tile tile in CurrentLayer.Tiles)
			{
				x = (int)((tile.transform.localPosition.x - currentLayerCenterTile.transform.localPosition.x) / Consts.TILE_SIZE_IN_UNITS);
				y = (int)((tile.transform.localPosition.y - currentLayerCenterTile.transform.localPosition.y) / Consts.TILE_SIZE_IN_UNITS) * -1;

				tile.TileData = new TileData(tile.TileData.index, _currentZoomLevel, _referenceTileData.x + x, _referenceTileData.y + y);

				if (CheckTileOnScreen(tile.transform.position))
				{
					DoTileDownload(tile.TileData);
				}
			}
		}

		public void CheckCurrentLayerWithinScreenLimits(bool pConsiderMovement = true)
		{
			foreach (Tile tile in CurrentLayer.Tiles)
			{
				if ((_isMovingLeft || pConsiderMovement == false) && tile.transform.position.x + Consts.TILE_HALF_SIZE_IN_UNITS * CurrentLayer.MapScaleFraction < ScreenBoundaries.left)
				{
					_helperVector3.x = CurrentLayer.Config.width * Consts.TILE_SIZE_IN_UNITS;
					_helperVector3.y = 0;
					_helperVector3.z = 0;

					tile.transform.localPosition += _helperVector3;
					PrepareTileDataBeforeDownload(tile);
				}

				if ((_isMovingRight || pConsiderMovement == false) && tile.transform.position.x - Consts.TILE_HALF_SIZE_IN_UNITS * CurrentLayer.MapScaleFraction > ScreenBoundaries.right)
				{
					_helperVector3.x = CurrentLayer.Config.width * Consts.TILE_SIZE_IN_UNITS;
					_helperVector3.y = 0;
					_helperVector3.z = 0;

					tile.transform.localPosition -= _helperVector3;
					PrepareTileDataBeforeDownload(tile);					
				}

				if ((_isMovingUp || pConsiderMovement == false) && tile.transform.position.y - Consts.TILE_HALF_SIZE_IN_UNITS * CurrentLayer.MapScaleFraction > ScreenBoundaries.top)
				{
					_helperVector3.x = 0;
					_helperVector3.y = CurrentLayer.Config.height * Consts.TILE_SIZE_IN_UNITS;
					_helperVector3.z = 0;

					tile.transform.localPosition -= _helperVector3;
					PrepareTileDataBeforeDownload(tile);
				}

				if ((_isMovingDown || pConsiderMovement == false) && tile.transform.position.y + Consts.TILE_HALF_SIZE_IN_UNITS * CurrentLayer.MapScaleFraction < ScreenBoundaries.bottom)
				{
					_helperVector3.x = 0;
					_helperVector3.y = CurrentLayer.Config.height * Consts.TILE_SIZE_IN_UNITS;
					_helperVector3.z = 0;

					tile.transform.localPosition += _helperVector3;
					PrepareTileDataBeforeDownload(tile);
				}
			}

			if (_isStopped == false && pConsiderMovement == true)
			{
			 	MarkerManager.Instance.UpdateMarkers(_currentZoomLevel);
			}
		}

		public void CalculateTilesPositionAndUpdate()
		{
			foreach (Tile tile in CurrentLayer.Tiles)
			{
				PrepareTileDataBeforeDownload(tile);
			}
		}

		public Tile GetTileByVectorPoint(Vector3 pPoint)
		{
			foreach (Tile tile in CurrentLayer.Tiles)
			{
				if (CheckTileOnScreen(tile.transform.position))
				{
					if (tile.transform.position.x + Consts.TILE_HALF_SIZE_IN_UNITS > pPoint.x && tile.transform.position.x - Consts.TILE_HALF_SIZE_IN_UNITS < pPoint.x &&
						tile.transform.position.y + Consts.TILE_HALF_SIZE_IN_UNITS > pPoint.y && tile.transform.position.y - Consts.TILE_HALF_SIZE_IN_UNITS < pPoint.y)
					{
						return tile;
					}
				}
			}

			return null;
		}

		public Tile GetTileByPoint3(int x, int y)
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

		public void ResetLayerContainerScale()
		{
			if (_layerContainer != null)
			{
				_layerContainer.transform.localScale = Vector3.one;
			}
		}

		public void ResetLayerContainerPosition()
		{
			if (_layerContainer != null)
			{
				_layerContainer.transform.position = Vector3.zero;
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

		private void MoveReplicaLayerToContainer()
		{
			if (LayerReplica != null && _layerContainer != null)
			{
				LayerReplica.transform.SetParent(_layerContainer.transform);
			}
		}

		private void MoveReplicaLayerToMap()
		{
			if (LayerReplica != null)
			{
				LayerReplica.transform.SetParent(transform);
			}
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
			LayerReplica.ChangeToMiddleLayer();

			CurrentLayer.ChangeToFrontLayer();
			OtherLayer.ChangeToBackLayer();
		}
	}
}