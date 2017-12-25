using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrothySample : MonoBehaviour {

    private Transform owner;
	public void SetOwner( Transform owner )
    {
        this.owner = owner;
    }
	
	// Update is called once per frame
	void Update ()
    {
	    if( owner != null )
        {
            transform.SetPositionAndRotation(owner.position, owner.rotation);
        }
	}
}
