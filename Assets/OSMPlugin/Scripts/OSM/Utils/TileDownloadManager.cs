using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace OSM
{
	public class TileDownloadManager : MonoBehaviour
	{
		private const string MAP_TILE_BASE_URI = "http://c.tile.openstreetmap.org/";

		[SerializeField]
		private bool _cacheAllTexturesAfterDownloading;

		[SerializeField]
		private float _clearUnusedTexturesEachXSeconds = 5;
		private float _clearTextureTime = 0;

		#region Properties
		private static TileDownloadManager instance;
		public static TileDownloadManager Instance
		{
			get
			{
				if(instance == null)
				{
					instance = FindObjectOfType<TileDownloadManager>();
				}

				return instance;
			}
		}

		private Dictionary<string, Texture2D> _textures = new Dictionary<string, Texture2D>();

		#endregion

		#region Monobehaviour

		private void Awake()
		{
			instance = this;
		}

		private void LateUpdate()
		{

			if (_clearTextureTime >= _clearUnusedTexturesEachXSeconds)
			{
				Resources.UnloadUnusedAssets();
				_clearTextureTime = 0;
			}

			_clearTextureTime += Time.deltaTime;
		}

		#endregion

		#region Methods

		public void DownloadTileImage(string pTileName, Action<Texture2D> pOnCompleteDownloading, float delay = 0)
		{
			StartCoroutine(DownloadTileByName(pTileName, pOnCompleteDownloading, delay));
		}

		private IEnumerator DownloadTileByName(string pTileName, Action<Texture2D> pOnCompleteDownloading, float delay = 0)
		{
			yield return new WaitForSeconds(delay);

			Texture2D texture = null;

			//Getting the texture from the cached textures
			if (_textures != null && _cacheAllTexturesAfterDownloading)
			{
				_textures.TryGetValue(pTileName, out texture);

				if (texture != null)
				{
					texture.name = pTileName;
					pOnCompleteDownloading?.Invoke(texture);
				}
			}

			//Will download the texture
			if (texture == null)
			{
				using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(MAP_TILE_BASE_URI + "/" + pTileName, false))
				{
					yield return request.SendWebRequest();

					if (request.isNetworkError || request.isHttpError)
					{
						pOnCompleteDownloading?.Invoke(null);
					}
					else
					{
						texture = DownloadHandlerTexture.GetContent(request);
						texture.name = pTileName;

						if (_cacheAllTexturesAfterDownloading && texture != null && _textures != null)
						{							
							_textures[pTileName] = texture;
						}

						pOnCompleteDownloading?.Invoke(texture);
					}
				}
			}
		}

		#endregion
	}
}