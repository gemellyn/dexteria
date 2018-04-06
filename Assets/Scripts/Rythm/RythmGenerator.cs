using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RythmPlayback), typeof(RythmPlayerControllerAssync))]
public class RythmGenerator : MonoBehaviour
{

    RythmPlayerControllerAssync RPController;
    RythmPlayback RPlayback;
    int NbPlayed = 0;
    bool AcquisitionStarted = false;
    public Text ScoreText;

    public GameDifficultyManager DiffManager;
    double[] LastDiffVars;
    int NumLevel = 0;

    public int[] Choregraphie; //Pour les anims, suite de 1-10

    public Button ClickToGo;

    bool GeneratingLevel = false;

    public AudioSource SoundFail;
    public AudioSource SoundWin;

    float LastScore = 0;

    // Use this for initialization
    void Awake()
    {
        RPController = GetComponent<RythmPlayerControllerAssync>();
        RPlayback = GetComponent<RythmPlayback>();
    }

    void Start()
    {
        GameObject pm = GameObject.Find("PlayerManager");
        if (pm)
            DiffManager.setPlayerId(pm.GetComponent<PlayerManager>().PlayerName);
        DiffManager.setActivity(GameDifficultyManager.GDActivityEnum.SIMON);
        newLevel(false, true);
        ScoreText.text = "";
    }

    /**
    * Calcul de la complexité d'une séquence rythmique
    * Cette fonction normalise par la difficulté max d'une séquence de taille nbSlots
    * Il faut donc gérer la taille de la séquence indépendament
    **/
    float[] RefTabComplexity = { 5, 1, 2, 1, 3, 1, 2, 1, 4, 1, 2, 1, 3, 1, 2, 1 };
    bool[] HasBeenMax = new bool[16];
    bool[] HasBeenMin = new bool[16];
    float calcRythmComplexity(bool[] tab, int nbSlots)
    {
        //Taille de la séquence
        int seqSize = Mathf.Min(RefTabComplexity.Length, tab.Length);

        //On calcule l'accentuation de la séquence
        float accentuation = 0;
        for (int i = 0; i < seqSize; i++)
        {
            accentuation += tab[i] ? RefTabComplexity[i] : 0;
        }

        //On se prépare à calculer l'acentation min et max pour une séquence de cette taille
        for (int i = 0; i < HasBeenMax.Length; i++)
        {
            HasBeenMax[i] = false;
            HasBeenMin[i] = false;
        }
        float accentuationMax = 0;
        float accentuationMin = 0;
        float currentMax = 0;
        float currentMin = 3000;
        int iMax = -1;
        int iMin = -1;

        //Calcul des accentuations min et max
        for (int j = 0; j < nbSlots; j++)
        {
            for (int i = 0; i < seqSize; i++)
            {
                if (RefTabComplexity[i] > currentMax && !HasBeenMax[i])
                {
                    currentMax = RefTabComplexity[i];
                    iMax = i;
                }
                if (RefTabComplexity[i] < currentMin && !HasBeenMin[i])
                {
                    currentMin = RefTabComplexity[i];
                    iMin = i;
                }
            }
            accentuationMax += currentMax;
            accentuationMin += currentMin;
            HasBeenMax[iMax] = true;
            HasBeenMin[iMin] = true;
            currentMax = 0;
            currentMin = 3000;
        }

        //On prend la difficulté max pour une séquence de cette taille et on l'utilise pour normaliser
        float diffMax = accentuationMax - accentuationMin;

        return (accentuationMax - accentuation) / diffMax;
    }

    //On met toujours le premier à 1 car on fait comme si on avait pas de metronome
    //et donc le joueur ne connait pas le début de la mesure
    //pour lui c'est toujours le premier son le début de la mesure
    //Aussi on ne s'autorise pas à avoir deux slots consécutifs pleins, c'est trop dur à jouer
    public void makeRandomTab(ref bool[] tab, int nbTrue)
    {
        for (int i = 0; i < tab.Length; i++)
            tab[i] = false;

        tab[0] = true;

        int iSet = 0;
        for (int i = 0; i < nbTrue - 1; i++)
        {
            iSet = (iSet + Random.Range(0, tab.Length)) % tab.Length;
            int iSearch = 0;
            while ((tab[iSet] || tab[Mathf.Max(0, iSet - 1)]) && iSearch < tab.Length)
            {
                iSearch++;
                iSet = (iSet + 1) % tab.Length;
            }
            tab[iSet] = true;
        }
    }

