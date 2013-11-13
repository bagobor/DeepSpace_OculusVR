using System;
using UnityEngine;


public class Util{

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


