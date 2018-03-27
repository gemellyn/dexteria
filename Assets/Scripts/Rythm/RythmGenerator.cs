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
        GameObject sm = GameObject.Find("SceneManager");
        if (sm)
            DiffManager.setPlayerId(sm.GetComponent<LoadMainScene>().getPlayerName());
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
        float currentMin = 30;
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
            currentMin = 30;
        }

        //On prend la difficulté max pour une séquence de cette taille et on l'utilise pour normaliser
        float diffMax = accentuationMax - accentuationMin;

        return (accentuationMax - accentuation) / diffMax;
    }

    //On met toujours le premier à 1 car on a pas de metronome
    //et donc le joueur ne connait pas le début de la mesure
    //pour lui c'est toujours le premier son le début de la mesure
    //Aussi on autorise pas d'avoir deux slots consécutifs pleins, c'est trop dur à jouer
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

        float wantedComplexity = (float)(LastDiffVars[0]);
        float bestSlotsDist = Mathf.Infinity;
        //wantedComplexity = 0.5f;

        //Creation du niveau
        for (int i = 0; i < 500; i++)
        {
            int nbSlots = Mathf.RoundToInt(Mathf.Lerp(4, 7, wantedComplexity));
            nbSlots = Mathf.Clamp(nbSlots, 4, 7);
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
        
        NbPlayed = 1;
        AcquisitionStarted = false;

        StartCoroutine("playNewRythm");

        GeneratingLevel = false;
    }

    IEnumerator playNewRythm()
    {
        yield return new WaitForSeconds(RPlayback.getMeasureDuration());
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
        //if(Input.GetButton("Fire2"))
        //newLevel();

        if (RPlayback.isRythmPlayed())
        {
            RPlayback.playRythm(false);
            if (NbPlayed < 2)
            {
                NbPlayed++;
                StartCoroutine("playNewRythm");
            }
            else
            {
                RPController.startAcquisition(RPlayback.getNbActiveSlots(), RPlayback.getMeasureDuration()+1);
                AcquisitionStarted = true;
            }
        }

        if (AcquisitionStarted)
        {
            if (RPController.isAcquisitionDone())
            {
                AcquisitionStarted = false;
                RPlayback.playRythm(false);
                LastScore = RPlayback.scoreOnMax(RPController.TapTimes);
                
                if (LastScore > 0.5)
                    StartCoroutine("playWinAnimAndNewLevel");
                else
                    StartCoroutine("playFailAnimAndNewLevel");
            }
        }
    }
}
