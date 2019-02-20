using UnityEngine;

namespace OSM
{
	public class Tile : MonoBehaviour
	{
		[SerializeField]
		private TileData _tileData;		
		[SerializeField]
		private SpriteRenderer _renderer;
		[SerializeField]
		private MeshRenderer _meshRenderer;

		private Material _meshMaterial;
		private Vector3 _tileSize;

		public int NorthNeighbour;
		public int NorthEastNeighbour;
		public int EastNeighbour;
		public int SouthEastNeighbour;
		public int SouthNeighbour;
		public int SouthWestNeighbour;
		public int WestNeighbour;
		public int NorthWestNeighbour;

		public int Index
		{
			get { return _tileData.index; }
			set { _tileData.index = value; }
		}

		public int Zoom
		{
			get { return _tileData.zoom; }
			set { _tileData.zoom = value; }
		}

		public int X
		{
			get { return _tileData.x; }
			set { _tileData.x = value; }
		}

		public int Y
		{
			get { return _tileData.y; }
			set { _tileData.y = value; }
		}

		public string Name
		{
			get { return _tileData.name; }
			set { _tileData.name = value; }
		}

		public TileData TileData
		{
			get { return _tileData; }
			set { _tileData = value; }
		}

		public Vector3 TileSize
		{
			get
			{
				if(_tileSize != Vector3.zero)
				{
					return _tileSize;
				}
				if (_renderer != null)
				{
					_tileSize = _renderer.bounds.size;
				}
				if(_meshRenderer != null)
				{
					_tileSize = _meshRenderer.bounds.size;
				}

				return _tileSize;
			}
			set
			{
				_tileSize = value;
			}
		}

		private void Awake()
		{
			if (_renderer == null)
			{
				_renderer = GetComponent<SpriteRenderer>();
			}

			if(_renderer == null && _meshRenderer == null)
			{
				_meshRenderer = GetComponent<MeshRenderer>();
				_meshRenderer.material = new Material(Shader.Find("Sprites/Default"));
			}						
		}

		public void SetRenderingLayer(int layer)
		{
			if(_renderer != null)
			{
				_renderer.sortingOrder = layer;
			}
			if (_meshRenderer != null)
			{
				_meshRenderer.sortingOrder = layer;
			}
		}

		public void SetAlpha(float alpha)
		{
			if (_renderer != null)
			{
				Color c = _renderer.color;
				c.a = alpha;
				_renderer.color = c;
			}

			if(_meshRenderer != null)
			{
				Color c = _meshRenderer.material.color;
				c.a = alpha;
				_meshRenderer.material.color = c;
			}
		}

		public void SetTileTexture(Texture2D pTexture)
		{
			if (pTexture != null)
			{
				if (_renderer != null)
				{
					_renderer.sprite = Sprite.Create(pTexture, new Rect(0, 0, pTexture.width, pTexture.height), new Vector2(0.5f, 0.5f));
					gameObject.name = pTexture.name;
				}

				if (_meshRenderer != null && _meshRenderer.material != null)
				{
					_tileSize = transform.localScale = new Vector3(pTexture.width * 0.01f, pTexture.width * 0.01f, 1);

					_meshRenderer.material.name = gameObject.name = pTexture.name;
					_meshRenderer.material.mainTexture = pTexture;
					_meshRenderer.material.mainTexture.wrapMode = TextureWrapMode.Clamp;
					_meshRenderer.material.mainTexture.filterMode = FilterMode.Trilinear;
				}
			}
			else
			{
				if (_renderer != null)
				{
					_renderer.sprite = null;
					gameObject.name = pTexture.name;
				}

				if (_meshRenderer != null && _meshRenderer.material != null)
				{
					_meshRenderer.material.name = gameObject.name = "Tile" + Index;
					_meshRenderer.material.mainTexture = null;
				}
			}
		}
	}
}