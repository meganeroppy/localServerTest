using UnityEngine;
using System.Collections;
using System.Net;
public class NetConnector : MonoBehaviour
{
    // 自分のIPアドレス
    private string myIP = "";
    // 接続先のIPアドレス
    private string servIP = "";
    // 接続が完了したときtrue
    private bool isConnected = false;
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
            }
            // IPの編集
            servIP = GUI.TextField(new Rect(10, 50, 200, 30), servIP);
            // クライアントになるボタン
            if (GUI.Button(new Rect(10, 80, 200, 30), "上のゲームサーバーに接続"))
            {
            }
        }
    }
}