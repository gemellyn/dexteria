using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RythmPlayback))]
public class RythmPlayerControllerRT : MonoBehaviour {


    bool WasTouching = false;
    float ElapsedSinceLastTouch = 0;
    float Score;
    RythmPlayback RythmePb;

    public float TypeQualityThreshold = 0.9f;
    public int ScoreGainGoodTouch = 100;
    public int ScoreLossBadTouch = 200;


    // Use this for initialization
    void Start () {
        RythmePb = GetComponent<RythmPlayback>();
        Score = 0;
    }
	
	// Update is called once per frame
	void FixedUpdate () {

        ElapsedSinceLastTouch += Time.deltaTime;
        
        bool isTouching = (Input.GetButton("Fire1") || (Input.touchSupported && Input.touchCount > 0));
        bool newTap = (isTouching && !WasTouching);
        WasTouching = isTouching;

        //On limite le nombre de touch par seconde
        if (ElapsedSinceLastTouch < 0.5)
            return;

        if (newTap)
        {
            ElapsedSinceLastTouch = 0;
            float tapQuality = RythmePb.evaluatePlayerTap();
            if(tapQuality > TypeQualityThreshold)
            {
                Score += ScoreGainGoodTouch;
            }
            else
            {
                Score -= ScoreLossBadTouch * Mathf.Pow((TypeQualityThreshold-tapQuality) / TypeQualityThreshold,0.3f);
            }
        }
    }
}
