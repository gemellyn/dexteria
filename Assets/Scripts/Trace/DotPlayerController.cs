using UnityEngine;
using System.Collections;

[RequireComponent(typeof(DotManager))]
public class DotPlayerController : MonoBehaviour
{
    private DotManager dotManager;
    private Transform currentDot = null;
    //private Vector3 lastClickPos;
    public float timeBetweenDots = 1.0f;
    private float timerEndPath = 0;
    public float DistMinPath = 0.5f;
    public int nbNextPointShow = 10;

    public Material matDot;
    public Material matSlope;
    public Color colTimeOut;
    public Color colBase;

    //Stats
    private float[] timeDots;
    private int iTimeDots = 0;
    private Vector3 previousTouch;
    private Vector3 previousDir;

    private bool startWaveDone = false;

    // Use this for initialization
    void Awake()
    {
        dotManager = GetComponent<DotManager>();
    }

    public void newLevel()
    {
        startWaveDone = false;
    }

    public int getNbDotsTouched()
    {
        return iTimeDots;
    }

    public void getStatsPlayer(ref float moyenne, ref float ecartType)
    {
        float nbDotsTouchs = 0.0f;
        moyenne = 0.0f;
        for (int i = 1; i < iTimeDots; i++)
        {
            moyenne += timeDots[i] - timeDots[i - 1];
            nbDotsTouchs++;
        }
        if (nbDotsTouchs > 0)
            moyenne /= nbDotsTouchs;

        ecartType = 0;
        for (int i = 1; i < iTimeDots; i++)
        {
            ecartType += Mathf.Pow(((timeDots[i] - timeDots[i - 1]) - moyenne), 2);
        }
        if (nbDotsTouchs > 0)
        {
            ecartType /= nbDotsTouchs;
            ecartType = Mathf.Sqrt(ecartType);
        }

    }

    void endGame()
    {
        currentDot = null;
        dotManager.validateAllDots(false);
        dotManager.showAllPath(false);
        dotManager.showAllDots(false);
        dotManager.showDot(0, true);
        stopSounds();
    }

    void stopSounds()
    {
        AudioSource[] srcs = GetComponents<AudioSource>();
        for (int i = 0; i < srcs.Length; i++)
            srcs[i].Stop();
    }

    public void setPitch(float pitch)
    {
        AudioSource[] srcs = GetComponents<AudioSource>();
        for (int i = 0; i < srcs.Length; i++)
            srcs[i].pitch = pitch;
    }

    void upPitch(float delta)
    {
        AudioSource[] srcs = GetComponents<AudioSource>();
        for (int i = 0; i < srcs.Length; i++)
            srcs[i].pitch += delta;
    }

#if UNITY_EDITOR
    public void Update()
    {
        Debug.DrawLine(previousTouch, previousTouch + previousDir,Color.cyan, 0, false);
        Debug.DrawLine(Vector3.zero, previousDir, Color.cyan, 0, false);

        float distance = dotManager.distanceToCurve(new Vector3(previousTouch.x, previousTouch.y,0));

        Debug.DrawLine(Vector3.zero, Vector3.up* distance, Color.red, 0, false);

        
    }
#endif

    void FixedUpdate()
    {
        if (!startWaveDone && dotManager.Dots.Count > 0|| Input.GetButton("Jump")) 
        {
            dotManager.Dots[0].GetComponent<Dot>().doWave(timeBetweenDots);
            startWaveDone = true;
        }


        Vector3 clickPos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10);
        bool touch = true;
        if (Input.touchSupported)
        {
            touch = false;
            if (Input.touchCount > 0)
            {
                // Get movement of the finger since last frame
                Vector2 touchPosition = Input.GetTouch(0).position;
                clickPos = new Vector3(touchPosition.x, touchPosition.y, 10);
                touch = true;
            }
        }
        clickPos = Camera.main.ScreenToWorldPoint(clickPos);


        //float distance = Vector3.Distance(lastClickPos, clickPos);

        //Temps entre deux point attrappés 
        timerEndPath -= Time.fixedDeltaTime;
        

        float distanceToCurve = dotManager.distanceToCurve(clickPos);
        if( ( distanceToCurve > 0.6 || timerEndPath <= 0) && currentDot != null)
        {
            //FAAAAAAAAAIL
            transform.GetComponent<DotLevelManager>().nextLevel(false, false);
            endGame();
            return;
        }
       


