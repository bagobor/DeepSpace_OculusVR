using UnityEngine;
using System.Collections;

public class GameGUI : MonoBehaviour {

	public static GameGUI instance;

	TextMesh text_hp;
	TextMesh text_score;

	GameObject health_bar_image;
	tk2dSprite health_bar_sprite;

	public int score = 0;

	void Start () {
		instance = this;
		text_hp = Util.FindInHierarchy(this.gameObject,"HP").GetComponent<TextMesh>();
		text_score = Util.FindInHierarchy(this.gameObject,"ScoreText").GetComponent<TextMesh>();
		health_bar_image = Util.FindInHierarchy(this.gameObject,"HealthBarImage");
		health_bar_sprite = health_bar_image.GetComponent<tk2dSprite>();
	}
	
	void Update () {
		/*Vector3 hbi_sc = health_bar_image.transform.localScale;
		hbi_sc.x = 0.25f;
		health_bar_image.transform.localScale = hbi_sc;

		text_hp.color = Color.red;
		text_score.color = Color.red;
		health_bar_sprite.color = Color.red;*/

		text_score.text = "Score: "+score;
	}
}
