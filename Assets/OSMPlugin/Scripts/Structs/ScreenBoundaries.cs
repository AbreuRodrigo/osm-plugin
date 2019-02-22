using System;
using UnityEngine;

namespace OSM
{
	[Serializable]
	public struct ScreenBoundaries
	{
		public float left;
		public float top;
		public float right;
		public float bottom;

		public ScreenBoundaries(Camera camera)
		{
			Vector3 maxScreenLimit = camera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, 1)) * 10;
			this.left = -maxScreenLimit.x;
			this.top = maxScreenLimit.y;
			this.right = maxScreenLimit.x;
			this.bottom = -maxScreenLimit.y;
		}
	}
}
