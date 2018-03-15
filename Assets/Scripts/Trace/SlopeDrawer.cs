using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SlopeDrawer : MonoBehaviour
{

    //Use GameObject in 3d space as your points or define array with desired points
    public GameObject[] points;

    //Store points on the Catmull curve so we can visualize them
    List<Vector2> newPoints = new List<Vector2>();

    //How many points you want on the curve
    float amountOfPoints = 10.0f;

    //set from 0-1
    public float alpha = 0.5f;

    /////////////////////////////

    void Update()
    {
        CatmulRom();
    }

    void CatmulRom()
    {
        newPoints.Clear();

        Vector2 p0 = new Vector2(points[0].transform.position.x, points[0].transform.position.y);
        Vector2 p1 = new Vector2(points[1].transform.position.x, points[1].transform.position.y);
        Vector2 p2 = new Vector2(points[2].transform.position.x, points[2].transform.position.y);
        Vector2 p3 = new Vector2(points[3].transform.position.x, points[3].transform.position.y);

        float t0 = 0.0f;
        float t1 = GetT(t0, p0, p1);
        float t2 = GetT(t1, p1, p2);
        float t3 = GetT(t2, p2, p3);

        for (float t = t1; t < t2; t += ((t2 - t1) / amountOfPoints))
        {
            Vector2 A1 = (t1 - t) / (t1 - t0) * p0 + (t - t0) / (t1 - t0) * p1;
            Vector2 A2 = (t2 - t) / (t2 - t1) * p1 + (t - t1) / (t2 - t1) * p2;
            Vector2 A3 = (t3 - t) / (t3 - t2) * p2 + (t - t2) / (t3 - t2) * p3;

            Vector2 B1 = (t2 - t) / (t2 - t0) * A1 + (t - t0) / (t2 - t0) * A2;
            Vector2 B2 = (t3 - t) / (t3 - t1) * A2 + (t - t1) / (t3 - t1) * A3;

            Vector2 C = (t2 - t) / (t2 - t1) * B1 + (t - t1) / (t2 - t1) * B2;

            newPoints.Add(C);
        }
    }

    float GetT(float t, Vector2 p0, Vector2 p1)
    {
        float a = Mathf.Pow((p1.x - p0.x), 2.0f) + Mathf.Pow((p1.y - p0.y), 2.0f);
        float b = Mathf.Pow(a, 0.5f);
        float c = Mathf.Pow(b, alpha);

        return (c + t);
    }

    //Visualize the points
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        foreach (Vector2 temp in newPoints)
        {
            Vector3 pos = new Vector3(temp.x, temp.y, 0);
            Gizmos.DrawSphere(pos, 0.3f);
        }
    }
}