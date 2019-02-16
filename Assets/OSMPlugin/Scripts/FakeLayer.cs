using UnityEngine;

namespace OSM
{
	public class FakeLayer : MonoBehaviour
	{
		public Tile tile1;
		public Tile tile2;
		public Tile tile3;
		public Tile tile4;

		// Use this for initialization
		void Start()
		{
			tile1.SetRenderingLayer(1);
			tile1.SetRenderingLayer(1);
			tile1.SetRenderingLayer(1);
			tile1.SetRenderingLayer(1);

			TileDownloadManager.Instance.DownloadTileImageByTileName(tile1.TileData.name,
				(Texture2D texture) => {
					tile1.SetTileTexture(texture);
				}
			);
			TileDownloadManager.Instance.DownloadTileImageByTileName(tile2.TileData.name,
				(Texture2D texture) => {
					tile2.SetTileTexture(texture);
				}
			);
			TileDownloadManager.Instance.DownloadTileImageByTileName(tile3.TileData.name,
				(Texture2D texture) => {
					tile3.SetTileTexture(texture);
				}
			);
			TileDownloadManager.Instance.DownloadTileImageByTileName(tile4.TileData.name,
				(Texture2D texture) => {
					tile4.SetTileTexture(texture);
				}
			);
		}

		public void SetScaler(float scaler)
		{
			transform.localScale *= scaler;
		}
	}
}