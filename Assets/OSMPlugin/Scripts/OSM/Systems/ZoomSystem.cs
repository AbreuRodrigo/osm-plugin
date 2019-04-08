using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OSM
{
	/*
	 *  ZOOM LEVELS  
	 *  zoom	scale	sum
	 *  3		x1		+0
	 *  4		x2		+1
	 *  5		x4		+2
	 *  6		x8		+3
	 *  7		x16		+4
	 *  8		x32		+5
	 *  9		x64		+6
	 *  10		x128	+7	
	 *  11		x256	+8
	 *  12		x512	+9
	 *  13		x1024	+10
	 *  14		x2048	+11
	 *  15		x4096	+12
	 *  16		x8192	+13
	 *  17		x16384	+14
	 *  18		x32768	+15
	 *  19		x65536	+16
	 */
	public class ZoomSystem
	{
		//Passing a sum will return the corresponding value from the scale column described above the class definition here
		public static float GetScaleBySum(int sum)
		{
			//works on power of 2
			return Mathf.Pow(2, sum);
		}

		//Passing a scale will return the corresponding value from the sum column described above the class definition here
		public static int GetSumByScale(float scale)
		{
			//works on log of base 2
			return (int) Mathf.Log(scale, 2);
		}

		/*
		 * scale	zoom	range
		 * 1		3		<= 1.5
		 * 2		4		<= 2.5
		 * 4		5		<= 4.5
		 * 8		6		<= 8.5
		 * 16		7		<= 16.5
		 * 32		8		<= 32.5
		 * 64		9		<= 64.5	
		 * 128		10		<= 128.5 ...
		 */
		//TODO:  Refactor to a less manual way of doing it
		public static ZoomFraction GetZoomScaleByObjectScale(float objectScale)
		{
			int zoom = 0;

			if (objectScale <= 1.5f)
			{
				zoom = 3;
			}
			else if (objectScale <= 2.5f)
			{
				zoom = 4;
			}
			else if (objectScale <= 4.5f)
			{
				zoom = 5;
			}
			else if (objectScale <= 8.5f)
			{
				zoom = 6;
			}
			else if (objectScale <= 16.5f)
			{
				zoom = 7;
			}
			else if (objectScale <= 32.5f)
			{
				zoom = 8;
			}
			else if (objectScale <= 64.5f)
			{
				zoom = 9;
			}
			else if (objectScale <= 128.5f)
			{
				zoom = 10;
			}
			else if (objectScale <= 256.5f)
			{
				zoom = 11;
			}
			else if (objectScale <= 512.5f)
			{
				zoom = 12;
			}
			else if (objectScale <= 1024.5f)
			{
				zoom = 13;
			}
			else if (objectScale <= 2048.5f)
			{
				zoom = 14;
			}
			else if (objectScale <= 4096.5f)
			{
				zoom = 15;
			}
			else if (objectScale <= 8192.5f)
			{
				zoom = 16;
			}
			else if (objectScale <= 16384.5f)
			{
				zoom = 17;
			}
			else if (objectScale <= 32768.5f)
			{
				zoom = 18;
			}
			else if (objectScale > 32768.5f)
			{
				zoom = 19;
			}

			//Obtain the zoom level by objectScale range, and transform the zoomLevel into zoomSum,
			//To convert to scale and find the zoomFraction
			float zoomScale = GetScaleBySum(zoom - Consts.MIN_ZOOM_LEVEL);
			return new ZoomFraction(zoom, zoomScale, objectScale);
		}
	}
}