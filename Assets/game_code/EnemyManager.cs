using UnityEngine;
using System.Collections.Generic;
using System;

public class EnemyManager : MonoBehaviour {

	public static EnemyManager instance;

	List<Enemy> enemies = new List<Enemy>();
	int ct = 0;

	void Start () {
		instance = this;
	}

	void Update () {
		ct++;
		if (ct%25==0) {
			generate_enemy();
		}

		for (int i = enemies.Count-1; i >= 0; i--) {
			Enemy b = enemies[i];
			b.update();

			List<Bullet> bullets = BulletManager.instance.bullets;
			bool hit = false;
			Vector3 enemy_pos = b.get_position();
			for (int j = bullets.Count-1; j >= 0; j--) {
				Vector3 bullet_pos = bullets[j].get_position();
				if (Util.vec_dist(enemy_pos,bullet_pos) < 1.5f) {
					hit = true;
					GameGUI.instance.score++;
					EffectManager.instance.add_effect(new Effect("Explosion",enemy_pos,100));
				}
			}

			if (hit || b.should_remove()) {
				enemies.RemoveAt(i);
				b.do_remove();
			}
		}

	}

	void generate_enemy() {
		Vector3 pos = new Vector3(
			Util.rand_range(GameControl.X_MIN,GameControl.X_MAX),
			Util.rand_range(GameControl.Y_MIN,GameControl.Y_MAX),
			75
		);
		GameObject enemy_object = (GameObject)Instantiate(Resources.Load("A10_red"));
		enemy_object.transform.parent = this.gameObject.transform;
		enemies.Add(new Enemy(pos,enemy_object));
	}
}
