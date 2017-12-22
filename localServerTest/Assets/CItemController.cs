using UnityEngine;
using System.Collections;
public class CItemController : MonoBehaviour {
	/** アイテムのプレハブを設定する*/
	public GameObject prefItem = null;
	/** アイテムを出現させる横範囲*/
	public float RAND_WIDTH = 6f;
	/** アイテムを出現させる縦範囲*/
	public float RAND_HEIGHT = 5f;
	void Start()
	{
		// アイテムの出現フラグをクリアしておく
		CItem.exist = false;
	}
	void Update()
	{
		// サーバー時のみ処理
		if (Network.isServer)
		{
			if (!CItem.exist)
			{
				Vector3 pos = new Vector3(Random.Range(-RAND_WIDTH, RAND_WIDTH), Random.Range(-RAND_HEIGHT, RAND_HEIGHT), 0f);
				Network.Instantiate(prefItem, pos, Quaternion.identity, 0);
				CItem.exist = true;
			}
		}
	}
}