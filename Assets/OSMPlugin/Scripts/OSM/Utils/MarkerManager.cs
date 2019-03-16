using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OSM
{
	public class MarkerManager : MonoBehaviourSingleton<MarkerManager>
	{
		[SerializeField]
		private GameObject _pinPrefab;
		[SerializeField]
		private Camera _mainCamera;

		public void CreateMarker(Vector3 pPoint, Transform pParent)
		{
			Vector3 start = new Vector3(pPoint.x, _mainCamera.ScreenToWorldPoint(new Vector3(0, Screen.height, _mainCamera.transform.position.z * -1)).y * 3, 0);
			GameObject instance = Instantiate(_pinPrefab, start, Quaternion.identity, pParent) as GameObject;

			TweenManager.Instance.FallDownAndSquish(instance, 0.25f, pPoint, null);
		}
	}
}