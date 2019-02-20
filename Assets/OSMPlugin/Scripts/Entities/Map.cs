﻿using System.Collections;
using System.Collections.Generic;
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
			ExecuteZooming(0);
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
				layer.DefineTilesNeighbours();

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

			DefineCenterTileOnCurrentLayer();

			//StartCoroutine(CentralizeMapInTile());
		}

		public void ZoomIn()
		{
			ExecuteZooming(1);
		}

		public void ZoomOut()
		{
			ExecuteZooming(-1);
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
			DefineAllTilesNameBasedOnTile();

			DownloadInitialTiles();

			CheckCurrentLayerWithFrustum();

			//StartCoroutine(CentralizeMapInTile());

			//DoZoomDisplacement();
		}

		private void DoZoomDisplacement()
		{
			//StartCoroutine(Displacement(_currentZoomLevel));
		}

		private void ScaleTo(GameObject pTarget, Vector3 pEnd, float pDuration)
		{
			StartCoroutine(DoScaleTo(pTarget, pEnd, pDuration));
		}

		private void DefineCenterTileOnCurrentLayer()
		{
			_currentLayer.DefineCenterTile(_currentZoomLevel, _currentLatitude, _currentLongitude);
		}

		private void DefineAllTilesNameBasedOnTile()
		{
			if (_currentLayer != null)
			{
				ApplyTileLogicsForTargetTile(_currentLayer.TargetTile, true, true, true, true, true, true, true, true);

				Tile tile = _currentLayer.Tiles[_currentLayer.TargetTile.NorthWestNeighbour];
				ApplyTileLogicsForTargetTile(tile, true, true, false, false, false, true, true, true);
												
				tile = _currentLayer.Tiles[_currentLayer.TargetTile.SouthWestNeighbour];
				ApplyTileLogicsForTargetTile(tile, false, false, false, true, true, true, true, false);

				tile = _currentLayer.Tiles[_currentLayer.TargetTile.SouthEastNeighbour];
				ApplyTileLogicsForTargetTile(tile, false, false, true, true, true, false, false, false);

				tile = _currentLayer.Tiles[_currentLayer.TargetTile.NorthEastNeighbour];
				ApplyTileLogicsForTargetTile(tile, true, true, true, true, false, false, false, false);
			}
		}

		private void ApplyTileLogicsForTargetTile(Tile tile, bool north, bool northEast, bool east, bool southEast, bool south, bool southWest, bool west, bool northWest)
		{
			int index = 0;
			int x = tile.X;
			int y = tile.Y;

			TileNeighbours neighbours = OSMGeoHelper.GetTileNeighbourNames(tile.TileData);

			if (north)
			{
				index = tile.NorthNeighbour;
				_currentLayer.Tiles[index].TileData = new TileData(index, _currentZoomLevel, x, y - 1, neighbours.northNeighbour);
			}

			if (south)
			{
				index = tile.SouthNeighbour;
				_currentLayer.Tiles[index].TileData = new TileData(index, _currentZoomLevel, x, y + 1, neighbours.southNeighbour);
			}

			if (east)
			{
				index = tile.EastNeighbour;
				_currentLayer.Tiles[index].TileData = new TileData(index, _currentZoomLevel, x + 1, y, neighbours.eastNeighbour);
			}

			if (west)
			{
				index = tile.WestNeighbour;
				_currentLayer.Tiles[index].TileData = new TileData(index, _currentZoomLevel, x - 1, y, neighbours.westNeighbour);
			}

			if (northWest)
			{
				index = tile.NorthWestNeighbour;
				_currentLayer.Tiles[index].TileData = new TileData(index, _currentZoomLevel, x - 1, y - 1, neighbours.northWestNeighbour);
			}

			if (northEast)
			{
				index = tile.NorthEastNeighbour;
				_currentLayer.Tiles[index].TileData = new TileData(index, _currentZoomLevel, x + 1, y - 1, neighbours.northEastNeighbour);
			}

			if (southWest)
			{
				index = tile.SouthWestNeighbour;
				_currentLayer.Tiles[index].TileData = new TileData(index, _currentZoomLevel, x - 1, y + 1, neighbours.southWestNeighbour);
			}

			if (southEast)
			{
				index = tile.SouthEastNeighbour;
				_currentLayer.Tiles[index].TileData = new TileData(index, _currentZoomLevel, x + 1, y + 1, neighbours.southEastNeighbour);
			}
		}

		private void DownloadInitialTiles()
		{
			if(_currentLayer != null)
			{
				foreach(Tile tile in _currentLayer.Tiles)
				{
					//if (tile.isActiveAndEnabled)
					{
						DoTileDownload(tile.TileData);
					}
				}
			}
		}

		private void DoTileDownload(TileData pTileData)
		{
			if (string.IsNullOrEmpty(pTileData.name) == false)
			{
				if (pTileData.zoom != _currentZoomLevel || pTileData.x < 0 || pTileData.y < 0)
				{
					pTileData.name = null;
					_currentLayer.SetTexture(pTileData.index, null);
				}
				else
				{
					TileDownloadManager.Instance.DownloadTileImageByTileName(pTileData.name,
						(Texture2D texture) => {
							_currentLayer.SetTexture(pTileData.index, texture);
						}
					);
				}
			}
		}

		private IEnumerator Displacement(float pZoomLevel)
		{
			if (_previousZoomLevel != pZoomLevel)
			{
				Vector3 init = _currentLayer.TargetTile.transform.position;
				Vector3 dest = init + (_previousZoomLevel > pZoomLevel ? _displacementLevel : -_displacementLevel);

				_previousZoomLevel = pZoomLevel;

				float t = 0;

				while (t < 1)
				{
					yield return new WaitForSeconds(0.01f);

					t += 0.1f;

					_currentLayer.TargetTile.transform.position = Vector3.Lerp(init, dest, t);
				}

				_currentLayer.TargetTile.transform.position = dest;
			}
		}

		private IEnumerator CentralizeMapInTile()
		{
			Tile target = _currentLayer.TargetTile;

			Vector3 init = transform.position;
			Vector3 dest = init - target.transform.localPosition;

			float t = 0;

			yield return new WaitForSeconds(1);

			while (t < 1)
			{
				yield return new WaitForSeconds(0.01f);

				t += 0.1f;

				transform.position = Vector3.Lerp(init, dest, t);
			}

			transform.position = dest;
		}

		private IEnumerator DoScaleTo(GameObject pTarget, Vector3 pEnd, float pDuration)
		{
			_isScaling = true;

			float elapsedTime = 0;
			Vector3 initPos = pTarget.transform.localScale;

			while (elapsedTime < pDuration)
			{
				pTarget.transform.localScale = Vector3.Lerp(initPos, pEnd, elapsedTime / pDuration);
				elapsedTime += Time.deltaTime;
				yield return null;
			}

			pTarget.transform.localScale = pEnd;
			_isScaling = false;

			_currentLayer.FadeOut(1, 0, 1);
		}

		public void CheckCurrentLayerWithFrustum()
		{
			if (_currentLayer != null)
			{				
				foreach (Tile tile in _currentLayer.Tiles)
				{
					if(tile.transform.position.x + TILE_HALF_SIZE_IN_UNITS >= left && tile.transform.position.x - TILE_HALF_SIZE_IN_UNITS < right &&
					   tile.transform.position.y + TILE_HALF_SIZE_IN_UNITS > bottom && tile.transform.position.y - TILE_HALF_SIZE_IN_UNITS < top)
					{
						tile.gameObject.SetActive(true);
					}
					else
					{
						tile.gameObject.SetActive(false);
					}
				}
			}
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