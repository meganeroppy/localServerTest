using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BaseSceneManager : MonoBehaviour
{
    public static BaseSceneManager instance;

    public string firstSceneName;

    [SerializeField]
    private Camera[] presetCameras;

	// Use this for initialization
	void Start ()
    {
        instance = this;
        SceneManager.LoadScene(firstSceneName, LoadSceneMode.Additive);
	}

    public void ActivatePresetCameras()
    {
        for( int i = 0 ; i < presetCameras.Length; ++i )
        {
            presetCameras[i].enabled = true;
            presetCameras[i].targetDisplay = i+1;
        }
    }
}
