using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OSM
{
	public class Map : MonoBehaviour
	{
		public const string CURRENT_LAYER = "CurrentLayer";
		public const string OTHER_LAYER = "OtherLayer";

		public const int MIN_ZOOM_LEVEL = 3;
		public const int MAX_ZOOM_LEVEL = 19;

		public const int TILE_SIZE_IN_PIXELS = 256;
		public const float TILE_SIZE_IN_UNITS = TILE_SIZE_IN_PIXELS * 0.01f;
		public const float TILE_HALF_SIZE_IN_UNITS = TILE_SIZE_IN_PIXELS * 0.005f;
		public const string LAYER_BASE_NAME = "Layer";

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

		[SerializeField]
		[Range(MIN_ZOOM_LEVEL, MAX_ZOOM_LEVEL)]
		private int _currentZoomLevel = 1;

		private float _previousZoomLevel = 1;

		[Header("Prefabs")]
		[SerializeField]
		private Tile _tileTemplate;
		[SerializeField]
		private Layer _layerTemplate;

		private List<Layer> _layers = new List<Layer>();

		public Layer CurrentLayer { get; private set; }
		public Layer OtherLayer { get; private set; }

		public int NextZoomLevel { get; set; }

		private Layer _auxLayer = null;

		private float _tileValidationSeconds = 1f;
		private float _tileValidationCounter = 0;

		public int _tileCycleLimit;

		public ScreenBoundaries _screenBoundaries;

		//Movement Detection
		public bool _isMovingLeft;
		public bool _isMovingRight;
		public bool _isMovingUp;
		public bool _isMovingDown;
		public bool _isStopped;

		public float TileSize { get; private set; }
		public float CurrentZoomLevel { get { return _currentZoomLevel; } }

		[SerializeField]
		private TileData _centerTileData;
		private Vector3 _displacementLevel;
		private Vector3 _helperVector3;
		private Vector3 _zoomTileDistanceFromMapCenter;

		[SerializeField]
		private GameObject _layerContainer;
		
		private bool _isScaling;

		private void Start()
		{
			StopMovements();
			InitializeMap();
			InitializeLayers();
			InitialZoom();

			CalculateScreenBoundaries();
			CheckInsideScreenInitially();

			DebugManager.Instance.CreateDebugFeatures();
		}

		private void Update()
		{
			if (_isScaling == false)
			{
				if (Input.GetKeyDown(KeyCode.S))
				{
					ZoomIn();
				}
				if (Input.GetKeyDown(KeyCode.Z))
				{
					ZoomOut();
				}
			}
		}

		private void LateUpdate()
		{
			if (_tileValidationCounter >= _tileValidationSeconds)
			{
				ValidateTiles();
				_tileValidationCounter = 0;
			}

			_tileValidationCounter += Time.deltaTime;						
		}

		private void UpdateTargetCoordinateBasedInTile()
		{
			Vector3 mp = Vector3.zero;//mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, mainCamera.transform.position.z * -1));

			foreach (Tile tile in CurrentLayer.Tiles)
			{
				if (CheckTileOnScreen(tile.transform.position))
				{
					if (tile.transform.position.x + TILE_HALF_SIZE_IN_UNITS >= mp.x && tile.transform.position.x - TILE_HALF_SIZE_IN_UNITS <= mp.x &&
						tile.transform.position.y + TILE_HALF_SIZE_IN_UNITS >= mp.y && tile.transform.position.y - TILE_HALF_SIZE_IN_UNITS <= mp.y)
					{
						Coordinates c = OSMGeoHelper.TileToWorldPos(tile.X, tile.Y, _currentZoomLevel);

						_currentLatitude = c.latitude;
						_currentLongitude = c.longitude;

						_zoomTileDistanceFromMapCenter = tile.transform.position;

						break;
					}
				}
			}
		}

		public void CalculateScreenBoundaries()
		{
			_screenBoundaries = new ScreenBoundaries(mainCamera);

			_tileCycleLimit = (int)Mathf.Pow(2, _currentZoomLevel) - 1;

			_mapMinYByZoomLevel = _centerTileData.y * TILE_SIZE_IN_UNITS + TILE_HALF_SIZE_IN_UNITS;
			_mapMaxYByZoomLevel = (_tileCycleLimit - _centerTileData.y) * TILE_SIZE_IN_UNITS + TILE_HALF_SIZE_IN_UNITS;
		}

		protected void ValidateTiles()
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

		protected void InitializeMap()
		{
			TileSize = _tileTemplate.TileSize.x;

			if (TileSize == 0)
			{
				TileSize = TILE_SIZE_IN_PIXELS;
			}

			_displacementLevel = new Vector3(TileSize * 0.5f, TileSize * 0.5f, 0);
		}

		protected void InitializeLayers()
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

				_layers.Add(layer);

				layerIndex++;
			}
		}

		public void ZoomIn()
		{
			ExecuteZooming(1);
		}

		public void ZoomOut()
		{
			ExecuteZooming(-1);
		}

		private void ForcePrepareTilesForDownload()
		{
			foreach (Tile tile in CurrentLayer.Tiles)
			{
				PrepareTileDataDownload(tile);
			}
		}

		public void PrepareZoomIn(float pZoomDuration, Action pOnComplete)
		{
			if (_currentZoomLevel < MAX_ZOOM_LEVEL)
			{
				NextZoomLevel++;

				_layerContainer.transform.SetParent(null);
				_layerContainer.transform.position = Vector3.zero;

				CurrentLayer.transform.SetParent(_layerContainer.transform);

				TweenManager.Instance.ScaleTo(_layerContainer.gameObject, _layerContainer.gameObject.transform.localScale * 2, pZoomDuration, TweenType.Linear, true, null, () =>
				{					
					_layerContainer.transform.SetParent(transform);

					CurrentLayer.transform.SetParent(transform);

					OtherLayer.transform.position = CurrentLayer.transform.position;

					_layerContainer.transform.localScale = Vector3.one;

					ExecuteZoomProcedures();

					pOnComplete?.Invoke();
				});
			}
			else
			{
				pOnComplete?.Invoke();
			}
		}

		public void PrepareZoomOut(float pZoomDuration, Action pOnComplete)
		{
			if (_currentZoomLevel > MIN_ZOOM_LEVEL)
			{
				NextZoomLevel--;

				TweenManager.Instance.ScaleTo(CurrentLayer.gameObject, CurrentLayer.gameObject.transform.localScale / 2, pZoomDuration, TweenType.Linear, true, null, () =>
				{
					ExecuteZoomProcedures();
					pOnComplete?.Invoke();
					UpdateZoomLevel();
				});
			}
			else
			{
				pOnComplete?.Invoke();
			}
		}

		private void ExecuteZoomProcedures()
		{
			//OtherLayer.FadeOut(0);

			//UpdateZoomLevel();
			//UpdateTargetCoordinateBasedInTile();
			//ReferenceTilesBetweenLayers();

			//SwapLayers();

			//DefineCenterTileOnCurrentLayer();

			//CalculateScreenBoundaries();
			//DownloadInitialTiles();

			//CurrentLayer.FadeIn(0.5f);
			//OtherLayer.FadeOut(0.5f);
			//OtherLayer.ScaleToOne(1f);

			OtherLayer.FadeOut(0);
			
			UpdateZoomLevel();

			SwapLayers();
						
			TileData data = new TileData();
			Vector3 tilePos = Vector3.zero;

			foreach(Tile t in OtherLayer.Tiles)
			{
				if(CheckTileOnScreen(t.transform.position))
				{
					if (t.transform.position.x - TILE_HALF_SIZE_IN_UNITS * t.transform.localScale.x < 0 &&
					    t.transform.position.x + TILE_HALF_SIZE_IN_UNITS * t.transform.localScale.x > 0 &&
						t.transform.position.y + TILE_HALF_SIZE_IN_UNITS * t.transform.localScale.y > 0 &&
						t.transform.position.y - TILE_HALF_SIZE_IN_UNITS * t.transform.localScale.y < 0)
					{
						data = t.TileData;
						CurrentLayer.transform.position = t.transform.position + new Vector3(-TILE_HALF_SIZE_IN_UNITS, TILE_HALF_SIZE_IN_UNITS, 0);
						tilePos = t.transform.position;
						break;
					}
				}
			}

			foreach(Tile t in CurrentLayer.Tiles)
			{
				if(CheckTileOnScreen(t.transform.position))
				{
					if (t.transform.position.x - TILE_HALF_SIZE_IN_UNITS * t.transform.localScale.x < tilePos.x &&
						t.transform.position.x + TILE_HALF_SIZE_IN_UNITS * t.transform.localScale.x > tilePos.x &&
						t.transform.position.y + TILE_HALF_SIZE_IN_UNITS * t.transform.localScale.y > tilePos.y &&
						t.transform.position.y - TILE_HALF_SIZE_IN_UNITS * t.transform.localScale.y < tilePos.y)
					{
						_centerTileData = new TileData(_currentZoomLevel, data.x * 2, data.y * 2);						
						break;						
					}
				}
			}

			CalculateScreenBoundaries();

			DownloadInitialTiles();

			CurrentLayer.FadeIn(0.5f);
			OtherLayer.FadeOut(0.5f);
			OtherLayer.ScaleToOne(1f);
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

		private void ExecuteZooming(int pZoomLevel = 0)
		{
			UpdateTargetCoordinateBasedInTile();

			_currentZoomLevel += pZoomLevel;

			if (_currentZoomLevel < MIN_ZOOM_LEVEL)
			{
				_currentZoomLevel = MIN_ZOOM_LEVEL;
				return;
			}
			if (_currentZoomLevel > MAX_ZOOM_LEVEL)
			{
				_currentZoomLevel = MAX_ZOOM_LEVEL;
				return;
			}

			DefineCenterTileOnCurrentLayer();
			CalculateScreenBoundaries();
			DownloadInitialTiles();
		}

		private void UpdateZoomLevel()
		{
			_currentZoomLevel = NextZoomLevel;
		}

		private void DoZoomDisplacement(Layer pLayer, int pDuration = 0)
		{
			if (_previousZoomLevel != _currentZoomLevel)
			{
				Vector3 dest = pLayer.transform.position + (_previousZoomLevel > _currentZoomLevel ? _displacementLevel : -_displacementLevel);

				_previousZoomLevel = _currentZoomLevel;

				TweenManager.Instance.SlideTo(pLayer.gameObject, dest, pDuration);
			}
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

		private void ScaleTo(GameObject pTarget, Vector3 pEnd, float pDuration)
		{
			TweenManager.Instance.ScaleTo(pTarget, pEnd, pDuration);
		}		

		private void DefineCenterTileOnCurrentLayer()
		{
			CurrentLayer.DefineCenterTile(_currentZoomLevel, _currentLatitude, _currentLongitude);
			_centerTileData = CurrentLayer.CenterTile.TileData;
		}

		private void PrepareTileDataDownload(Tile tile)
		{
			tile.ClearTexture();

			int distX = Mathf.RoundToInt((tile.transform.position.x - transform.position.x) / TILE_SIZE_IN_UNITS);
			int distY = Mathf.RoundToInt((tile.transform.position.y - transform.position.y) / TILE_SIZE_IN_UNITS);

			int nextX = _centerTileData.x + distX;
			int nextY = _centerTileData.y - distY;

			if(nextX > _tileCycleLimit)
			{
				int correctionX = nextX / (_tileCycleLimit + 1);
				nextX = nextX - (_tileCycleLimit + 1) * correctionX;
			}
			else if(nextX < 0)
			{
				nextX = (nextX * -1) - 1;//Inversion
				int correctionX = nextX / (_tileCycleLimit + 1);//Correction
				nextX = nextX - (_tileCycleLimit + 1) * correctionX;//Formula
				nextX = _tileCycleLimit - nextX;
			}
			
			tile.TileData = new TileData(tile.TileData.index, _currentZoomLevel, nextX, nextY);
			
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
			return tilePosition.x + size >= _screenBoundaries.left &&
				   tilePosition.x - size <= _screenBoundaries.right &&
				   tilePosition.y + size >= _screenBoundaries.bottom &&
				   tilePosition.y - size <= _screenBoundaries.top;
		}

		private void DownloadInitialTiles()
		{
			if (CurrentLayer != null)
			{
				int x = 0;
				int y = 0;

				foreach (Tile tile in CurrentLayer.Tiles)
				{					
					x = _centerTileData.x + (int)(tile.transform.localPosition.x / TILE_SIZE_IN_UNITS);//tile x position for tile size scale
					y = _centerTileData.y + (int)(tile.transform.localPosition.y / TILE_SIZE_IN_UNITS) * -1;//tile y position for tile size scale

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

				OtherLayer.ChangeRenderingLayer(CurrentLayer.RenderingOrder);
				CurrentLayer.ChangeRenderingLayer(_auxLayer.RenderingOrder);

				_auxLayer = null;
			}
		}

		private void ReferenceTilesBetweenLayers()
		{
			TileData tileData = new TileData();
			Tile firstTile = OtherLayer.GetTopLeftTile();

			tileData.zoom = _currentZoomLevel;

			foreach (Tile t in CurrentLayer.Tiles)
			{
				if (firstTile.transform.position.x >= t.transform.position.x - TILE_SIZE_IN_UNITS &&
					firstTile.transform.position.x <= t.transform.position.x + TILE_SIZE_IN_UNITS &&
					firstTile.transform.position.y >= t.transform.position.y - TILE_SIZE_IN_UNITS &&
					firstTile.transform.position.y <= t.transform.position.y + TILE_SIZE_IN_UNITS)
				{
					//Define X
					if (firstTile.transform.position.x >= t.transform.position.x - TILE_SIZE_IN_UNITS &&
						firstTile.transform.position.x <= t.transform.position.x)
					{
						tileData.x = t.X * 2;
					}
					else if (firstTile.transform.position.x >= t.transform.position.x &&
						firstTile.transform.position.x <= t.transform.position.x + TILE_SIZE_IN_UNITS)
					{
						tileData.x = t.X * 2 + 1;
					}

					//Define Y
					if (firstTile.transform.position.y <= t.transform.position.y + TILE_SIZE_IN_UNITS &&
						firstTile.transform.position.y >= t.transform.position.y)
					{
						tileData.y = t.Y * 2;
					}
					else if (firstTile.transform.position.y <= t.transform.position.y &&
						firstTile.transform.position.y >= t.transform.position.y - TILE_SIZE_IN_UNITS)
					{
						tileData.y = t.Y * 2 + 1;
					}

					tileData.RedefineName();
					firstTile.TileData = tileData;
					break;
				}
			}
						
			int height = OtherLayer.Config.height;

			Tile tileRef = null;

			for(int i = 0; i < OtherLayer.Tiles.Count; i++)
			{
				tileRef = OtherLayer.Tiles[i];

				int x = (int)((tileRef.transform.localPosition.x - firstTile.transform.localPosition.x) / TILE_SIZE_IN_UNITS);
				int y = (int)((tileRef.transform.localPosition.y - firstTile.transform.localPosition.y) / TILE_SIZE_IN_UNITS) * -1;

				tileRef.TileData = new TileData(tileRef.TileData.index, _currentZoomLevel, tileData.x + x, tileData.y + y);
			}
		}

		public void CheckCurrentLayerWithinScreenLimits()
		{
			if (CurrentLayer != null)
			{
				foreach (Tile tile in CurrentLayer.Tiles)
				{
					if (_isMovingLeft && tile.transform.position.x + TILE_HALF_SIZE_IN_UNITS < _screenBoundaries.left)
					{
						_helperVector3.x = CurrentLayer.Config.width * TILE_SIZE_IN_UNITS;
						_helperVector3.y = 0;
						_helperVector3.z = 0;

						tile.transform.localPosition += _helperVector3;
						PrepareTileDataDownload(tile);
					}

					if (_isMovingRight && tile.transform.position.x - TILE_HALF_SIZE_IN_UNITS > _screenBoundaries.right)
					{
						_helperVector3.x = CurrentLayer.Config.width * TILE_SIZE_IN_UNITS;
						_helperVector3.y = 0;
						_helperVector3.z = 0;

						tile.transform.localPosition -= _helperVector3;
						PrepareTileDataDownload(tile);
					}

					if (_isMovingUp && tile.transform.position.y - TILE_HALF_SIZE_IN_UNITS > _screenBoundaries.top)
					{
						_helperVector3.x = 0;
						_helperVector3.y = CurrentLayer.Config.height * TILE_SIZE_IN_UNITS;
						_helperVector3.z = 0;

						tile.transform.localPosition -= _helperVector3;
						PrepareTileDataDownload(tile);
					}

					if (_isMovingDown && tile.transform.position.y + TILE_HALF_SIZE_IN_UNITS < _screenBoundaries.bottom)
					{
						_helperVector3.x = 0;
						_helperVector3.y = CurrentLayer.Config.height * TILE_SIZE_IN_UNITS;
						_helperVector3.z = 0;

						tile.transform.localPosition += _helperVector3;
						PrepareTileDataDownload(tile);
					}
				}
			}
		}

		private void CheckInsideScreenInitially()
		{
			foreach (Tile tile in CurrentLayer.Tiles)
			{
				if (tile.transform.position.x + TILE_HALF_SIZE_IN_UNITS < _screenBoundaries.left)
				{
					_helperVector3.x = CurrentLayer.Config.width * TILE_SIZE_IN_UNITS;
					_helperVector3.y = 0;
					_helperVector3.z = 0;

					tile.transform.localPosition += _helperVector3;
					PrepareTileDataDownload(tile);
				}

				if (tile.transform.position.x - TILE_HALF_SIZE_IN_UNITS > _screenBoundaries.right)
				{
					_helperVector3.x = CurrentLayer.Config.width * TILE_SIZE_IN_UNITS;
					_helperVector3.y = 0;
					_helperVector3.z = 0;

					tile.transform.localPosition -= _helperVector3;
					PrepareTileDataDownload(tile);
				}

				if (tile.transform.position.y - TILE_HALF_SIZE_IN_UNITS > _screenBoundaries.top)
				{
					_helperVector3.x = 0;
					_helperVector3.y = CurrentLayer.Config.height * TILE_SIZE_IN_UNITS;
					_helperVector3.z = 0;

					tile.transform.localPosition -= _helperVector3;
					PrepareTileDataDownload(tile);
				}

				if (tile.transform.position.y + TILE_HALF_SIZE_IN_UNITS < _screenBoundaries.bottom)
				{
					_helperVector3.x = 0;
					_helperVector3.y = CurrentLayer.Config.height * TILE_SIZE_IN_UNITS;
					_helperVector3.z = 0;

					tile.transform.localPosition += _helperVector3;
					PrepareTileDataDownload(tile);
				}
			}
		}
	}
}