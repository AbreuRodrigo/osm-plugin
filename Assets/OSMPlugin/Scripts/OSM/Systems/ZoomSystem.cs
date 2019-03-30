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
	}
}