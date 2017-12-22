using UnityEngine;
using System.Collections;
public class CItem : MonoBehaviour {
	/** アイテムが出現しているかフラグ*/
	public static bool exist {get; set;}
	private NetworkView netView = null;
	/** アイテムの取得処理の開始*/
	private bool isPicked = false;

	// Use this for initialization
	void Start () {
		netView = GetComponent<NetworkView>();
	}

	// Update is called once per frame
	void Update () {

	}

	void OnTriggerEnter(Collider col) {
		// 接続相手がプレイヤーの時に処理
		if (col.gameObject.CompareTag("Player")) {
			// プレイヤーのNetworkViewを取得
			NetworkView netplayer = col.gameObject.GetComponent<NetworkView>();
			// アイテムを自分が制御している時は、そのままアイテム取得処理を呼び出す
			if (netView.isMine) {
				PickupItem(netplayer.viewID);
			}
			else {
				// アイテムがネットワーク先の場合
				// アイテムの管理者をownerで指定して、RPC越しに呼び出す
				netView.RPC("PickupItem", netView.owner, netplayer.viewID);
			}
		}
	}

	[RPC]
	void PickupItem(NetworkViewID viewID) {
		// 処理を開始していたら処理しない
		if (isPicked) return;

		// 処理開始フラグを設定
		isPicked = true;

		// 取得したプレイヤーを検索
		NetworkView netplayer = NetworkView.Find(viewID);
		if (netplayer == null)	return;

		// 自分が制御している場合は、自分のネットにいるプレイヤーのアイテム取得関数を呼び出す
		if (netplayer.isMine) {
			netplayer.SendMessage("GetItem", 1);
		}
		else {
			// ネット先にいるプレイヤーのアイテム取得関数を呼び出す
			netplayer.RPC("GetItem", netplayer.owner, 1);
		}

		// 自分自身を破棄する
		Network.Destroy(gameObject);
		// RPCから自分を生成する命令を削除
		Network.RemoveRPCs(netView.viewID);
		// アイテムをいなくする
		exist = false;
	}
}