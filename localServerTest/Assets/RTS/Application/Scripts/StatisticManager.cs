using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

public  class StatisticManager : MonoBehaviour {

    public bool  isStatistic = true;
    public string StatisticServerURL;
    public string franchiseeKey;
    public string franchiseeValue;
    public  Dictionary<string, string> Franchisee = new Dictionary<string, string>();

    private int statisticTimeRate = 60;
    public bool debug ;
    public int StatisticTimeRate { get { return statisticTimeRate; } }
    private string statisticSucced = "云服务器请求成功，可以正常游戏";
    public string StatisticSucced { get { return statisticSucced; } }
    private string statisticFailed = "云服务器请求失败，请检查网络";
    public string StatisticFailed { get { return statisticFailed; } }
    [HideInInspector]
    public float timeDown;
    private bool allowNextRequest = false;
    private bool isAllowGame = true;
    public bool IsAllowGame
    {
        get { return isAllowGame; }
    }
    private static StatisticManager instance=null;
    public static StatisticManager Instance { get { return instance; } }
    ///URL:http://statistic.realis-e.com:8083/open/rpg/get
    ///Franchisee.Add("DevTest", "dev000");
    // Use this for initialization
    void Awake()
    {
        
        instance = this;
    }
    void Start () {
        
        if(isStatistic)
        {
            timeDown = Time.time + StatisticTimeRate;
            Franchisee.Add(franchiseeKey, franchiseeValue);
            StartStatistic();
        }
    }
	
	// Update is called once per frame
	void Update () {
        // Debug.Log(ConvertToUnixTimestamp(DateTime.UtcNow));
            OpenGame();
    }

    void OpenGame()
    {
        if (!isStatistic)
            return;
        if (IsAllowGame)
            return;
        if (!allowNextRequest)
            return;
        
        if (Franchisee.ContainsKey(franchiseeKey))
        {
            // StatisticServerURL += 
            // token time franchisee
            var api = StatisticServerURL + "?";

            var now = ConvertToUnixTimestamp(DateTime.UtcNow);
            api += "t=" + now;

            var sum = Md5Sum(Franchisee[franchiseeKey] + now);
            api += "&k=" + sum;

            api += "&f=" + franchiseeKey;

            WWW www = new WWW(api);
            
            allowNextRequest = false;
            Debug.Log(api);
            StartCoroutine(WaitForRequest(www));
        }
        else
        {
            //TODO
        }
    }

    class StatisticRetData
    {
        public string msg;
        public int ret;
    }

    IEnumerator WaitForRequest(WWW www)
    {
        yield return www;
        
        // check for errors
        if (www.error == null)
        {
            // Debug.Log("WWW Ok!: " + www.text);
            var jsonRet = JsonUtility.FromJson<StatisticRetData>(www.text);
            // Debug.Log(jsonRet.msg);
            if (jsonRet.ret != 0)
            {
                allowNextRequest = true;
                isAllowGame = false;
            }
            else
            {
                allowNextRequest = false;
                isAllowGame = true;
            }
        }
        else
        {
            Debug.Log("WWW Error: " + www.error);
            allowNextRequest = true;
            isAllowGame = false;
        }

    }
   
    public string Md5Sum(string strToEncrypt)
    {
        System.Text.UTF8Encoding ue = new System.Text.UTF8Encoding();
        byte[] bytes = ue.GetBytes(strToEncrypt);

        // encrypt bytes
        System.Security.Cryptography.MD5CryptoServiceProvider md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
        byte[] hashBytes = md5.ComputeHash(bytes);

        // Convert the encrypted bytes back to a string (base 16)
        string hashString = "";

        for (int i = 0; i < hashBytes.Length; i++)
        {
            hashString += System.Convert.ToString(hashBytes[i], 16).PadLeft(2, '0');
        }

        return hashString.PadLeft(32, '0');
    }

   
   
    double ConvertToUnixTimestamp(DateTime date)
    {
        DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
        TimeSpan diff = date - origin;
        return Math.Floor(diff.TotalSeconds);
    }
    //开始统计倒计时
    private IEnumerator StatisticCountDown()
    {
        allowNextRequest = false;
        isAllowGame = true;
        yield return new WaitForSeconds(StatisticTimeRate);
        isAllowGame = false;
        allowNextRequest = true;
    }
    //开始统计
    public void StartStatistic()
    {
        StopCoroutine("StatisticCountDown");
        StartCoroutine("StatisticCountDown");
    }

    void OnGUI()
    {
	    if (!isStatistic)
            return;
        if (!debug)
            return;
        if (timeDown - Time.time >= 0)
        {
            GUILayout.Label((int)(timeDown - Time.time) + "秒后开始统计");
        }
        else
            GUILayout.Label(IsAllowGame ? StatisticSucced : StatisticFailed);
    }
}
