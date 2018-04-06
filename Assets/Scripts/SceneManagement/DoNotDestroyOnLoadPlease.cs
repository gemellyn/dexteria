using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoNotDestroyOnLoadPlease : MonoBehaviour {

    public static DoNotDestroyOnLoadPlease instance;

    // Use this for initialization
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            Debug.Log("Create " + this.GetInstanceID());
            DontDestroyOnLoad(this.gameObject);
        }
        else if (instance != this)
        {
            Debug.Log("Destroy " + this.GetInstanceID());
            Destroy(gameObject);
        }
    }
}
