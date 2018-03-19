using UnityEngine;
using System.Collections;

public class DotManager : MonoBehaviour {

	public System.Collections.Generic.List<Transform> Dots;
    public Transform DotPrefab;


    //Attention, ca marche quand on est proche de la courbe, sinon ca projette sur la droite qui passe par les deux points les plus
    //proches et donc ca peut etre faux
    public float distanceToCurve(Vector3 point, bool onlyVisible = true)
    {
        float distance = 0;

        if (Dots.Count < 2)
            return 0;

        //On cherche les deux plus proches

        int i= 0;
        float dMin1 = 1000000;
        Transform Dot1 = null;
        float dMin2 = 1000000;
        Transform Dot2 = null;

        Transform tmpDot = Dots[0];
        while (tmpDot!=null && i < Dots.Count)
        {
            if (!onlyVisible || tmpDot.GetComponentInChildren<SpriteRenderer>().enabled)
            {
                float distLoc = (point - tmpDot.position).sqrMagnitude;
                if (distLoc < dMin1)
                {
                    Dot2 = Dot1;
                    dMin2 = dMin1;
                    Dot1 = tmpDot;
                    dMin1 = distLoc;
                    
                }
                else if (distLoc < dMin2)
                {
                    dMin2 = distLoc;
                    Dot2 = tmpDot;
                    
                }
                
            }

            tmpDot = tmpDot.GetComponent<Dot>().NextDot;
            i++;
        }

        if (Dot1 == null || Dot2 == null)
            return 0;

        //On projette sur le vecteur perpendiculaire (distance à la courbe)
        Vector3 vec = (Dot2.position - Dot1.position).normalized;
        float tmpx = vec.x;
        vec.x = -vec.y;
        vec.y = tmpx;

        distance = Mathf.Abs(Vector3.Dot(vec, point - Dot1.position));

        //Debug.Log(distance);

        return distance;
    }

    /*
     * PATH DRAWING 
     * 
     */
    public void showDot(Transform dot, bool enable, int nbNext=0, bool all = false)
    {
        Transform tmpDot = dot;
        for (int i = 0; i <= nbNext || all; i++)
        {
            tmpDot.GetComponentInChildren<SpriteRenderer>().enabled = enable;
            tmpDot = tmpDot.GetComponent<Dot>().NextDot;
            if (tmpDot == null)
                break;
        }
    }

    public void showDot(int i, bool enable, int nbNext = 0, bool all = false)
    {
        if(i < Dots.Count)
            showDot(Dots[i],enable,nbNext,all);
    } 

    public void showPath(Transform dot, bool enable, int nbNext = 0, bool all=false)
    {
        Transform tmpDot = dot;
        for(int i = 0; i <= nbNext || all; i++)
        {
            tmpDot.GetComponent<LineRenderer>().enabled = enable;
            tmpDot = tmpDot.GetComponent<Dot>().NextDot;
            if (tmpDot == null)
                break;
        }
    }

    public void showPath(int i, bool enable, int nbNext = 0, bool all = false)
    {
        showPath(Dots[i], enable, nbNext, all);
    }

    //private Transform spsTmpDot;
    //private int spsNbNext;
    //private bool spsAll;
    /*public IEnumerator showPathSlowly()
    {
        Transform tmpDot = spsTmpDot;
        for (int i = 0; i <= spsNbNext || spsAll; i++)
        {
            tmpDot.GetComponent<LineRenderer>().enabled = true;
            tmpDot = tmpDot.GetComponent<Dot>().NextDot;
            if (tmpDot == null)
                break;
            else
                yield return new WaitForSeconds(.1f);
        }
    }*/

    public void showAllDots(bool enable)
    {
        foreach (Transform tmpDot in Dots)
        {
            tmpDot.GetComponentInChildren<SpriteRenderer>().enabled = enable;
        }
    }

    public void showAllPath(bool enable)
    {
        foreach (Transform tmpDot in Dots)
        {
            tmpDot.GetComponent<LineRenderer>().enabled = enable;

        }
    }

    public void validateAllDots(bool validate)
    {
        foreach (Transform tmpDot in Dots)
        {
            tmpDot.GetComponent<Dot>().setValidated(validate,false);
        }
    }

    public bool isOutOfViewPort()
    {
        foreach (Transform tmpDot in Dots)
        {
            Vector3 pos = Camera.main.WorldToViewportPoint(tmpDot.position);
            if (pos.x < 0.1 || pos.x > 0.9 || pos.y < 0.1 || pos.y > 0.9)
                return true;

        }
        return false;
    }

    /*public Vector3 getBarycenter()
    {
        Vector3 bary = new Vector3(0, 0, 0);
        foreach (Transform tmpDot in Dots)
        {
            bary += tmpDot.position;
        }

        if (Dots.Count > 0)
            bary /= Dots.Count;

        return bary;
    }*/

    /*
     * ADD and REMOVE DOTS 
     */
    public void cleanDots()
    {
        foreach(Transform dot in Dots)
        {
            GameObject.Destroy(dot.gameObject);
        }
        Dots.Clear();
    }

