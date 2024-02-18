using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveToParent : MonoBehaviour {

    public GameObject Child;
    public string Parent;

	// Use this for initialization
	void Start () {
        Child.transform.SetParent(GameObject.Find(Parent).transform, false);
    }
	
}
