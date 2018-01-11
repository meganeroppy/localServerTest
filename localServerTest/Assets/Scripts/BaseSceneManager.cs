using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BaseSceneManager : MonoBehaviour {

    public string firstSceneName;

	// Use this for initialization
	void Start () {
        SceneManager.LoadScene(firstSceneName, LoadSceneMode.Additive);
	}
	
}
