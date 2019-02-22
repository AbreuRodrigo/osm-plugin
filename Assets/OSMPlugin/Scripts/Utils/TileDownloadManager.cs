using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace OSM
{
	public class TileDownloadManager : MonoBehaviour
	{
		private const string MAP_TILE_BASE_URI = "http://a.tile.openstreetmap.org/";

		public Text error;

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

		#endregion

		#region Methods

		public void DownloadTileImage(string pTileName, Action<Texture2D> pOnCompleteDownloading)
		{
			StartCoroutine(DownloadTileByName(pTileName, pOnCompleteDownloading));
		}

		private IEnumerator DownloadTileByName(string pTileName, Action<Texture2D> pOnCompleteDownloading)
		{
			Texture2D texture = null;

			//Getting the texture from the cached textures
			if (_textures != null)
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
						error.text = request.error;
						pOnCompleteDownloading?.Invoke(null);
					}
					else
					{
						texture = DownloadHandlerTexture.GetContent(request);

						if (texture != null && _textures != null)
						{
							texture.name = pTileName;

							_textures[pTileName] = texture;

							pOnCompleteDownloading?.Invoke(texture);
						}
					}
				}
			}
		}

		#endregion
	}
}