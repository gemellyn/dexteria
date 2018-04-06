using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadScene : MonoBehaviour {

    public InputField PlayerNameText;

    public void Start()
    {
        GameObject pm = GameObject.Find("PlayerManager");
        if (pm)
        {
            Debug.Log("Auto set name "+ pm.GetComponent<PlayerManager>().PlayerName);
            PlayerNameText.text = pm.GetComponent<PlayerManager>().PlayerName;
        }
         
    }

    public void loadSerpentSceneAndSavePlayerName()
    {
        if (PlayerNameText.text.Length > 1) 
        {
            GameObject pm = GameObject.Find("PlayerManager");
            if (pm)
            {
                pm.GetComponent<PlayerManager>().PlayerName = PlayerNameText.text;
            }
            
            SceneManager.LoadScene(1);
        }
    }

    public void loadDanseSceneAndSavePlayerName()
    {
        if (PlayerNameText.text.Length > 1)
        {
            GameObject pm = GameObject.Find("PlayerManager");
            if (pm)
            {
                pm.GetComponent<PlayerManager>().PlayerName = PlayerNameText.text;
            }
            SceneManager.LoadScene(2);
        }
    }

    public void loadMainScene()
    {
        SceneManager.LoadScene(0);
    }
}
