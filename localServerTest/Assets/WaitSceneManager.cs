﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WaitSceneManager : MonoBehaviour
{
    [SerializeField]
    private bool autoRetryToJoinHost = true;
    private float wait = 5f;
    private float timer = 0;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		if( Input.GetKeyDown( KeyCode.T ) )
        {
            SceneManager.LoadScene("LobbyPractce");
        }

        if( autoRetryToJoinHost )
        {
            timer += Time.deltaTime;
            if( timer >= wait )
            {
                SceneManager.LoadScene("LobbyPractce");
                autoRetryToJoinHost = false;
            }
        }
        
	}
}
