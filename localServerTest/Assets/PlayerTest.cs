using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class PlayerTest : MonoBehaviour {
	private Rigidbody myRigidbody;
	public float fSpeed = 10f;
	private NetworkView netView = null;
	public int iItemCount = 0;
	private TextMesh textItem = null;
	private NetworkTransform nTransform = null;
	private NetworkIdentity nIdentity = null;

	// Use this for initialization
	void Start () {
		myRigidbody = GetComponent<Rigidbody>();
		netView = GetComponent<NetworkView>();
		textItem = GetComponentInChildren<TextMesh>();
		nTransform = GetComponent<NetworkTransform>();
		nIdentity = GetComponent<NetworkIdentity>();
	}

	// Update is called once per frame
	void Update () {
	//	textItem.text = ""+iItemCount;

	//	if (!netView.isMine)
	//	{
	//		return;
	//	}


		if( !nTransform.isLocalPlayer )
		{
			return;
		}
		Vector3 move = new Vector3(
			Input.GetAxisRaw("Horizontal"),
            0,
			Input.GetAxisRaw("Vertical"));
		myRigidbody.velocity = fSpeed * move.normalized;
	}

	[RPC]//
	void GetItem(int add) {
		iItemCount += add;
		netView.RPC("SetItemCount", RPCMode.OthersBuffered, iItemCount);
	}

	[RPC]
//	[Command]
	void SetItemCount(int ic) {
		iItemCount = ic;
	}
}