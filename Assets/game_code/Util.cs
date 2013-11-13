using System;
using UnityEngine;


public class Util{

	public static System.Random rand = new System.Random();

	public static float rand_range(float min, float max) {
		float r = (float)rand.NextDouble();
		return (max-min)*r + min;
	}

	public static float vec_dist(Vector3 a, Vector3 b) {
		return (float)Math.Abs(Math.Sqrt(Math.Pow(a.x-b.x,2)+Math.Pow(a.y-b.y,2)+Math.Pow(a.z-b.z,2)));
	}

	public static GameObject FindInHierarchy(GameObject root, string name)
	{
		if (root == null || root.name == name)
		{
			return root;
		}

		Transform child = root.transform.Find(name);
		if (child != null)
		{
			return child.gameObject;
		}

		int numChildren = root.transform.childCount;
		for (int i = 0; i < numChildren; i++)
		{
			GameObject go = FindInHierarchy(root.transform.GetChild(i).gameObject, name);
			if (go != null)
			{
				return go;
			}
		}

		return null;
	}

}


