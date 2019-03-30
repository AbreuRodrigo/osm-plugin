using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OSM
{
	public class Layer : MonoBehaviour
	{
		private const int BACK_RENDERING_LAYER = 2;
		private const int MIDDLE_RENDERING_LAYER = 3;
		private const int FRONT_RENDERING_LAYER = 4;

		private float _tileSize;
		private List<Tile> _tiles = new List<Tile>();
		private int _centerIndex;
		private int _gridSqrtSize;
		private int _totalTiles;

		private Tile[,] _myTiles;

		[SerializeField]
		private int _index;
		[SerializeField]
		private int _renderingOrder;

		private LayerConfig _config;

		private bool _isScalingToOne;
		private float _scalingToOneDelay = 0;
		private float _scalingToOneTime = 0;

		private bool _isFadingOut;
		private float _fadeOutTimer = 0;
		private float _fadeOutDuration = 0;

		public float TileSize { get { return _tileSize; } }
		public int RenderingOrder { get { return _renderingOrder; } }
		public TileData CenterTileData { get; set; }

		private event Action _onCompleteFadingOut;

		public LayerConfig Config
		{
			get { return _config; }
			set {
				_config = value;
				_totalTiles = _config.width * _config.height;
				_gridSqrtSize = Mathf.RoundToInt(Mathf.Sqrt(_totalTiles));
			}
		}

		public int Index
		{
			get { return _index; }
			set { _index = value; }
		}

		public List<Tile> Tiles
		{
			get { return _tiles; }
		}

		public Tile CenterTile { get; private set; }

		private void LateUpdate()
		{
			if(_isScalingToOne == true)
			{
				_scalingToOneTime += Time.deltaTime;

				if (_scalingToOneTime >= _scalingToOneDelay)
				{
					_isScalingToOne = false;
					gameObject.transform.localScale = Vector3.one;
					_scalingToOneTime = 0;
				}
			}

			if(_isFadingOut == true)
			{
				_fadeOutTimer += Time.deltaTime;

				if (_fadeOutTimer >= _fadeOutDuration)
				{
					_isFadingOut = false;
					_fadeOutTimer = 0;
					_fadeOutDuration = 0;

					_onCompleteFadingOut?.Invoke();
				}
			}
		}

		public void AddTile(Tile pTile)
		{
			if(_tiles != null)
			{
				_tiles.Add(pTile);
			}
		}

		public void SetTileSize(float pTileSize)
		{
			_tileSize = pTileSize;
		}

		public void SetTexture(int pIndex, Texture2D pTexture)
		{
			if(_tiles != null && _tiles.Count > pIndex)
			{
				_tiles[pIndex].SetTileTexture(pTexture);
			}
		}

		public void ActivateTile(int pIndex)
		{
			if (_tiles != null && _tiles.Count > pIndex)
			{
				_tiles[pIndex].gameObject.SetActive(true);
			}
		}

		public void SetCenterTexture(Texture2D pTexture)
		{
			SetTexture(_centerIndex, pTexture);
		}

		public void OrganizeTilesAsGrid()
		{	
			int halfSqrt = _gridSqrtSize / 2;
			float halfSize = _tileSize / 2;
			bool isEven = _totalTiles % 2 == 0;

			float modifier = (isEven ? halfSqrt - 1 : halfSqrt) * _tileSize + (isEven ? halfSize : 0);

			Vector3 fixedPos;
			Tile tileRef = null;

			int i = 0;
			for(int x = 0; x < _gridSqrtSize; x++)
			{
				for(int y = 0; y < _gridSqrtSize; y++)
				{
					tileRef = _tiles[i];

					//Redefining the position
					fixedPos.x = x * _tileSize - modifier;
					fixedPos.y = -y * _tileSize + modifier;
					fixedPos.z = 0;

					tileRef.transform.position = fixedPos;
					tileRef.gameObject.SetActive(true);

					i++;
				}
			}
		}

		public void DefineCenterTile(int pZoom, double pLatitude, double pLongitude)
		{
			CenterTile = _tiles[_totalTiles / 2];
			TileData data = OSMGeoHelper.GetTileData(pZoom, pLatitude, pLongitude);
			data.index = CenterTile.TileData.index;
			CenterTile.TileData = data;
		}

		private int GetTileIndexByXY(int x, int y)
		{
			if (x < 0 || y < 0 || x >= _config.width || y >= _config.height)
			{
				return -1;
			}

			return _myTiles[x, y].Index;
		}

		public void CreateTilesByLayer(Tile pTileTemplate, int pZoomLevel)
		{
			int tileIndex = 0;

			_myTiles = new Tile[_config.width, _config.height];
			_renderingOrder = _config.layerOrder;

			for (int x = 0; x < _config.width; x++)
			{
				for (int y = 0; y < _config.height; y++)
				{
					Tile tile = Instantiate(pTileTemplate, transform);
					tile.gameObject.SetActive(false);
					tile.Index = tileIndex;
					tile.X = x;
					tile.Y = y;
					tile.Zoom = pZoomLevel;
					tile.SetRenderingLayer(_renderingOrder);

					_myTiles[x, y] = tile;

					AddTile(tile);

					tileIndex++;
				}
			}
		}

		public void ChangeRenderingLayer(int pNewLayerOrder)
		{
			_renderingOrder = pNewLayerOrder;

			foreach (Tile tile in _tiles)
			{
				if(tile != null)
				{
					tile.SetRenderingLayer(_renderingOrder);					
				}
			}
		}

		public void ChangeToBackLayer()
		{
			ChangeRenderingLayer(BACK_RENDERING_LAYER);
		}

		public void ChangeToMiddleLayer()
		{
			ChangeRenderingLayer(MIDDLE_RENDERING_LAYER);
		}

		public void ChangeToFrontLayer()
		{
			ChangeRenderingLayer(FRONT_RENDERING_LAYER);
		}

		public void ManualFadeOut(float pInit, float pEnd, float pPercent)
		{
			foreach (Tile tile in _tiles)
			{
				tile.SetAlpha(Mathf.Lerp(pInit, pEnd, pPercent));
			}
		}

		public void FadeOut(float pDuration, Action pOnComplete = null)
		{
			if (_isFadingOut == false)
			{	
				if(pOnComplete != null)
				{
					_onCompleteFadingOut -= pOnComplete;
					_onCompleteFadingOut += pOnComplete;
				}

				_fadeOutDuration = pDuration;
				_fadeOutTimer = 0;
				_isFadingOut = true;

				FadeOutAllTiles(pDuration);
			}
		}

		private void FadeOutAllTiles(float pDuration)
		{
			foreach (Tile tile in _tiles)
			{
				tile.FadeOut(pDuration);
			}
		}

		public void FadeIn(float pDuration)
		{
			foreach (Tile tile in _tiles)
			{
				tile.FadeIn(pDuration);
			}
		}

		public void ScaleToOne(float pDelay)
		{
			_isScalingToOne = true;
			_scalingToOneDelay = pDelay;
		}

		public void ClearTileTextures()
		{
			foreach (Tile tile in _tiles)
			{
				tile.ClearTexture();
			}
		}

		public Tile GetTopLeftTile()
		{
			Tile tile = _tiles[0];
			
			foreach (Tile t in _tiles)
			{
				if(t.transform.position.x <= tile.transform.position.x && t.transform.position.y >= tile.transform.position.y)
				{
					tile = t;
				}
			}

			return tile;
		}

		bool isChangingAlpha = false;
		float alphaInit, alphaEnd, alphaDuration, elapsedTime;

		private void InitAlphaUpdate()
		{
			if(isChangingAlpha == false)
			{
				elapsedTime = 0;
				alphaInit = 1;
				alphaEnd = 0;
				alphaDuration = 5;
				isChangingAlpha = true;
			}
		}
	}
}