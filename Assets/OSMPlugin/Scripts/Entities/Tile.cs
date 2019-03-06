using UnityEngine;

namespace OSM
{
	public class Tile : MonoBehaviour, IDownloadable
	{
		[SerializeField]
		private TileData _tileData;
		[SerializeField]
		public MeshRenderer _meshRenderer;

		private Material _meshMaterial;
		private Vector3 _tileSize;

		public int Index
		{
			get { return _tileData.index; }
			set
			{
				if (_tileData.index != value)
				{
					//Debug.Log("Tile Name: " + Name + " Was: " + _tileData.index + " Will be: " + value);
				}

				_tileData.index = value;
			}
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
			set
			{
				_tileData.name = value;
			}
		}

		public TileData TileData
		{
			get { return _tileData; }
			set
			{
				_tileData = value;
			}
		}

		public bool TextureUpToDate
		{
			get
			{
				return _tileData.name.Equals(_meshRenderer.name);
			}
		}

		public Vector3 TileSize
		{
			get
			{
				if (_tileSize != Vector3.zero)
				{
					return _tileSize;
				}
				if (_meshRenderer != null)
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
			if (_meshRenderer == null)
			{
				_meshRenderer = GetComponent<MeshRenderer>();
				_meshRenderer.material = new Material(Shader.Find("Sprites/Default"));
			}
		}

		public void ClearTexture()
		{
			if (_meshRenderer != null)
			{
				_meshRenderer.material.mainTexture = null;
			}
		}

		public void SetRenderingLayer(int layer)
		{
			if (_meshRenderer != null)
			{
				_meshRenderer.sortingOrder = layer;
			}
		}

		public void SetAlpha(float alpha)
		{
			if(_meshRenderer != null)
			{
				Color c = _meshRenderer.material.color;
				c.a = alpha;
				_meshRenderer.material.color = c;
			}
		}

		public void FadeOut(float pDuration)
		{
			TweenManager.Instance.ValueTransition(1, 0, pDuration, TweenType.Linear, true, null, SetAlpha);
		}

		public void FadeIn(float pDuration)
		{
			TweenManager.Instance.ValueTransition(0, 1, pDuration, TweenType.Linear, true, null, SetAlpha);
		}

		public void SetTileTexture(Texture2D pTexture)
		{			
			if (pTexture != null)
			{
				if (_meshRenderer != null)
				{
					SetAlpha(0);

					_tileSize = transform.localScale = new Vector3(pTexture.width * 0.01f, pTexture.width * 0.01f, 1);

					_meshRenderer.name = _meshRenderer.material.name = gameObject.name = pTexture.name;
					_meshRenderer.material.mainTexture = pTexture;										
					_meshRenderer.material.mainTexture.wrapMode = TextureWrapMode.Clamp;
					_meshRenderer.material.mainTexture.filterMode = FilterMode.Trilinear;

					FadeIn(0.5f);
				}
			}
			else
			{
				if (_meshRenderer != null)
				{
					_meshRenderer.material.name = gameObject.name = "Tile" + Index;
					_meshRenderer.material.mainTexture = null;
				}
			}					
		}

		public void OnEnterScreen()
		{
			Debug.Log("Enter Screen");
		}

		public void OnExitScreen()
		{
			Debug.Log("Exit Screen");
		}
	}
}