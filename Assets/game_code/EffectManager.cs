using UnityEngine;
using System.Collections.Generic;

public class EffectManager : MonoBehaviour {

	public static EffectManager instance;

	public List<Effect> effects = new List<Effect>();

	void Start () {
		instance = this;
	}

	void Update () {
		for (int i = effects.Count-1; i >= 0; i--) {
			Effect b = effects[i];
			b.update();
			if (b.should_remove()) {
				effects.RemoveAt(i);
				b.do_remove();
			}
		}
	}

	public void add_effect(Effect e) {
		effects.Add(e);
	}
}
