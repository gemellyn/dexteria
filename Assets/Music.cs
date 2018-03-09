using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Music : MonoBehaviour {

    AudioSource[] musics;

	// Use this for initialization
	void Awake () {
        musics = GetComponents<AudioSource>();
    }
	
	// Update is called once per frame
	void Update () {

        int iplaying = 0;
        bool noPlaying = true;
        for (int i = 0; i < musics.Length; i++)
            if (musics[i].isPlaying)
            {
                iplaying = i;
                noPlaying = false;
            }



        if (noPlaying)
        {
            int iNewZic = (iplaying + Random.Range(1, musics.Length-1)) % musics.Length;
            musics[iNewZic].Play();
        }
    }

    public void newMusic()
    {
        int iplaying = 0;
        for (int i = 0; i < musics.Length; i++)
            if (musics[i].isPlaying)
            {
                iplaying = i;
                musics[i].Stop();
            }

        int iNewZic = (iplaying + Random.Range(1, musics.Length-1)) % musics.Length;

        Debug.Log("Old music was " + iplaying + " new is " + iNewZic);

        musics[iNewZic].Play();
    }
}
