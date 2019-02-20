﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OSM
{
	public class Layer : MonoBehaviour
	{
		private float _tileSize;
		private List<Tile> _tiles = new List<Tile>();
		private int _centerIndex;
		private int _gridSqrtSize;
		private int _totalTiles;

		private Tile[,] _myTiles;

		[SerializeField]
		private int _index;
		[SerializeField]
		private LayerConfig _config;

		public TileData CenterTileData { get; set; }

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
			int index = CenterTile.Index;
			CenterTile.TileData = OSMGeoHelper.GetTileData(pZoom, pLatitude, pLongitude);
			CenterTile.Index = index;
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
					tile.SetRenderingLayer(_config.layerOrder);

					_myTiles[x, y] = tile;

					AddTile(tile);

					tileIndex++;
				}
			}
		}

		public void ManualFadeOut(float pInit, float pEnd, float pPercent)
		{
			foreach (Tile tile in _tiles)
			{
				tile.SetAlpha(Mathf.Lerp(pInit, pEnd, pPercent));
			}
		}

		public void FadeOut(float pInit, float pEnd, float pDuration)
		{
			StartCoroutine(FadeOutTiles(pInit, pEnd, pDuration));
			//InitAlphaUpdate();
		}

		private IEnumerator FadeOutTiles(float pInit, float pEnd, float pDuration)
		{
			float elapsedTime = 0;

			while (elapsedTime <= pDuration)
			{
				foreach(Tile tile in _tiles)
				{
					tile.SetAlpha(Mathf.Lerp(pInit, pEnd, elapsedTime / pDuration));					
				}

				elapsedTime += Time.deltaTime;

				yield return null;
			}
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

		private void UpdateAlpha()
		{
			if (isChangingAlpha == true)
			{
				foreach (Tile tile in _tiles)
				{
					tile.SetAlpha(Mathf.Lerp(alphaInit, alphaEnd, elapsedTime / alphaDuration));
				}

				elapsedTime += Time.deltaTime;

				if(elapsedTime > alphaDuration)
				{
					isChangingAlpha = false;
				}
			}
		}
	}
}