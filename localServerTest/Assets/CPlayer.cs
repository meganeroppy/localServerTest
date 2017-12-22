using UnityEngine;
using System.Collections;
public class CPlayer : MonoBehaviour {
	private Rigidbody myRigidbody;
	public float fSpeed = 10f;
	private NetworkView netView = null;
	public int iItemCount = 0;
	private TextMesh textItem = null;

	// Use this for initialization
	void Start () {
		myRigidbody = GetComponent<Rigidbody>();
		netView = GetComponent<NetworkView>();
		textItem = GetComponentInChildren<TextMesh>();
	}

	// Update is called once per frame
	void Update () {
		textItem.text = ""+iItemCount;

		if (!netView.isMine)
		{
			return;
		}
		Vector3 move = new Vector3(
			Input.GetAxisRaw("Horizontal"),
			Input.GetAxisRaw("Vertical"));
		myRigidbody.velocity = fSpeed * move.normalized;
	}

	[RPC]
	void GetItem(int add) {
		iItemCount += add;
		netView.RPC("SetItemCount", RPCMode.OthersBuffered, iItemCount);
	}

	[RPC]
	void SetItemCount(int ic) {
		iItemCount = ic;
	}
}