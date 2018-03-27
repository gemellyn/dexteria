using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RythmPlayerControllerAssync : MonoBehaviour {

    public List<float> TapTimes;
    float TotalTimeLeft = 0;
    bool WasTouching = false;
    float ElapsedSinceLastTouch = 0;
    float ElapsedSinceFirstTouch = 0;
    int NbTapsToGet = 0;
    bool AcquisitionDone = false;

    public Animator DancerAnims;
    bool PlayFirst = true;

    public AudioSource SoundSourceFirst;
    public AudioSource SoundSourceOther;

    // Use this for initialization
    void Awake () {
        TapTimes = new List<float>();
    }

    public void startAcquisition(int nbTapsToGet,float totalTimeLeft)
    {
        ElapsedSinceFirstTouch = 0;
        TapTimes.Clear();
        NbTapsToGet = nbTapsToGet;
        AcquisitionDone = false;
        PlayFirst = true;
        TotalTimeLeft = totalTimeLeft;
    }

    public bool isAcquisitionDone()
    {
        return AcquisitionDone;
    }
	
	// Update is called once per frame
	void Update () {
        ElapsedSinceLastTouch += Time.deltaTime;
        if (TapTimes.Count > 0)
            ElapsedSinceFirstTouch += Time.deltaTime;

        bool isTouching = (Input.GetButton("Fire1") || (Input.touchSupported && Input.touchCount > 0));
        bool newTap = (isTouching && !WasTouching);
        WasTouching = isTouching;

        //On limite le nombre de touch par seconde
        if (ElapsedSinceLastTouch < 0.001)
            return; 

        if (ElapsedSinceFirstTouch > TotalTimeLeft)
            AcquisitionDone = true;

        if (newTap && !AcquisitionDone)
        {
            TapTimes.Add(ElapsedSinceFirstTouch);
            /*if (PlayFirst)
                SoundSourceFirst.Play();
            else
                SoundSourceOther.Play();

            PlayFirst = false;*/
            DancerAnims.SetInteger("DanceNumber", Random.Range(1, 10));
            DancerAnims.SetTrigger("EndMove");
            DancerAnims.SetTrigger("Dance");
            if (TapTimes.Count >= NbTapsToGet)
            {
                AcquisitionDone = true;
            }
        }
    }
}
