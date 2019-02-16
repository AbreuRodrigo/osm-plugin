using System.Collections;
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

		[SerializeField]
		private int _index;
		[SerializeField]
		private LayerConfig _config;

		public LayerConfig Config
		{
			get { return _config; }
			set { _config = value; }
		}

		public int Index
		{
			get { return _index; }
			set { _index = value; }
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
			_totalTiles = _tiles.Count;
			_gridSqrtSize = Mathf.RoundToInt(Mathf.Sqrt(_totalTiles));

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
			if(_tiles != null)
			{
				_centerIndex = _tiles.Count / 2;
				CenterTile = _tiles[_centerIndex];
				CenterTile.TileData = OSMGeoHelper.GetTileData(pZoom, pLatitude, pLongitude);
				CenterTile.Index = _centerIndex;

				DefineOtherTilesBasedOnCenterTile();
			}
		}

		public void DefineOtherTilesBasedOnCenterTile()
		{
			if (CenterTile != null)
			{
				CenterTile.NorthWestNeighbour = _centerIndex - (_gridSqrtSize + 1);
				CenterTile.WestNeighbour = _centerIndex - _gridSqrtSize;
				CenterTile.SouthWestNeighbour = _centerIndex - (_gridSqrtSize - 1);
				CenterTile.NorthNeighbour = _centerIndex - 1;
				CenterTile.SouthNeighbour = _centerIndex + 1;
				CenterTile.NorthEastNeighbour = _centerIndex + (_gridSqrtSize - 1);
				CenterTile.EastNeighbour = _centerIndex + _gridSqrtSize;
				CenterTile.SouthEastNeighbour = _centerIndex + _gridSqrtSize + 1;
			}
		}

		public void CreateTilesByLayer(Tile pTileTemplate)
		{
			int tileIndex = 0;

			for (int t = 0; t < _config.totalTiles; t++)
			{
				Tile tile = Instantiate(pTileTemplate, transform);
				tile.gameObject.SetActive(false);
				tile.Index = tileIndex;
				tile.SetRenderingLayer(_config.layerOrder);

				AddTile(tile);

				tileIndex++;
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