using System.Collections.Generic;
using UnityEngine;

public class RythmPlayback : MonoBehaviour {

    public const int NbSoundSlots = 16; //On a 16 divisions dans une mesure
    const int NbBeats = 2; //Nombre de noires (beat) par mesure ici 4/4 
    const int NbSlotsPerBeat = NbSoundSlots / NbBeats;
    int BPM; //Nombre de noires par minute
    float SlotDuration; //A calculer au start
    float MeasureDuration;

    public bool [] SoundSlots;
    public AudioSource SoundSourceFirst;
    public AudioSource SoundSourceOther;
    public AudioSource SoundSourceMetronome;

   
    float ElapsedInCurrentTimeSlot;
    int NumSlotForMetronome = 0;
    int CurrentSoundSlot = 0;
    bool PlayRythm = false;
    bool WaitForMesureStart = true;
    bool PlayFirst = true;
    bool RythmPlayed = false;
    public bool PlayMetronome = false;

    public Animator DancerAnims;
    int StepChoregraphie = 0;

    RythmGenerator RGenerator;


    void Awake()
    {
        SoundSlots = new bool[NbSoundSlots];
        setBPM(60);
        RGenerator = GetComponent<RythmGenerator>();
    }



    public int getNbActiveSlots()
    {
        int nbSlots = 0;
        for (int i = 0; i < NbSoundSlots; i++)
            if (SoundSlots[i])
                nbSlots++;
        return nbSlots;
    }

    public float getMeasureDuration()
    {
        return MeasureDuration;
    }

    public void setBPM(int bpm)
    {
        BPM = bpm;
        SlotDuration = (60.0f / (float)BPM) / (float)NbSlotsPerBeat;
        MeasureDuration = SlotDuration * NbSoundSlots;
    }

    public void setSoundSlot(int num, bool activate)
    {
        if(num < NbSoundSlots)
            SoundSlots[num] = activate;
    }

    public void playRythm(bool play)
    {
        CurrentSoundSlot = 0;
        ElapsedInCurrentTimeSlot = 0;
        PlayRythm = play;
        PlayFirst = true;
        RythmPlayed = false;
        WaitForMesureStart = true;
        StepChoregraphie = 0;
    }

    public bool isRythmPlayed()
    {
        return RythmPlayed;
    }

    public float score(List<float> tapTimes)
    {
        float res = 0;

        //On genere le resultat ideal
        List<float> bestTap = new List<float>();
        float firstSlotIndex = 0;
        for(int i = 0; i < NbSoundSlots; i++)
        {
            if (SoundSlots[i])
            {
                if (bestTap.Count == 0)
                    firstSlotIndex = i;
                bestTap.Add((i - firstSlotIndex) * SlotDuration);
            }
        }

        //On compare les deux listes
        int iCompare = 0;
        foreach(float time in bestTap)
        {
            if (iCompare >= tapTimes.Count)
                break;
            res += Mathf.Abs(time - tapTimes[iCompare]);
            iCompare++;
        }

        res /= MeasureDuration * (getNbActiveSlots()-1); //-1 car le premier est toujours aligné

        return 1-Mathf.Pow(res,0.5f); //Oon veut plus de détail au début
    }

    //Calcul du score sur la diff max
    public float scoreOnMax(List<float> tapTimes)
    {
        float res = 0;

        //On genere le resultat ideal
        List<float> bestTap = new List<float>();
        float firstSlotIndex = 0;
        for (int i = 0; i < NbSoundSlots; i++)
        {
            if (SoundSlots[i])
            {
                if (bestTap.Count == 0)
                    firstSlotIndex = i;
                bestTap.Add((i - firstSlotIndex) * SlotDuration);
            }
        }

        //On compare les deux listes
        int iCompare = 0;
        foreach (float time in bestTap)
        {
            if (iCompare >= tapTimes.Count)
                break;
            if(Mathf.Abs(time - tapTimes[iCompare]) > res)
                res = Mathf.Abs(time - tapTimes[iCompare]);
            iCompare++;
        }

        if (iCompare != bestTap.Count) 
            res = 1;

        res = Mathf.Clamp01(res);
        //res /= MeasureDuration * (getNbActiveSlots() - 1); //-1 car le premier est toujours aligné

        return 1 - Mathf.Pow(res, 0.5f); //Oon veut plus de détail au début
    }

    //Retourne le temps entre le slot activé le plus proche et maintenant
    public float evaluatePlayerTap()
    {
        //Find the closest activated sound slot
        for(int i = 0; i < NbSoundSlots / 2; i++)
        { 
            //On commence par les slots suivants, qui sont les plus proches (vu que notre slot est entammé)
            int testSlot = (CurrentSoundSlot + i)%NbSoundSlots;
            if (SoundSlots[testSlot])
                return 1.0f - (((i * SlotDuration) + (SlotDuration - ElapsedInCurrentTimeSlot)) / MeasureDuration);

            //On fait ensuite les slots précédents si il était pas actif
            testSlot = (CurrentSoundSlot - i);
            if (testSlot < 0)
                testSlot += NbSoundSlots;
            if (SoundSlots[testSlot])
                return 1.0f - (((i * SlotDuration) + (ElapsedInCurrentTimeSlot))/ MeasureDuration);

        }

        return 0; 
    }

    // Update is called once per frame
    void Update () {
        float deltaTime = Time.deltaTime;
        if (Time.deltaTime > 0.3f)
            deltaTime = 1 / 60.0f;

        ElapsedInCurrentTimeSlot += deltaTime;
        if (ElapsedInCurrentTimeSlot >= SlotDuration)
        {
            ElapsedInCurrentTimeSlot -= SlotDuration;
            if (NumSlotForMetronome % 8 == 0 && PlayMetronome)
            {
                SoundSourceMetronome.Play();
                WaitForMesureStart = false;
            }
                
            NumSlotForMetronome++;

            if (PlayRythm && !WaitForMesureStart)
            {
                if (SoundSlots[CurrentSoundSlot])
                {
                    if(PlayFirst)
                        SoundSourceFirst.Play();
                    else
                    {
                        SoundSourceOther.Play();
                    }
                        
                    DancerAnims.SetInteger("DanceNumber", RGenerator.Choregraphie[StepChoregraphie++]);
                    DancerAnims.SetTrigger("EndMove");
                    DancerAnims.SetTrigger("Dance");
                    PlayFirst = false;
                }

                
                CurrentSoundSlot = (CurrentSoundSlot + 1);
                if(CurrentSoundSlot >= NbSoundSlots)
                {
                    CurrentSoundSlot -= NbSoundSlots;
                    PlayFirst = true;
                    RythmPlayed = true;
                }
            }
        }
	}
}
