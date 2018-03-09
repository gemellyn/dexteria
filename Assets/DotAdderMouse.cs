using UnityEngine;
using System.Collections;

[RequireComponent(typeof(DotManager))]
public class DotAdderMouse : MonoBehaviour {

    private DotManager dotManager;
    
    // Use this for initialization
    void Start () {
        dotManager = GetComponent<DotManager>();
    }
    
    void Update()
    {
        if (Input.GetButtonDown("Fire2"))
        {
            Vector3 spawnPos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10);
            spawnPos = Camera.main.ScreenToWorldPoint(spawnPos);
            dotManager.addDot(spawnPos,-1);
        }                
    }
}