       float matLerp = 1;
        if (currentDot != null)
            matLerp = 1 - distanceToCurve;
        /*if (currentDot != null)
            matLerp = Mathf.Pow(timerEndPath / timeBetweenDots, 0.33f);*/

        colTimeOut = new Color(colBase.grayscale, colBase.grayscale, colBase.grayscale,1);

        matDot.color = Color.Lerp(colTimeOut, colBase, matLerp);
        matSlope.color = Color.Lerp(colTimeOut, colBase, matLerp);


        //Si on a pas encore de dot selectionné, le premier doit etre animé
        if (currentDot == null && dotManager.Dots.Count > 0)
        {
            dotManager.Dots[0].GetComponent<Dot>().setValidated(false, true);
        }

        if (touch)
        {
            //On compute la direction du touch
            Vector3 dirTouch = new Vector3();
            if (previousTouch.magnitude == 0)
            {
                previousDir = new Vector3();
                previousTouch = clickPos;
            }
            else
            {
                float distance = (clickPos - previousTouch).magnitude;
                dirTouch = Vector3.Lerp(previousDir,(clickPos - previousTouch).normalized,0.8f).normalized;
                previousDir = dirTouch;
                if(distance > 0f)
                    previousTouch = clickPos;
            }
            


            RaycastHit2D[] hits = Physics2D.RaycastAll(clickPos, Vector2.zero);
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider != null)
                {
                    bool validate = false;
                    //Si on a pas encore validé de points
                    if (currentDot == null) {
                        //Si on touche le premier, on valide
                        if (dotManager.Dots[0] == hit.collider.transform)
                            validate = true;
                    } else {
                        Transform dotToValidate = currentDot;
                        //On va checker n suivants
                        for(int i = 0; i < 2;i++)
                        {
                            dotToValidate = dotToValidate.GetComponent<Dot>().NextDot; //On prend le suivant
                            if(dotToValidate != null && dotToValidate == hit.collider.transform) //Si on a touché ce suivant
                            {
                                if(dotToValidate.GetComponent<Dot>().TouchQuality(dirTouch) > 0.5)
                                    validate = true;
                            }
                        }
                        
                    }

                    //Si c'est un dot utile (le premier, le n+1 ou le n+2)
                    if (validate)
                    {

                        //Si c'est le premier, on raz les temps
                        if (currentDot == null && dotManager.Dots[0] == hit.collider.transform)
                        {
                            if (timeDots == null || timeDots.Length != dotManager.Dots.Count)
                                timeDots = new float[dotManager.Dots.Count];
                            for (int i = 0; i < timeDots.Length; i++)
                                timeDots[i] = 0.0f;
                            iTimeDots = 0;
                        }

                        timeDots[iTimeDots++] = Time.time;

                        currentDot = hit.collider.transform;
                        currentDot.GetComponent<Dot>().setValidated(true, false);

                        timerEndPath = timeBetweenDots;
                        //Pour qu'il soit fonction de la distance, mais on le vire si on se bas sur les stats du joueur
                        /*if (currentDot.GetComponent<Dot>().NextDot != null)
                            timerEndPath *= Mathf.Max(1.0f, Vector3.Distance(currentDot.position, currentDot.GetComponent<Dot>().NextDot.position));*/

                        dotManager.showAllPath(false);
                        dotManager.showAllDots(false);
                        dotManager.showDot(hit.collider.transform, true, nbNextPointShow);
                        dotManager.showPath(hit.collider.transform, true, Mathf.Max(nbNextPointShow - 1, 0));

                        //Du son
                        AudioSource[] srcs = GetComponents<AudioSource>();
                        AudioSource src = srcs[Mathf.RoundToInt(Random.Range(0.0f, 1.0f) * (srcs.Length - 1))];
                        if (!src.isPlaying || src.time > 0.3f)
                            src.Play();
                    }

                    //On a atteint le dernier dot !
                    if (currentDot != null && currentDot.GetComponent<Dot>().NextDot == null)
                    {
                        endGame();
                        transform.GetComponent<DotLevelManager>().nextLevel(false, true);
                        break;
                    }
                }
            }
        }
    }
     

}

