using System;
using UnityEngine;

public class Bullet {

	Vector3 vel;
	GameObject obj;
	int ct;

	public Bullet (Vector3 pos, Vector3 _vel, GameObject _obj) {
		vel = _vel;
		obj = _obj;
		obj.transform.position = pos;
		ct = 100;
	}

	public void update() {
		Vector3 pos = obj.transform.position;
		pos.x += vel.x;
		pos.y += vel.y;
		pos.z += vel.z;
		obj.transform.position = pos;
		ct--;
	}

	public bool should_remove() {
		return ct <= 0;
	}

	public void do_remove() {
		GameObject.Destroy(obj);
		obj = null;
	}

	public Vector3 get_position() {
		return obj.transform.position;
	}
}

