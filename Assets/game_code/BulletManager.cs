using UnityEngine;
using System.Collections.Generic;

public class BulletManager : MonoBehaviour {

	public static BulletManager instance;

	List<Bullet> bullets = new List<Bullet>();

	void Start () {
		instance = this;
	}

	void Update () {
		for (int i = bullets.Count-1; i >= 0; i--) {
			Bullet b = bullets[i];
			b.update();
			if (b.should_remove()) {
				bullets.RemoveAt(i);
				b.do_remove();
			}
		}
	}

	public void AddBullet(Vector3 pos, Vector3 vel) {
		GameObject bullet_object = (GameObject)Instantiate(Resources.Load("Bullet"));
		bullets.Add(new Bullet(pos,vel,bullet_object));
	}

}
