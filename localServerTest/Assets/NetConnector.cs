using UnityEngine;
using System.Collections;
using System.Net;
public class NetConnector : MonoBehaviour
{
	public string useThisIp = "";
    // 自分のIPアドレス
    private string myIP = "";
    // 接続先のIPアドレス
    private string servIP = "";
    // 接続が完了したときtrue
    private bool isConnected = false;

	public GameObject prefPlayer = null;

    // Use this for initialization
    void Start()
    {
        string hostname = Dns.GetHostName();
        // ホスト名からIPアドレスを取得
        IPAddress[] adrList = Dns.GetHostAddresses(hostname);
        foreach (IPAddress address in adrList)
        {
            myIP = address.ToString();
            servIP = myIP;
			if(!string.IsNullOrEmpty(useThisIp))
			{
				servIP = useThisIp;
			}
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
    void OnGUI()
    {
        // 未接続のとき、接続用UIを表示
        if (!isConnected)
        {
            // ゲームサーバーになるボタン
            if (GUI.Button(new Rect(10, 10, 200, 30), "ゲームサーバーになる"))
            {
				var result = Network.InitializeServer(20, 25000, false);
				if ( result == NetworkConnectionError.NoError)
				{
					procConnect();
				}
				else
				{
					Debug.Log("ゲームサーバー初期化エラー");
				}
            }
            // IPの編集
            servIP = GUI.TextField(new Rect(10, 50, 200, 30), servIP);
            // クライアントになるボタン
            if (GUI.Button(new Rect(10, 80, 200, 30), "上のゲームサーバーに接続"))
            {
				var result = Network.Connect(servIP, 25000);

				if (result == NetworkConnectionError.NoError)
				{
					procConnect();
				}
				else
				{
					Debug.Log("接続エラー");
				}
            }
        }
    }

	private void procConnect()
	{
		isConnected = true;
		// プレイヤーを出現させる
		StartCoroutine("instantiatePlayer");
	}
	private IEnumerator instantiatePlayer()
	{
		// 接続が完了するまで待つ
		while (!Network.isServer && !Network.isClient)
		{
			yield return null;
		}
		// 接続したので出場
		Vector3 pos = new Vector3(Random.Range(-5f, 5f), Random.Range(-2.5f, 2.5f));
		Network.Instantiate(prefPlayer, pos, Quaternion.identity, 0);
	}
}