using UnityEngine;
using System.Collections;

public class Effect {

	GameObject obj;
	int ct;

	public Effect(string resc, Vector3 pos, int _ct) {
		obj = (GameObject)EffectManager.Instantiate(Resources.Load(resc));
		obj.transform.position = pos;
		ct = _ct;
		obj.transform.parent = EffectManager.instance.gameObject.transform;
	}

	public void update() {
		ct--;
	}

	public bool should_remove() {
		return ct <= 0;
	}

	public void do_remove() {
		GameObject.Destroy(obj);
		obj = null;
	}

}
