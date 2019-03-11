using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraRaycast : MonoBehaviour {
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		if (Input.GetMouseButtonDown(0)) 
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit = new RaycastHit();
            if (Physics.Raycast(ray, out hit, 1000)) {
				// raycastに当たったオブジェクトの名前を取得
                string objectName = hit.collider.gameObject.name;
				// ロージアちゃんが得る
                RosiaScript rosia =  GameObject.Find(objectName).GetComponent<RosiaScript>();
                if (rosia != null){
                    rosia.changeState();
                }
            }
        }
	}
}
