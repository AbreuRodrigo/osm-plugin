using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace OSM
{
	public class Map : MonoBehaviour
	{
		#region Constants
		private const int MIN_ZOOM_LEVEL = 3;
		private const int MAX_ZOOM_LEVEL = 19;
		private const int TILE_SIZE_IN_PIXELS = 256;
		private const float TILE_SIZE_IN_UNITS = TILE_SIZE_IN_PIXELS * 0.01f;
		private const float TILE_HALF_SIZE_IN_UNITS = TILE_SIZE_IN_PIXELS * 0.005f;
		private const string LAYER_BASE_NAME = "Layer";
		#endregion

		[Header("Layer Properties")]
		[SerializeField]
		private List<LayerConfig> layerConfigs;

		[Header("General Properties")]
		[SerializeField]
		private double _currentLatitude = 49.2674573;
		[SerializeField]
		private double _currentLongitude = -123.0930032;

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

		private float _tileSize;
		private List<Layer> _layers = new List<Layer>();
		private Layer _currentLayer;
		private Layer _nextLayer;

		[SerializeField]
		private TileData _centerTileData;

		private Vector3 _displacementLevel;

		private bool _isScaling;

		public float left;
		public float top;
		public float right;
		public float bottom;

		private void Awake()
		{
			CalculateCorners();
		}

		private void Start()
		{
			InitializeMap();
			InitializeLayers();
			InitialZoom();
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

		public void CalculateCorners()
		{
			Vector3 maxScreenLimit = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, 1)) * 10;
			top = maxScreenLimit.y;
			bottom = -top;
			right = maxScreenLimit.x;
			left = -right;
		}

		protected void InitializeMap()
		{
			_tileSize = _tileTemplate.TileSize.x;

			if(_tileSize == 0)
			{
				_tileSize = TILE_SIZE_IN_PIXELS;
			}

			_displacementLevel = new Vector3(_tileSize * 0.5f, _tileSize * 0.5f, 0);
		}

		protected void InitializeLayers()
		{
			int layerIndex = 0;

			//Creating the total layers and it's tiles
			foreach(LayerConfig layerConfig in layerConfigs)
			{
				Layer layer = Instantiate(_layerTemplate, transform);
				layer.gameObject.name = LAYER_BASE_NAME + ( layerIndex + 1 );
				layer.Index = layerIndex;
				layer.Config = layerConfig;
				layer.SetTileSize(_tileSize);
				layer.CreateTilesByLayer(_tileTemplate, _currentZoomLevel);
				layer.OrganizeTilesAsGrid();

				if (_currentLayer == null)
				{
					_currentLayer = layer;
				}
				else if(_nextLayer == null)
				{
					_nextLayer = layer;
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

			DefineCenterTileOnCurrentLayer();
			DownloadInitialTiles();
		}

		private void ExecuteZooming(int pZoomLevel = 0)
		{
			_currentZoomLevel += pZoomLevel;

			/*if (_currentZoomLevel >= MIN_ZOOM_LEVEL && _currentZoomLevel <= MAX_ZOOM_LEVEL)
			{
				ScaleTo(_currentLayer.gameObject, _currentLayer.transform.localScale * (pZoomLevel == 1 ? 2 : 0.5f), 0.25f);
			}*/

			if (_currentZoomLevel < MIN_ZOOM_LEVEL)
			{
				_currentZoomLevel = MIN_ZOOM_LEVEL;
			}

			if (_currentZoomLevel > MAX_ZOOM_LEVEL)
			{
				_currentZoomLevel = MAX_ZOOM_LEVEL;
			}

			DefineCenterTileOnCurrentLayer();
			CheckCurrentLayerWithinScreenLimits();
			DownloadInitialTiles();
		}

		private void DoZoomDisplacement()
		{
			if (_previousZoomLevel != _currentZoomLevel)
			{
				Vector3 dest = _currentLayer.CenterTile.transform.position + (_previousZoomLevel > _currentZoomLevel ? _displacementLevel : -_displacementLevel);

				_previousZoomLevel = _currentZoomLevel;

				TweenManager.Instance.SlideTo(_currentLayer.CenterTile.gameObject, dest, 1);
			}
		}

		private void ScaleTo(GameObject pTarget, Vector3 pEnd, float pDuration)
		{
			TweenManager.Instance.ScaleTo(pTarget, pEnd, pDuration);
		}

		private void DefineCenterTileOnCurrentLayer()
		{
			_currentLayer.DefineCenterTile(_currentZoomLevel, _currentLatitude, _currentLongitude);
			_centerTileData = _currentLayer.CenterTile.TileData;
			DoTileDownload(_centerTileData);
		}
				
		private void DownloadInitialTiles()
		{
			if(_currentLayer != null)
			{
				foreach(Tile tile in _currentLayer.Tiles)
				{
					int x = (int)(tile.transform.localPosition.x / TILE_SIZE_IN_UNITS);
					int y = (int)(tile.transform.localPosition.y / TILE_SIZE_IN_UNITS) * -1;//Y up is crescent, for the map it's the oposite

					tile.TileData = new TileData(tile.TileData.index, _currentZoomLevel, _centerTileData.x + x, _centerTileData.y + y);

					DoTileDownload(tile.TileData);
				}
			}
		}

		private void PrepareTileDataDownload(Tile tile)
		{
			tile.ClearTexture();

			int distX = Mathf.RoundToInt((tile.transform.position.x - transform.position.x) / TILE_SIZE_IN_UNITS);
			int distY = Mathf.RoundToInt((tile.transform.position.y - transform.position.y) / TILE_SIZE_IN_UNITS);

			tile.TileData = new TileData(tile.TileData.index, _currentZoomLevel, _centerTileData.x + distX, _centerTileData.y + distY);
			tile.TileData = new TileData(tile.TileData.index, _currentZoomLevel, _centerTileData.x + distX, _centerTileData.y - distY);

			DoTileDownload(tile.TileData);
		}

		private void DoTileDownload(TileData pTileData)
		{			
			if (pTileData.zoom != _currentZoomLevel || pTileData.x < 0 || pTileData.y < 0)
			{
				pTileData.name = null;
				_currentLayer.SetTexture(pTileData.index, null);
			}
			else
			{
				TileDownloadManager.Instance.DownloadTileImage(pTileData.name, (Texture2D texture) =>
				{
					_currentLayer.SetTexture(pTileData.index, texture);
				});
			}			
		}

		public void CheckCurrentLayerWithinScreenLimits()
		{
			if (_currentLayer != null)
			{
				foreach (Tile tile in _currentLayer.Tiles)
				{
					if (tile.transform.position.x + TILE_HALF_SIZE_IN_UNITS < left)
					{
						tile.transform.localPosition += new Vector3(_currentLayer.Config.width * TILE_SIZE_IN_UNITS, 0, 0);
						PrepareTileDataDownload(tile);
					}
					//if (tile.transform.position.x - TILE_HALF_SIZE_IN_UNITS > right)
					//{
					//	tile.transform.localPosition -= new Vector3(_currentLayer.Config.width * TILE_SIZE_IN_UNITS, 0, 0);
					//PrepareTileDataDownload(tile);
					//}
					if (tile.transform.position.y - TILE_HALF_SIZE_IN_UNITS > top)
					{
						tile.transform.localPosition -= new Vector3(0, _currentLayer.Config.height * TILE_SIZE_IN_UNITS, 0);
						PrepareTileDataDownload(tile);
					}
					//if (tile.transform.position.y + TILE_HALF_SIZE_IN_UNITS < bottom)
					//{
					//	tile.transform.localPosition += new Vector3(0, _currentLayer.Config.height * TILE_SIZE_IN_UNITS, 0);
					//PrepareTileDataDownload(tile);
					//}
				}
			}
		}

		private bool CheckTileOnScreen(Vector3 tilePosition)
		{
			return tilePosition.x + TILE_HALF_SIZE_IN_UNITS > left &&
				   tilePosition.x - TILE_HALF_SIZE_IN_UNITS < right &&
				   tilePosition.y + TILE_HALF_SIZE_IN_UNITS > bottom &&
				   tilePosition.y - TILE_HALF_SIZE_IN_UNITS < top;
		}

		#region TestOnly

		[Range(0, 1)]
		public float zoomPercent;

		public void LateUpdate()
		{
			DoScaleTo(_currentLayer.gameObject, zoomPercent);
		}

		private void DoScaleTo(GameObject pTarget, float pPercent)
		{
			pTarget.transform.localScale = Vector3.Lerp(Vector3.one, new Vector3(2, 2, 1), pPercent);

			if (_nextLayer != null && _nextLayer.isActiveAndEnabled)
			{
				_nextLayer.transform.localScale = pTarget.transform.localScale * 0.5f;
			}

			if (pPercent >= 0.5f)
			{
				_currentLayer.ManualFadeOut(1, 0, pPercent);
			}
			else
			{
				_currentLayer.ManualFadeOut(1, 0, 0);
			}

			if(pPercent == 1)
			{
				
			}
		}

		#endregion
	}
}