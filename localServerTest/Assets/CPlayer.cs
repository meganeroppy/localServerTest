using UnityEngine;
using System.Collections;
public class CPlayer : MonoBehaviour {
	private Rigidbody myRigidbody;
	public float fSpeed = 10f;
	// Use this for initialization
	void Start () {
		myRigidbody = GetComponent<Rigidbody>();
	}

	// Update is called once per frame
	void Update () {
		Vector3 move = new Vector3(
			Input.GetAxisRaw("Horizontal"),
			Input.GetAxisRaw("Vertical"));
		myRigidbody.velocity = fSpeed * move.normalized;
	}
}