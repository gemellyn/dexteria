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

    public GameDifficultyManager DiffManager;
    double[] LastDiffVars; //En 0 on a le temps, en 1 on a la courbe

    int points = 0;
    int numLevel = 0;



    // Use this for initialization
    void Awake() {
        dotGenerator = GetComponent<DotAdderAuto>();
        dotPlayerController = GetComponent<DotPlayerController>();
        dotManager = GetComponent<DotManager>();
        numLevel = 0;
        points = 0;
    }

    void Start()
    {
        GameObject pm = GameObject.Find("PlayerManager");
        if (pm)
            DiffManager.setPlayerId(pm.GetComponent<PlayerManager>().PlayerName);
        DiffManager.setActivity(GameDifficultyManager.GDActivityEnum.TRACE);
        nextLevel(true, false);
        //LastDiffVars = DiffManager.getDiffParams(0);
    }

    void Update()
    {
        /*
        if (Input.GetButtonDown("Fire2"))
        {
            numLevel += 10;
            nextLevel(false, false);
        }

        if (Input.GetButtonDown("Jump"))
        {
            numLevel += 10;
            nextLevel(false, false);
        }*/
        
    }

    public void nextLevel(bool reset, bool win)
    {

        if(LastDiffVars != null && !reset)
        {
            //Si on considère le précédent comme valide (au moins 3 dots validés)
            if(dotPlayerController.getNbDotsTouched() >= 3)
                DiffManager.addTry(LastDiffVars, win);
        }
            

        numLevel++;

        if (win)
        {
            points++;
            dotPlayerController.colBase = Color.HSVToRGB(Random.Range(0.0f, 1.0f), 0.9f, 0.9f);
        }
        
        if (reset)
        {
            GameObject.Find("Music").GetComponent<Music>().newMusic();
            points = 0;
            numLevel = 0;
        }
                    
        if (!win)
        {
            GameObject.Find("Music").GetComponent<Music>().newMusic();
        }

        LastDiffVars = DiffManager.getDiffParams(numLevel);
        LastDiffVars[0] = Mathf.Clamp01((float)(LastDiffVars[0]));
        LastDiffVars[1] = Mathf.Clamp01((float)(LastDiffVars[1]));

        points = Mathf.Max(0, points);

        TextLevel.text = ""+ points;
              
        Debug.Log("Making level " + numLevel);

        //On s'en servira un jour :)
        /*float moyenne = 0;
        float ecartType = 0;
        dotPlayerController.getStatsPlayer(ref moyenne, ref ecartType);

        //Jolie courbe de diff
        float paramLerpBase = (float)numLevel / (float)nbMaxLevels;
        float paramLerpPow = Mathf.Pow(paramLerpBase, 0.1f);
        float paramLerpSigm = 1/(1+Mathf.Exp(-30.0f* paramLerpBase + 6.0f));
        float paramLerp = (paramLerpSigm + paramLerpPow)/2.0f;
        if (paramLerpPow < paramLerpSigm)
            paramLerp = paramLerpPow;*/

        //courbe de diff avec modele
        float paramLerpTime = (float)LastDiffVars[0];
        float paramLerpComplexity = (float)LastDiffVars[1];
        float paramLerpMean = (paramLerpComplexity + paramLerpTime) / 2.0f;

        //Si on force pour tester
#if UNITY_EDITOR
        paramLerpTime = 0.1f;
        paramLerpComplexity = 0.8f;
        paramLerpMean = (paramLerpComplexity + paramLerpTime) / 2.0f;
#endif

        //Son
        dotPlayerController.setPitch(Mathf.Lerp(1.0f,3.0f, paramLerpMean));
        WinSound.pitch = Mathf.Lerp(1.0f, 3.0f, paramLerpMean);
        if (win)
            WinSound.Play();

        //Selection du temps

        //Selection du temps
        float tempsEntreDots = Mathf.Lerp(paramDiffVeryEasy.tempsEntreDots, paramDiffInsane.tempsEntreDots, paramLerpTime);
        dotPlayerController.timeBetweenDots = tempsEntreDots;
        //Debug.Log("timeBetweenDots = " + tempsEntreDots);

        //Selection des autres paramètres 
        float nbDots = Mathf.Lerp(paramDiffVeryEasy.nbDots, paramDiffInsane.nbDots, paramLerpComplexity);
        dotGenerator.nbDots = (int)nbDots;
        //Debug.Log("nbDots = " + nbDots);
        float nbDotsBtwNewAngle = Mathf.Lerp(paramDiffVeryEasy.nbDotsBetweenNewAngle, paramDiffInsane.nbDotsBetweenNewAngle, paramLerpComplexity);
        dotGenerator.freqNewAngle = (int)nbDotsBtwNewAngle;
        //Debug.Log("nbDotsBtwNewAngle = " + (int)nbDotsBtwNewAngle);
        float baseAngle = Mathf.Lerp(paramDiffVeryEasy.baseAngle, paramDiffInsane.baseAngle, paramLerpComplexity);
        dotGenerator.minAngleBase = baseAngle * (1.0f - (spreadBaseAngle-1.0f));
        dotGenerator.maxAngleBase = baseAngle * spreadBaseAngle;
        //Debug.Log("baseAngle = " + baseAngle);
        float probaInverse = Mathf.Lerp(paramDiffVeryEasy.probaInverseAngle, paramDiffInsane.probaInverseAngle, paramLerpComplexity);
        dotGenerator.probaInverseAngle = probaInverse;
        //Debug.Log("probaInverse = " + probaInverse);
        float probaGrosAngle = Mathf.Lerp(paramDiffVeryEasy.probaGrosAngle, paramDiffInsane.probaGrosAngle, paramLerpComplexity);
        dotGenerator.probaGrosAngle = probaGrosAngle;
        //Debug.Log("probaGrosAngle = " + probaGrosAngle);
        float grosAngle = Mathf.Lerp(paramDiffVeryEasy.grosAngle, paramDiffInsane.grosAngle, paramLerpComplexity);
        dotGenerator.minGrosAngle = grosAngle * (1.0f - (spreadBaseAngle - 1.0f));
        dotGenerator.maxGrosAngle = grosAngle * spreadBaseAngle;
        //Debug.Log("grosAngle = " + grosAngle);

        dotGenerator.generate();

        dotPlayerController.newLevel();




    }
	
}
