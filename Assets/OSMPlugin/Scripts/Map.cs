using System.Collections;
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
		
		private void Start()
		{			
			InitializeMap();
			InitializeLayers();
			DownloadInitialTiles();
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
				layer.CreateTilesByLayer(_tileTemplate);
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

			_currentLayer.DefineCenterTile(_currentZoomLevel, _currentLatitude, _currentLongitude);
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

			_currentLayer.DefineCenterTile(_currentZoomLevel, _currentLatitude, _currentLongitude);

			DownloadInitialTiles();

			//DoZoomDisplacement();
		}

		private void DoZoomDisplacement()
		{
			StartCoroutine(Displacement(_currentZoomLevel));
		}

		private void ScaleTo(GameObject pTarget, Vector3 pEnd, float pDuration)
		{
			StartCoroutine(DoScaleTo(pTarget, pEnd, pDuration));
		}

		private void DownloadInitialTiles()
		{
			if(_currentLayer != null)
			{
				//Download the central tile
				DoTileDownload(_currentLayer.CenterTile.Index, _currentLayer.CenterTile.TileData.name);
				
				TileNeighbours neighbours = OSMGeoHelper.GetTileNeighbourNames(_currentLayer.CenterTile.TileData);
				DoTileDownload(_currentLayer.CenterTile.NorthNeighbour, neighbours.northNeighbour);
				DoTileDownload(_currentLayer.CenterTile.NorthEastNeighbour, neighbours.northEastNeighbour);
				DoTileDownload(_currentLayer.CenterTile.EastNeighbour, neighbours.eastNeighbour);
				DoTileDownload(_currentLayer.CenterTile.SouthEastNeighbour, neighbours.southEastNeighbour);
				DoTileDownload(_currentLayer.CenterTile.SouthNeighbour, neighbours.southNeighbour);
				DoTileDownload(_currentLayer.CenterTile.SouthWestNeighbour, neighbours.southWestNeighbour);
				DoTileDownload(_currentLayer.CenterTile.WestNeighbour, neighbours.westNeighbour);
				DoTileDownload(_currentLayer.CenterTile.NorthWestNeighbour, neighbours.northWestNeighbour);
			}
		}

		private void DoTileDownload(int pTileIndex, string pTileName)
		{
			if (string.IsNullOrEmpty(pTileName) == false)
			{
				TileDownloadManager.Instance.DownloadTileImageByTileName(pTileName,
					(Texture2D texture) => {
						_currentLayer.SetTexture(pTileIndex, texture);
					}
				);
			}
		}

		private IEnumerator Displacement(float pZoomLevel)
		{
			if (_previousZoomLevel != pZoomLevel)
			{
				Vector3 init = _currentLayer.CenterTile.transform.position;
				Vector3 dest = init + (_previousZoomLevel > pZoomLevel ? _displacementLevel : -_displacementLevel);

				_previousZoomLevel = pZoomLevel;

				float t = 0;

				while (t < 1)
				{
					yield return new WaitForSeconds(0.01f);

					t += 0.1f;

					_currentLayer.CenterTile.transform.position = Vector3.Lerp(init, dest, t);
				}

				_currentLayer.CenterTile.transform.position = dest;
			}
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