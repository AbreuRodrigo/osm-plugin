using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OSM
{
	public class Map : MonoBehaviour
	{
		public const int MIN_ZOOM_LEVEL = 3;
		public const int MAX_ZOOM_LEVEL = 19;

		private const int TILE_SIZE_IN_PIXELS = 256;
		private const float TILE_SIZE_IN_UNITS = TILE_SIZE_IN_PIXELS * 0.01f;
		private const float TILE_HALF_SIZE_IN_UNITS = TILE_SIZE_IN_PIXELS * 0.005f;
		private const string LAYER_BASE_NAME = "Layer";

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

		private int _tileCycleLimit;		

		public ScreenBoundaries _screenBoundaries;

		//Movement Detection
		public bool _isMovingLeft;
		public bool _isMovingRight;
		public bool _isMovingUp;
		public bool _isMovingDown;
		public bool _isStopped;

		public float TileSize { get; private set; }
		public float CurrentZoomLevel { get { return _currentZoomLevel; } }

		private TileData _centerTileData;
		private Vector3 _displacementLevel;

		private bool _isScaling;

		private void Start()
		{
			StopMovements();
			InitializeMap();
			InitializeLayers();
			InitialZoom();

			CalculateScreenBoundaries();
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
			//DoScaleTo(_currentLayer.gameObject, zoomPercent);
						
			if (_tileValidationCounter >= _tileValidationSeconds)
			{
				ValidateTiles();
				_tileValidationCounter = 0;
			}

			_tileValidationCounter += Time.deltaTime;
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
			foreach(Tile tile in CurrentLayer.Tiles)
			{
				if(CheckTileOnScreen(tile.transform.position) && tile.TextureUpToDate == false)
				{
					DoTileDownload(tile.TileData);
				}
			}
		}

		protected void InitializeMap()
		{
			TileSize = _tileTemplate.TileSize.x;

			if(TileSize == 0)
			{
				TileSize = TILE_SIZE_IN_PIXELS;
			}

			_displacementLevel = new Vector3(TileSize * 0.5f, TileSize * 0.5f, 0);
		}

		protected void InitializeLayers()
		{
			int layerIndex = 0;

			foreach(LayerConfig layerConfig in layerConfigs)
			{
				Layer layer = Instantiate(_layerTemplate, transform);
				layer.gameObject.name = LAYER_BASE_NAME + ( layerIndex + 1 );
				layer.Index = layerIndex;
				layer.Config = layerConfig;
				layer.SetTileSize(TileSize);
				layer.CreateTilesByLayer(_tileTemplate, _currentZoomLevel);
				layer.OrganizeTilesAsGrid();

				if (CurrentLayer == null)
				{
					CurrentLayer = layer;
				}
				else if(OtherLayer == null)
				{
					OtherLayer = layer;
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

		public void PrepareZoomIn(float pZoomDuration, Action pOnComplete)
		{			
			if (_currentZoomLevel < MAX_ZOOM_LEVEL)
			{
				NextZoomLevel++;

				CurrentLayer.FadeOut(pZoomDuration * 2);

				TweenManager.Instance.ScaleTo(CurrentLayer.gameObject, CurrentLayer.gameObject.transform.localScale * 2, pZoomDuration, TweenType.Linear, true, null, () => 
				{
					pOnComplete?.Invoke();
					CurrentLayer.gameObject.transform.localScale = Vector3.one;
				});

				SwapLayers();
			}
		}

		public void PrepareZoomOut(float pZoomDuration, Action pOnComplete)
		{			
			if (_currentZoomLevel > MIN_ZOOM_LEVEL)
			{
				NextZoomLevel--;

				TweenManager.Instance.ScaleTo(CurrentLayer.gameObject, CurrentLayer.gameObject.transform.localScale / 2, pZoomDuration, TweenType.Linear, true, null, () => 
				{
					pOnComplete?.Invoke();
					CurrentLayer.gameObject.transform.localScale = Vector3.one;
				});

				SwapLayers();
			}
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

		private void ExecuteZooming(int pZoomLevel = 0)
		{
			_currentZoomLevel += pZoomLevel;

			if (_currentZoomLevel < MIN_ZOOM_LEVEL)
			{
				_currentZoomLevel = MIN_ZOOM_LEVEL;
			}
			if (_currentZoomLevel > MAX_ZOOM_LEVEL)
			{
				_currentZoomLevel = MAX_ZOOM_LEVEL;
			}
			
			CalculateScreenBoundaries();
			DefineCenterTileOnCurrentLayer();
			CheckCurrentLayerWithinScreenLimits();
			DownloadInitialTiles();
		}

		private void DoZoomDisplacement()
		{
			if (_previousZoomLevel != _currentZoomLevel)
			{
				Vector3 dest = CurrentLayer.transform.position + (_previousZoomLevel > _currentZoomLevel ? _displacementLevel : -_displacementLevel);

				_previousZoomLevel = _currentZoomLevel;

				TweenManager.Instance.SlideTo(CurrentLayer.gameObject, dest, 1);
			}
		}

		private void ScaleTo(GameObject pTarget, Vector3 pEnd, float pDuration)
		{
			TweenManager.Instance.ScaleTo(pTarget, pEnd, pDuration);
		}		

		private void DefineCenterTileOnCurrentLayer()
		{
			CurrentLayer.DefineCenterTile(_currentZoomLevel, _currentLatitude, _currentLongitude);
			_centerTileData = CurrentLayer.CenterTile.TileData;
			DoTileDownload(_centerTileData);
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
			if (pTileData.zoom != _currentZoomLevel || pTileData.x < 0 || pTileData.y < 0)
			{
				pTileData.name = null;
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

		private bool CheckTileOnScreen(Vector3 tilePosition)
		{
			return tilePosition.x + TILE_HALF_SIZE_IN_UNITS >= _screenBoundaries.left &&
				   tilePosition.x - TILE_HALF_SIZE_IN_UNITS <= _screenBoundaries.right &&
				   tilePosition.y + TILE_HALF_SIZE_IN_UNITS >= _screenBoundaries.bottom &&
				   tilePosition.y - TILE_HALF_SIZE_IN_UNITS <= _screenBoundaries.top;
		}

		private void DownloadInitialTiles()
		{
			if (CurrentLayer != null)
			{
				foreach (Tile tile in CurrentLayer.Tiles)
				{
					int x = (int)(tile.transform.localPosition.x / TILE_SIZE_IN_UNITS);//tile x position for tile size scale
					int y = (int)(tile.transform.localPosition.y / TILE_SIZE_IN_UNITS) * -1;//tile y position for tile size scale

					tile.TileData = new TileData(tile.TileData.index, _currentZoomLevel, _centerTileData.x + x, _centerTileData.y + y);

					DoTileDownload(tile.TileData);
				}
			}
		}

		private void SwapLayers()
		{
			if (CurrentLayer != null && OtherLayer != null)
			{
				float fadingSpeed = 0.5f;

				//OtherLayer.FadeOut(0);
				//CurrentLayer.FadeOut(fadingSpeed);

				/*_auxLayer = CurrentLayer;
				CurrentLayer = OtherLayer;
				OtherLayer = _auxLayer;

				OtherLayer.ChangeRenderingLayer(CurrentLayer.RenderingOrder);
				CurrentLayer.ChangeRenderingLayer(_auxLayer.RenderingOrder);*/
												
				if (NextZoomLevel != _currentZoomLevel)
				{
					ExecuteZooming(NextZoomLevel > _currentZoomLevel ? 1 : -1);
					NextZoomLevel = _currentZoomLevel;
				}				

				_auxLayer = null;
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
						tile.transform.localPosition += new Vector3(CurrentLayer.Config.width * TILE_SIZE_IN_UNITS, 0, 0);
						PrepareTileDataDownload(tile);
					}

					if (_isMovingRight && tile.transform.position.x - TILE_HALF_SIZE_IN_UNITS > _screenBoundaries.right)
					{
						tile.transform.localPosition -= new Vector3(CurrentLayer.Config.width * TILE_SIZE_IN_UNITS, 0, 0);
						PrepareTileDataDownload(tile);
					}

					if (_isMovingUp && tile.transform.position.y - TILE_HALF_SIZE_IN_UNITS > _screenBoundaries.top)
					{
						tile.transform.localPosition -= new Vector3(0, CurrentLayer.Config.height * TILE_SIZE_IN_UNITS, 0);
						PrepareTileDataDownload(tile);
					}

					if (_isMovingDown && tile.transform.position.y + TILE_HALF_SIZE_IN_UNITS < _screenBoundaries.bottom)
					{
						tile.transform.localPosition += new Vector3(0, CurrentLayer.Config.height * TILE_SIZE_IN_UNITS, 0);
						PrepareTileDataDownload(tile);
					}
				}
			}
		}
	}
}