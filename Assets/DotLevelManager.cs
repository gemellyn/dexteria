using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(DotAdderAuto), typeof(DotPlayerController))]
public class DotLevelManager : MonoBehaviour {

    [System.Serializable]
    public class ParamsDiff
    {
        public float tempsEntreDots = 0.5f;
        public float nbDots = 30.0f;
        public float nbDotsBetweenNewAngle = 10.0f;
        public float baseAngle = 0.0f;
        public float probaInverseAngle = 0.0f;
        public float probaGrosAngle = 0.0f;
        public float grosAngle = 0.0f;
    };

    public int nbMaxLevels = 30;
    public ParamsDiff paramDiffVeryEasy;
    public ParamsDiff paramDiffInsane;
    public Text TextLevel;
    public AudioSource WinSound;

    //Params diff
    private float spreadBaseAngle = 1.2f;
    
    DotAdderAuto dotGenerator;
    DotPlayerController dotPlayerController;
    DotManager dotManager;

    int numLevel = 0;

    // Use this for initialization
    void Awake() {
        dotGenerator = GetComponent<DotAdderAuto>();
        dotPlayerController = GetComponent<DotPlayerController>();
        dotManager = GetComponent<DotManager>();
        numLevel = 0;
    }

    void Start()
    {
        nextLevel(true, false);
    }

    void Update()
    {
        if (Input.GetButtonDown("Fire2"))
        {
            numLevel += 10;
            nextLevel(false, false);
        }

       /* if (Input.GetButtonDown("Jump"))
        {
            numLevel += 10;
            nextLevel(false, false);
        }*/
    }

    public void nextLevel(bool reset, bool win)
    {
        if (win)
        {
            numLevel += 1;
            dotPlayerController.colBase = Color.HSVToRGB(Random.Range(0.0f, 1.0f), 0.9f, 0.9f);
        }

        if (reset)
        {
            GameObject.Find("Music").GetComponent<Music>().newMusic();
            numLevel = 0;
        }
            
        
        if (!win)
        {
            numLevel -= 5;
            GameObject.Find("Music").GetComponent<Music>().newMusic();
        }
            

        numLevel = Mathf.Max(0, numLevel);

        TextLevel.text = ""+numLevel;
              

        Debug.Log("Making level " + numLevel);

        //On s'en servira un jour :)
        float moyenne = 0;
        float ecartType = 0;
        dotPlayerController.getStatsPlayer(ref moyenne, ref ecartType);

        //JOlie courbe de diff
        float paramLerpBase = (float)numLevel / (float)nbMaxLevels;
        float paramLerpPow = Mathf.Pow(paramLerpBase, 0.1f);
        float paramLerpSigm = 1/(1+Mathf.Exp(-30.0f* paramLerpBase + 6.0f));
        float paramLerp = (paramLerpSigm + paramLerpPow)/2.0f;
        if (paramLerpPow < paramLerpSigm)
            paramLerp = paramLerpPow;

        //Son
        dotPlayerController.setPitch(Mathf.Lerp(1.0f,3.0f, paramLerpBase));
        WinSound.pitch = Mathf.Lerp(1.0f, 3.0f, paramLerpBase);
        if (win)
            WinSound.Play();

        //Courbe de difficulté rigolote
        float tempsEntreDots = Mathf.Lerp(paramDiffVeryEasy.tempsEntreDots, paramDiffInsane.tempsEntreDots, paramLerp);
        dotPlayerController.timeBetweenDots = tempsEntreDots;
        Debug.Log("timeBetweenDots = " + tempsEntreDots);
        float nbDots = Mathf.Lerp(paramDiffVeryEasy.nbDots, paramDiffInsane.nbDots, paramLerp);
        dotGenerator.nbDots = (int)nbDots;
        Debug.Log("nbDots = " + nbDots);
        float nbDotsBtwNewAngle = Mathf.Lerp(paramDiffVeryEasy.nbDotsBetweenNewAngle, paramDiffInsane.nbDotsBetweenNewAngle, paramLerp);
        dotGenerator.freqNewAngle = (int)nbDotsBtwNewAngle;
        Debug.Log("nbDotsBtwNewAngle = " + (int)nbDotsBtwNewAngle);
        float baseAngle = Mathf.Lerp(paramDiffVeryEasy.baseAngle, paramDiffInsane.baseAngle, paramLerp);
        dotGenerator.minAngleBase = baseAngle * (1.0f - (spreadBaseAngle-1.0f));
        dotGenerator.maxAngleBase = baseAngle * spreadBaseAngle;
        Debug.Log("baseAngle = " + baseAngle);
        float probaInverse = Mathf.Lerp(paramDiffVeryEasy.probaInverseAngle, paramDiffInsane.probaInverseAngle, paramLerp);
        dotGenerator.probaInverseAngle = probaInverse;
        Debug.Log("probaInverse = " + probaInverse);
        float probaGrosAngle = Mathf.Lerp(paramDiffVeryEasy.probaGrosAngle, paramDiffInsane.probaGrosAngle, paramLerp);
        dotGenerator.probaGrosAngle = probaGrosAngle;
        Debug.Log("probaGrosAngle = " + probaGrosAngle);
        float grosAngle = Mathf.Lerp(paramDiffVeryEasy.grosAngle, paramDiffInsane.grosAngle, paramLerp);
        dotGenerator.minGrosAngle = grosAngle * (1.0f - (spreadBaseAngle - 1.0f));
        dotGenerator.maxGrosAngle = grosAngle * spreadBaseAngle;
        Debug.Log("grosAngle = " + grosAngle);

        dotGenerator.generate();

        dotPlayerController.newLevel();




    }
	
}
