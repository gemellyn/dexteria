using UnityEngine;
using System.Collections;

public class Dot : MonoBehaviour {

    public Transform NextDot;//{ get; set; }
    public Transform PreviousDot;//{ get; set; }
    public Material MatToValidate;
    public Material MatValidated;
    private bool Validated;
    private Vector3 Direction; //La direction du tracé à ce point (et donc du mouvement qui le valide)
    private float Directionality; //Continuité du tracé en ce point (et donc du mouvement qui le valide)
    private bool DoWave;
    private float WaveTimeBase;
    private float WaveTime;

    // Use this for initialization
    void Start () {
        setValidated(false,false);
        Validated = false;
        computeDirectionality();
    }

    public void doWave(float time)
    {
        
        WaveTimeBase = time;
        WaveTime = time;
        DoWave = true;

        Debug.Log("Go anim !! " + Time.time);
        GetComponentInChildren<Animator>().SetTrigger("Wave");
    }

    void computeDirectionality()
    {
        Vector3 dirPrev = new Vector3();
        Vector3 dirNext = new Vector3();

        if(PreviousDot != null)
        {
            dirPrev = (transform.position - PreviousDot.position).normalized;
        }
        
        if (NextDot != null)
        {
            dirNext = (NextDot.position - transform.position).normalized;
        }

        if (!PreviousDot && NextDot)
        {
            Direction = dirNext;
            Directionality = 1;
        }
        else if (PreviousDot && !NextDot)
        {
            Direction = dirPrev;
            Directionality = 1;
        } else if(PreviousDot && NextDot)
        {
            Direction = (dirPrev + dirNext).normalized;
            Directionality = Vector3.Dot(dirPrev, dirNext);
        } else
        {
            Debug.Log("Ben non pas de direction pour moi" + PreviousDot + NextDot);
            Direction = new Vector3();
            Directionality = 0;
        }
    }

    //Détermine si la direction du tracé du joueur est la
    //même que la direction du point. Tiens compte du fait
    //que la direction du point peut être plus ou moins forte (continuité du tracé en ce point)
    public float TouchQuality(Vector3 touchDirection)
    {
        float sameDir = Vector3.Dot(touchDirection, Direction);

        //Si le point est situé sur un tracé d'angle inférieur à 90 degres
        if(Directionality > 0)
        {
            //On retourne l'angle entre la direction du tracé joueur et celle du point
            return sameDir;
        }

        //Si le point est sur un tracé supérieur à 90 deg, on dit que c'est toujours bon
        return 1.0f;
    }


    public void setValidated(bool validated, bool first)
    {
        if (validated)
        {
            if (!this.Validated)
            {
                GetComponentInChildren<Animator>().SetTrigger("Validate");
                GetComponentInChildren<SpriteRenderer>().material = MatValidated;
            }
        }
        else
        {
            if(this.Validated)
                GetComponentInChildren<SpriteRenderer>().material = MatToValidate;
            if(first)
                GetComponentInChildren<Animator>().SetTrigger("Waiting");
        }
        this.Validated = validated; 
    }


    public void FixedUpdate()
    {
        if (DoWave)
        {
            WaveTime -= Time.deltaTime;
            if (WaveTime <= 0)
            {
                Debug.Log("Wave !! " + Time.time);

                DoWave = false;
                if (NextDot && NextDot.GetComponentInChildren<SpriteRenderer>().enabled)
                {
                    NextDot.GetComponent<Dot>().doWave(WaveTimeBase);
                }
                    
            }
        }
#if UNITY_EDITOR
        if (Direction.sqrMagnitude == 0)
            Debug.DrawLine(transform.position, transform.position + Vector3.forward, Color.cyan, 0, false);
        Debug.DrawLine(transform.position, transform.position + Direction, Color.Lerp(Color.red, Color.green, Directionality),0,false);
#endif
    }

}
