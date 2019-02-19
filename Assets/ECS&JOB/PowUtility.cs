using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PowUtility {

	public static float IntPow(float value, int val)
	{
		float result = value;
		for(int i = 0 ; i < val-1 ; i++)
		{
			result *= value;
		}
		return result;
	}

}
