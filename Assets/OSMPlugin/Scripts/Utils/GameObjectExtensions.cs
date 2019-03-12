using UnityEngine;

public static class GameObjectExtensions
{
	public static T AddMissingComponent<T>(this GameObject go) where T : Component
	{
		T result = go.GetComponent<T>();
		if (result == null)
		{
			result = go.AddComponent<T>();
		}
		return result;
	}
}