    public void newLevel(bool reset, bool win)
    {
        GeneratingLevel = true;

        NbPlayed = 0;
        AcquisitionStarted = false;

        Debug.Log("New Level");
        for (int i = 0; i < RythmPlayback.NbSoundSlots; i++)
            RPlayback.setSoundSlot(i, false);
        
        bool[] slots = new bool[RythmPlayback.NbSoundSlots];
        bool[] bestSlots = new bool[RythmPlayback.NbSoundSlots];
        bool[] tmpSlots = null;

        //On recherche la difficulté voulue
        if (LastDiffVars != null && !reset)
        {
            Debug.Log("New Level: update model");
            //Si on considère le précédent comme valide (au moins 3 dots validés)
            DiffManager.addTry(LastDiffVars, win);
            NumLevel++;
        }

        if (reset)
            NumLevel = 0;

        Debug.Log("New Level: get diff params");
        LastDiffVars = DiffManager.getDiffParams(NumLevel);
        LastDiffVars[0] = Mathf.Clamp01((float)(LastDiffVars[0]));

        float wantedComplexity = (float)(LastDiffVars[0]);
        float bestSlotsDist = Mathf.Infinity;
        //wantedComplexity = 0.5f;

        //Creation du niveau
        int nbSlots = 0;
        for (int i = 0; i < 500; i++)
        {
            int iNbSlots = Mathf.RoundToInt(Mathf.Lerp(0, 2, wantedComplexity));
            int[] slotsNb = { 2, 3, 4 }; 
            nbSlots = slotsNb[iNbSlots];
            makeRandomTab(ref slots, nbSlots);
            float diff = calcRythmComplexity(slots, nbSlots);

            float dist = Mathf.Abs(wantedComplexity - diff);
            //Debug.Log("Rythm diff " + diff);
            if (dist <= bestSlotsDist || bestSlotsDist== Mathf.Infinity)
            {
                Debug.Log("New diff " + diff);
                bestSlotsDist = dist;
                tmpSlots = bestSlots;
                bestSlots = slots;
                slots = tmpSlots;
            }
            if (dist < 0.01)
                break;
        }
        Debug.Log("Best rythm dist " + bestSlotsDist);
        slots = bestSlots;

        for (int i = 0; i < RythmPlayback.NbSoundSlots; i++)
            RPlayback.setSoundSlot(i, slots[i]);

        //La choré
        nbSlots = RPlayback.getNbActiveSlots();
        Choregraphie = new int[nbSlots];
        for (int i = 0; i < nbSlots; i++)
            Choregraphie[i] = Random.Range(1, 9);

        waitPlayerStartForNewRythm();

        GeneratingLevel = false;
    }

    IEnumerator waitBeforeNewRythm()
    {
        RPlayback.PlayMetronome = false;
        yield return new WaitForSeconds(RPlayback.getMeasureDuration() * 1.0f);
        RPlayback.PlayMetronome = true;
        StartCoroutine("playNewRythm");
    }

    void waitPlayerStartForNewRythm()
    {
        RPlayback.PlayMetronome = false;
        RPlayback.showTinyMetronome(false);
        ClickToGo.gameObject.SetActive(true);
    }

    public void ClickGoNewRythm()
    {
        ClickToGo.gameObject.SetActive(false);
        RPlayback.PlayMetronome = true;
        StartCoroutine("playNewRythm");
    }

    IEnumerator playNewRythm()
    {
        //Si on va capter le joueur, on demande le décompte
        if (AcquisitionStarted)
        {
            RPlayback.Decompte = true;
            RPlayback.DecompteDuration = 4;
            RPlayback.showTinyMetronome(true);
        }
        else
        {
            RPlayback.setAnimMetronome(false);
            RPlayback.showTinyMetronome(false);
        }
            


        yield return new WaitForSeconds(RPlayback.getMeasureDuration()*0.9f);
        RPlayback.playRythm(true);
        RPlayback.PlayMetronome = true;
    }

    IEnumerator playWinAnimAndNewLevel()
    {
        RPlayback.PlayMetronome = false;
        yield return new WaitForSeconds(1.0f);
        ScoreText.text = "" + (int)(LastScore * 1000);
        SoundWin.Play();
        RPlayback.DancerAnims.SetInteger("DanceNumber", 2);
        RPlayback.DancerAnims.SetTrigger("EndMove");
        RPlayback.DancerAnims.SetTrigger("Dance");
        yield return new WaitForSeconds(2.0f);
        ScoreText.text = "";
        newLevel(false, true);
        
    }

    IEnumerator playFailAnimAndNewLevel()
    {
        RPlayback.PlayMetronome = false;    
        yield return new WaitForSeconds(1.0f);
        ScoreText.text = "" + (int)(LastScore * 1000);
        SoundFail.Play();
        RPlayback.DancerAnims.SetInteger("DanceNumber", 8);
        RPlayback.DancerAnims.SetTrigger("EndMove");
        RPlayback.DancerAnims.SetTrigger("Dance");
        yield return new WaitForSeconds(2.0f);
        ScoreText.text = "";
        newLevel(false, false);
        
    }

    // Update is called once per frame
    void Update()
    {
        if (GeneratingLevel)
            return;
       
        if (RPlayback.isRythmPlayed())
        {
            NbPlayed++;
            RPlayback.playRythm(false);
            if (NbPlayed == 1)
            {
                AcquisitionStarted = true;
                StartCoroutine("playNewRythm");
                RPController.startAcquisition(RPlayback.getNbActiveSlots(), RPlayback.getMeasureDuration());
                
            }            
        }

        if (AcquisitionStarted)
        {
            if (RPController.isAcquisitionDone())
            {
                AcquisitionStarted = false;
                RPlayback.playRythm(false);
                LastScore = RPlayback.scoreOnMax(RPController.TapTimes);
                ScoreText.text = "" + (int)(LastScore * 1000);
                if (LastScore > 0.6)
                    SoundWin.Play();
                else
                    SoundFail.Play();

                newLevel(false, LastScore > 0.6);

                /*if (LastScore > 0.5)
                    StartCoroutine("playWinAnimAndNewLevel");
                else
                    StartCoroutine("playFailAnimAndNewLevel");*/
            }
        }
    }
}
