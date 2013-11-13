using UnityEngine;
using System.Collections;

public class Enemy {

	Vector3 pos;
	float vel;
	GameObject gameobj;

	public Enemy(Vector3 _pos, GameObject _gameobj) {
		pos = _pos;
		gameobj = _gameobj;
		gameobj.transform.position = pos;
		vel = Util.rand_range(0.15f,0.5f);
	}

	public void update() {
		pos.z -= vel;
		gameobj.transform.position = pos;
	}

	public Vector3 get_position() {
		return pos;
	}

	public bool should_remove() {
		return pos.z < -40;
	}

	public void do_remove() {
		GameObject.Destroy(gameobj);
		gameobj = null;
	}

}
