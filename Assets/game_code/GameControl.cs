using UnityEngine;
using System.Collections;
using System;

public class GameControl : MonoBehaviour {
		
	static float X_MAX = 25;
	static float X_MIN = -25;
	static float Y_MAX = 25;
	static float Y_MIN = -25;
	static float VX_MAX = 1;
	static float VY_MAX = 1;

	float vx = 0;
	float vy = 0;
	float rotation_z = 0;
	float rotation_x = 0;

	float gun_cooldown = 0;
	int gun_ct;

	GameObject crosshair_gui;

	GameObject left_gun_anchor;
	GameObject right_gun_anchor;
	GameObject player_anchor;

	void Start () {
		crosshair_gui = Util.FindInHierarchy(this.gameObject,"crosshair");
		player_anchor = Util.FindInHierarchy(this.gameObject,"OVRCameraController");
		left_gun_anchor = Util.FindInHierarchy(this.gameObject,"MissileAnchor_L");
		right_gun_anchor = Util.FindInHierarchy(this.gameObject,"MissileAnchor_R");
	}

	void Update () {
		Screen.showCursor = false;
		Vector3 position = this.gameObject.transform.position;
		Quaternion rotation_q = this.gameObject.transform.rotation;
		Vector3 rotation = rotation_q.eulerAngles;

		if (Input.GetKey(KeyCode.A)) {
			if (vx > 0) vx = 0;
			vx = Math.Max(-VX_MAX,vx - 0.01f);
			
		} else if (Input.GetKey(KeyCode.D)) {
			if (vx < 0) vx = 0;
			vx = Math.Min(VX_MAX,vx + 0.01f);

		} else {
			vx *= 0.95f;
		}

		if (Input.GetKey(KeyCode.S)) {
			if (vy > 0) vy = 0;
			vy = Math.Max(-VY_MAX,vy - 0.01f);

		} else if (Input.GetKey(KeyCode.W)) {
			if (vy < 0) vy = 0;
			vy = Math.Min(VY_MAX,vy + 0.01f);

		} else {
			vy *= 0.95f;
		}

		position.x = Mathf.Clamp(position.x+vx,X_MIN,X_MAX);
		position.y = Mathf.Clamp(position.y+vy,Y_MIN,Y_MAX);
		float target_rotation_z = 20.0f * -1 * vx;
		rotation_z = rotation_z + (target_rotation_z - rotation_z)/10.0f;
		float target_rotation_x = 20.0f * -1 * vy;
		rotation_x = rotation_x + (target_rotation_x - rotation_x)/10.0f;
		rotation.z = rotation_z;
		rotation.x = rotation_x;

		rotation_q.eulerAngles = rotation;
		this.gameObject.transform.rotation = rotation_q;
		this.gameObject.transform.position = position;

		float mousex = Input.mousePosition.x > Screen.width ? Screen.width : Input.mousePosition.x;
		float mousey = Input.mousePosition.y > Screen.height ? Screen.height : Input.mousePosition.y;

		Vector3 crosshair_position = crosshair_gui.transform.localPosition;
		float screen_scale = 0.13f;
		crosshair_position.x = (mousex - Screen.width/2.0f) / (Screen.width/2.0f) * screen_scale;
		crosshair_position.y = (mousey - Screen.height/2.0f) / (Screen.height/2.0f) * screen_scale * 0.86f + 0.08f;

		crosshair_gui.transform.localPosition = crosshair_position;

		if (gun_cooldown > 0) {
			gun_cooldown-=1;
		}
		if (Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0)) {
			if (gun_cooldown <= 0) {
				gun_ct++;

				if (gun_ct%4 == 0) {
					gun_cooldown = 20;
				} else {
					gun_cooldown = 6;
				}

				Vector3 cursor_position = crosshair_gui.transform.position;
				if (Math.Abs(position.x) < X_MAX) cursor_position.x += vx;
				if (Math.Abs(position.y) < Y_MAX) cursor_position.y += vy;
				Vector3 player_position = player_anchor.transform.position;
				Vector3 npc_dir = (new Vector3(
					cursor_position.x-player_position.x,
					cursor_position.y-player_position.y,
					cursor_position.z-player_position.z)).normalized;

				npc_dir.x *= 20;
				npc_dir.y *= 20;
				npc_dir.z *= 20;

				Vector3 convergence_pos = new Vector3(npc_dir.x + player_position.x,
				                                      npc_dir.y+player_position.y,
				                                      npc_dir.z+player_position.z);

				Vector3 lanchor_position = left_gun_anchor.transform.position;
				if (Math.Abs(position.x) < X_MAX) lanchor_position.x += vx;
				if (Math.Abs(position.y) < Y_MAX)lanchor_position.y += vy;
				Vector3 lbullet_vel = (new Vector3(convergence_pos.x-lanchor_position.x, 
				                                   convergence_pos.y-lanchor_position.y, 
				                                   convergence_pos.z-lanchor_position.z)).normalized;
				lbullet_vel.x *= 0.25f;
				lbullet_vel.y *= 0.25f;
				lbullet_vel.z *= 0.25f;
				if (Math.Abs(position.x) < X_MAX) lbullet_vel.x += vx;
				if (Math.Abs(position.y) < Y_MAX)lbullet_vel.y += vy;
				BulletManager.instance.AddBullet(
					lanchor_position,
					lbullet_vel
				);

				Vector3 ranchor_position = right_gun_anchor.transform.position;
				if (Math.Abs(position.x) < X_MAX) ranchor_position.x += vx;
				if (Math.Abs(position.y) < Y_MAX)ranchor_position.y += vy;
				Vector3 rbullet_vel = (new Vector3(convergence_pos.x-ranchor_position.x, 
				                                   convergence_pos.y-ranchor_position.y, 
				                                   convergence_pos.z-ranchor_position.z)).normalized;
				rbullet_vel.x *= 0.25f;
				rbullet_vel.y *= 0.25f;
				rbullet_vel.z *= 0.25f;
				if (Math.Abs(position.x) < X_MAX) rbullet_vel.x += vx;
				if (Math.Abs(position.y) < Y_MAX) rbullet_vel.y += vy;
				BulletManager.instance.AddBullet(
					ranchor_position,
					rbullet_vel
				);
			}
		}

	}
}
