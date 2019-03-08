using System;
using UnityEngine;

namespace OSM
{
	[Serializable]
	public struct ScreenBoundaries
	{
		private Vector3 _updateVector;
		private Vector3 _maxScreenLimit;

		public float left;
		public float top;
		public float right;
		public float bottom;

		public ScreenBoundaries(Camera pCamera)
		{
			_updateVector.x = Screen.width;
			_updateVector.y = Screen.height;
			_updateVector.z = 1;

			_maxScreenLimit = pCamera.ScreenToWorldPoint(_updateVector) * 10;
			left = -_maxScreenLimit.x;
			top = _maxScreenLimit.y;
			right = _maxScreenLimit.x;
			bottom = -_maxScreenLimit.y;
		}
	}
}