    public void addDot(Vector3 position, int nbDotsRemaining)
    {
        
        Transform dot = GameObject.Instantiate(DotPrefab, position, Quaternion.identity) as Transform;
        dot.parent = this.transform;

        //On les lie
        if (Dots.Count > 0)
        {
            Transform prevDot = Dots[Dots.Count - 1];
            prevDot.GetComponent<Dot>().NextDot = dot;
            dot.GetComponent<Dot>().PreviousDot = prevDot;         
        }

        //Un de plus dans la liste
        Dots.Add(dot);

        for (int i = Dots.Count - 3; i < Dots.Count - 1; i++)
            if (i >= 0)
                UpdateSpline(Dots[i]);

        //Plus gros collider du dernier point
        if(nbDotsRemaining == 1)
        {
            dot.GetComponent<CircleCollider2D>().radius *= 2;
        }
    }


    /*
     * SPLINE BUILDING  
     */
    public void UpdateSplineAllDots()
    {
        foreach (Transform t in Dots)
            UpdateSpline(t);
    }

    void UpdateSpline(Transform dot)
    {
        Transform d0 = dot.GetComponent<Dot>().PreviousDot;
        Transform d1 = dot;
        Transform d2 = dot.GetComponent<Dot>().NextDot;

        //Debug.Log(d1.position);

        //Il faut au moins D1 et D2
        if (d2 == null)
            return;

        Transform d3 = d2.GetComponent<Dot>().NextDot;

        Vector3 p0,p1,p2,p3;

        //On a au moins p1 et p2
        p1 = d1.position;
        p2 = d2.position;

        if (d0 != null)
            p0 = d0.position;
        else
            p0 = (p1 - p2) + p1; //On prolonge p1 dans la direction p2p1 pour avoir p0;

        if (d3 != null)
            p3 = d3.position;
        else
            p3 = (p2 - p1) + p2; //On prolonge p2 dans la direction p1p2 pour avoir p3;

        makeSpline(p0, p1, p2, p3);
        LineRenderer lr = d1.GetComponent<LineRenderer>();
        lr.positionCount = SplinePoints.Count;
        for(int i=0;i< SplinePoints.Count; i++)
            lr.SetPosition(i, SplinePoints[i]);
    }

    private const float SPLINE_SEGMENT_PIXEL_SIZE = 10;
    private System.Collections.Generic.List<Vector3> SplinePoints = new System.Collections.Generic.List<Vector3>();

    /*
     * Updates SplinePoints Vectors, to be set to a line renderer for instance
     * Warning : variable number of spline points, iterative algorithm.
     */
    void makeSpline(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3) 
    {
        float t0 = 0.0f;
        float t1 = GetT(t0, p0, p1);
        float t2 = GetT(t1, p1, p2);
        float t3 = GetT(t2, p2, p3);

        Vector3 p1Screen = Camera.main.WorldToScreenPoint(p1);
        Vector3 p2Screen = Camera.main.WorldToScreenPoint(p2);
        float distPixP1P2 = Vector3.Distance(p1Screen, p2Screen);
        //Debug.Log(distPixP1P2);
        int nbPointsSpline = (int)Mathf.Ceil(distPixP1P2 / SPLINE_SEGMENT_PIXEL_SIZE);
        //Debug.Log(nbPointsSpline);

        SplinePoints.Clear();
        for (float t = t1; t < t2; t += ((t2 - t1) / (float)(nbPointsSpline)))
        {
            Vector2 A1 = (t1 - t) / (t1 - t0) * p0 + (t - t0) / (t1 - t0) * p1;
            Vector2 A2 = (t2 - t) / (t2 - t1) * p1 + (t - t1) / (t2 - t1) * p2;
            Vector2 A3 = (t3 - t) / (t3 - t2) * p2 + (t - t2) / (t3 - t2) * p3;

            Vector2 B1 = (t2 - t) / (t2 - t0) * A1 + (t - t0) / (t2 - t0) * A2;
            Vector2 B2 = (t3 - t) / (t3 - t1) * A2 + (t - t1) / (t3 - t1) * A3;

            Vector2 C = (t2 - t) / (t2 - t1) * B1 + (t - t1) / (t2 - t1) * B2;

            SplinePoints.Add(new Vector3(C.x,C.y,2));
        }

        //Fix because not always adding last point to spline (t>t2 exit condition)
        //if((p2-SplinePoints[SplinePoints.Count- 1]).magnitude > (p2 - p1).magnitude / (nbPointsSpline*10))
        {
            SplinePoints.Add(new Vector3(p2.x, p2.y, 2));
        }
           

    }

    private const float alpha = 1.0f; //Chordal catmull rom
    float GetT(float t, Vector3 p0, Vector3 p1)
    {
        float a = Mathf.Pow((p1.x - p0.x), 2.0f) + Mathf.Pow((p1.y - p0.y), 2.0f);
        float b = Mathf.Pow(a, 0.5f);
        float c = Mathf.Pow(b, alpha);
        return (c + t);
    }
}

