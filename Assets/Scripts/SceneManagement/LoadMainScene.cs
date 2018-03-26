using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadMainScene : MonoBehaviour {

    public Text PlayerNameText;
    private string PlayerName;

    public string getPlayerName()
    {
        return PlayerName;
    }

    public void loadSerpentScene()
    {
        if (PlayerNameText.text.Length > 1) 
        {
            PlayerName = PlayerNameText.text;
            SceneManager.LoadScene(1);
        }
    }

    public void loadDanseScene()
    {
        if (PlayerNameText.text.Length > 1)
        {
            PlayerName = PlayerNameText.text;
            SceneManager.LoadScene(2);
        }
    }
}
