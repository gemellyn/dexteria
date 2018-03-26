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
        newLevel(false,true);
    }

    float[] RefTabComplexity = { 5, 1, 2, 1, 3, 1, 2, 1, 4, 1, 2, 1, 3, 1, 2, 1 };
    bool[] HasBeenMax = new bool[16];
    bool[] HasBeenMin = new bool[16];
    float calcRythmComplexity(bool[] tab, int nbSlots)
    {
        int seqSize = Mathf.Min(RefTabComplexity.Length, tab.Length);
        float accentuation = 0;
        for (int i = 0; i < seqSize; i++)
        {
            accentuation += tab[i] ? RefTabComplexity[i] : 0;
        }

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
        for (int i = 0; i < nbTrue-1; i++)
        {
            iSet = (iSet + Random.Range(0, tab.Length)) % tab.Length;
            int iSearch = 0;
            while ((tab[iSet] || tab[Mathf.Max(0,iSet-1)]) && iSearch < tab.Length)
            {
                iSearch++;
                iSet = (iSet + 1) % tab.Length;
            }
            tab[iSet] = true;
        }
    }

    public void newLevel(bool reset, bool win)
    {
        for (int i = 0; i < RythmPlayback.NbSoundSlots; i++)
            RPlayback.setSoundSlot(i, false);


        bool[] slots = new bool[RythmPlayback.NbSoundSlots];
        bool[] bestSlots = new bool[RythmPlayback.NbSoundSlots];
        bool[] tmpSlots = null;

        //On recherche la difficulté voulue
        if (LastDiffVars != null && !reset)
        {
            //Si on considère le précédent comme valide (au moins 3 dots validés)
            DiffManager.addTry(LastDiffVars, win);
            NumLevel++;
        }

        if (reset)
            NumLevel = 0;

        LastDiffVars = DiffManager.getDiffParams(NumLevel);

        float wantedComplexity = (float)(LastDiffVars[0]);
        float bestSlotsDist = 100;
        //wantedComplexity = 0.5f;

        //Creation du niveau
        for (int i = 0; i < 10000; i++)
        {
            int nbSlots = Mathf.RoundToInt(Mathf.Lerp(4, 7, wantedComplexity));
            makeRandomTab(ref slots, nbSlots);
            float diff = calcRythmComplexity(slots, nbSlots);

            float dist = Mathf.Abs(wantedComplexity - diff);
            //Debug.Log("Rythm diff " + diff);
            if (dist < bestSlotsDist)
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
        RPlayback.playRythm(true);
        NbPlayed = 1;
        AcquisitionStarted = false;
    }

    IEnumerator playNewRythm()
    {
        yield return new WaitForSeconds(RPlayback.getMeasureDuration());
        RPlayback.playRythm(true);
    }

    // Update is called once per frame
    void Update()
    {
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
                RPController.startAcquisition(RPlayback.getNbActiveSlots());
                AcquisitionStarted = true;
            }
        }

        if (AcquisitionStarted)
        {
            if (RPController.isAcquisitionDone())
            {
                float score = RPlayback.score(RPController.TapTimes);
                ScoreText.text = "" + (int)(score * 1000);
                newLevel(false, score > 0.95);
            }
        }
    }
}
