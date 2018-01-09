using UnityEngine;
using System.Collections;
public class CharaSynchronizer : MonoBehaviour {
	// 受信した位置
	Vector3 position;
	// 受信した回転
	Quaternion rotation;
	// Rigidbodyのインスタンス
	Rigidbody myRigidbody;
	// NetworkViewのインスタンス
	NetworkView netView;
	// Use this for initialization
	void Start () {
		myRigidbody = GetComponent<Rigidbody>();
		netView = GetComponent<NetworkView>();
		position = transform.position;
		rotation = transform.rotation;
	}

	// Update is called once per frame
	void FixedUpdate () {
		// 自分の制御キャラクター以外のとき、positionとrotationを反映させる
		if (!netView.isMine)
		{
			// データは1/Network.sendRate間隔で送信されてくる。このうちの経過時間分が内分する値
			float t = Time.fixedDeltaTime * Network.sendRate;
			// 移動先から速度を逆算
			Vector3 move = (Vector3.Lerp(transform.position, position, t)-transform.position)/Time.fixedDeltaTime;
			// 速度を設定
			myRigidbody.velocity = move;
			// 回転
			transform.rotation = Quaternion.Slerp(transform.rotation, rotation, t);
		}
	}
	void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
	{
		if (stream.isWriting)
		{
			Vector3 pos = transform.position;
			Quaternion rot = transform.rotation;
			// 送信
			stream.Serialize(ref pos);
			stream.Serialize(ref rot);
		}
		else
		{
			// 受信
			stream.Serialize(ref position);
			stream.Serialize(ref rotation);
		}
	}
}