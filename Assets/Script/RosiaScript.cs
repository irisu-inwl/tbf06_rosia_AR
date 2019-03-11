using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RosiaScript : MonoBehaviour {
	private Animator anim;
	private int state = 0;
	// Use this for initialization
	void Start () {
		anim = GetComponent<Animator>();
	}
	
	// Update is called once per frame
	void Update () {
		anim.SetInteger("state", state);
	}

	public void changeState(){
		state = (state + 1) % 2; //マジックナンバー…
	}
}
