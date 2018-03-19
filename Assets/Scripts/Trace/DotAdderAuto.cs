using UnityEngine;
using System.Collections;

[RequireComponent(typeof(DotManager))]
public class DotAdderAuto : MonoBehaviour {

    private DotManager dotManager;

    //Paramètres de génération du tracé
    public int nbTriesMax = 3000;
    public int nbTriesMin = 20; //Pour maximiser la variance du tracé
    public int nbDots = 90;

    public int freqNewAngle = 6; //Tous les combien de dots on fait un changement d'angle
    public float minAngleBase = 10; //Angle minimum de la trace
    public float maxAngleBase = 30; //Angle maximum de la trace
    public float probaInverseAngle = 0.5f; //Proba de changer de tourner dans l'autre sens
    public float probaGrosAngle = 0.3f; //Proba d'avoir un angle plus fort, une fois (casse le tracé)
    public float minGrosAngle = 30;
    public float maxGrosAngle = 50;
    public float probaLigneDroite = 0.1f;
    
    //Variables de la génération en cours
    private Vector3[] generatedDots;
    private Vector3[] finalDots;
    private int nbGeneratedDots;
    private float sommeAngle;
    private float sommeDist;
    private float sensAngle;
    private float angle;
    private float distBtwDots;
    private float varianceMax;
    private float boostAngle;
    private int nbLigneDroite;
    private bool canLigneDroite;

    // Use this for initialization
    void Awake () {
        dotManager = GetComponent<DotManager>();
        generatedDots = new Vector3[nbDots];
        finalDots = new Vector3[nbDots];
    }

    

    public void generate(bool inParallel = false)
    {
        this.varianceMax = 0.0f;
        dotManager.cleanDots();
        if (nbDots != generatedDots.Length)
        {
            generatedDots = new Vector3[nbDots];
            finalDots = new Vector3[nbDots];
        }

        StartCoroutine("generateCo");
    }

    public IEnumerator generateCo()
    {
        yield return new WaitForSeconds(0.1f);
    
        bool inViewPort = false;
        int c = 0;
        for (; c < nbTriesMin || (c < nbTriesMax && !inViewPort); c++)
        {
            generateOnePath();
            
            //On centre le tracé en 0 (comme ca il tourne mieux et c'est mieux)
            Vector3 bary = getBarycentreDots();
            for (int i = 0; i < nbGeneratedDots; i++)
                generatedDots[i] -= bary;

            //On place la camera en (0,0), le nouveau barycentre du groupe, pour les tests de visibilité et l'affichage
            Camera.main.transform.position = new Vector3(0, 0, Camera.main.transform.position.z);

            inViewPort = true;
            if (isOutOfViewPort())
            {
                inViewPort = false;
                //Debug.Log("Out of viewport");

                //On teste 20 angles (rot de 18 deg)
                int r = 0;
                Quaternion rot = Quaternion.AngleAxis(18, new Vector3(0, 0, 1));
                for (; r < 20 && !inViewPort; r++)
                {
                    for (int i = 0; i < nbGeneratedDots; i++)
                        generatedDots[i] = rot * generatedDots[i];
                    if (!isOutOfViewPort())
                        inViewPort = true;
                }

                /*Debug.Log("Nb Rotate :" + r);
                if (inViewPort)
                    Debug.Log("Rotate Worked !");*/

            }

            if (inViewPort)
            {
                float variance = getVariance(new Vector3(0,0,0));
                if (variance > varianceMax)
                {
                    for (int i = 0; i < nbGeneratedDots; i++)
                        finalDots[i] = generatedDots[i];
                    varianceMax = variance;
                }
            }

        }

        Debug.Log(c+" tries to generate dots");

        //Toujours dans la partie basse droite : que les doigts genent le moins possible la visu
        if (finalDots.Length >= 1)
        {

            if (finalDots[0].x < 0)
            {
                //Bary en 0, on inverse x
                for (int i = 0; i < nbGeneratedDots; i++)
                    finalDots[i].x = -finalDots[i].x;
            }

            if (finalDots[0].y > 0)
            {
                //Bary en 0, on inverse x
                for (int i = 0; i < nbGeneratedDots; i++)
                    finalDots[i].y = -finalDots[i].y;
            }

        }

        //Ajouter tous les points si ok !!
        dotManager.cleanDots();

        //On les ajoute dans le sens inverse pour que le dernier soit au dessus
        for (int i = 0; i < nbGeneratedDots; i++)
            dotManager.addDot(finalDots[i], nbGeneratedDots-i);

        dotManager.validateAllDots(false);
        dotManager.showAllPath(false);
        dotManager.showAllDots(false);
        dotManager.showDot(0, true, GetComponent<DotPlayerController>().nbNextPointShow);
        dotManager.showPath(0, true, Mathf.Max(GetComponent<DotPlayerController>().nbNextPointShow - 1, 0));
        
        yield return null;
    }
    
    private void generateOnePath()
    {
        this.sommeAngle = 0.0f;
        this.angle = 0.0f; 
        this.distBtwDots = 1.3f;
        this.sensAngle = 1.0f;
        this.sommeDist = 0.0f;
        this.boostAngle = 0.0f;
        
        Vector3 spawnPos = new Vector3(0.1f, 0.5f, 10);
        spawnPos = Camera.main.ViewportToWorldPoint(spawnPos);
        generatedDots[0] = spawnPos;
        nbGeneratedDots = 1;
        nbLigneDroite = 4;


        for (int i = 1; i < nbDots; i++)
            spawnNextPoint(nbDots - i);
     
    }
    
    Vector3 spawnNextPoint(int nbPointsRemaining)
    {
        Vector3 dir = Vector3.right;
        Vector3 lastPoint = generatedDots[nbGeneratedDots-1];

        if (nbGeneratedDots >= 2)
        { 
            Vector3 A = generatedDots[nbGeneratedDots - 2];
            dir = (lastPoint - A).normalized;
        }

        if (sommeDist >= freqNewAngle * distBtwDots || angle == 0.0f)
        {
            sommeAngle = 0.0f;
            sommeDist = 0.0f;

            angle = Random.Range(minAngleBase, maxAngleBase);
            
            if (Random.Range(0.0f, 1.0f) < probaInverseAngle && nbPointsRemaining > 4)
                sensAngle = -sensAngle;

            if (Random.Range(0.0f, 1.0f) < probaGrosAngle && nbPointsRemaining > 4)
            {
                boostAngle = Random.Range(minGrosAngle, maxGrosAngle);
                float angleSafe = 40;
                if (boostAngle > 180 - angleSafe && boostAngle < 180)
                    boostAngle = 180 - angleSafe;
                if (boostAngle > 180 && boostAngle < 180 + angleSafe)
                    boostAngle = 180 + angleSafe;
                //Debug.Log(boostAngle);
            }

            if (Random.Range(0.0f, 1.0f) < probaLigneDroite && canLigneDroite)
            {
                nbLigneDroite = 2;
                //Debug.Log("ligne !! "+ nbLigneDroite);
            }
            //Debug.Log("poiet" + nbLigneDroite);


            if (nbLigneDroite > 0)
            {
                angle = 0;
                boostAngle = 0;
                nbLigneDroite--;
                canLigneDroite = false;
            }
            else
            {
                canLigneDroite = true;
            }
               

        }

        dir = Quaternion.AngleAxis(sensAngle * Mathf.Max(angle,boostAngle), new Vector3(0,0,1)) * dir;
        boostAngle = 0;

        Vector3 spawnPos = lastPoint + dir * distBtwDots;
        generatedDots[nbGeneratedDots] = spawnPos;
        nbGeneratedDots++;

        sommeAngle += Mathf.Abs(angle);
        sommeDist += distBtwDots;

        return spawnPos;
    }

    private float getVariance(Vector3 barycentre)
    {
        float vari = 0.0f;
        for (int i = 0; i < nbGeneratedDots; i++)
            vari += Vector3.SqrMagnitude(barycentre - generatedDots[i]);
        return vari / (float)nbGeneratedDots;
    }

    private Vector3 getBarycentreDots()
    {
        Vector3 bary = new Vector3(0, 0, 0);
        for (int i = 0; i < nbGeneratedDots; i++)
            bary += generatedDots[i];
        return bary / (float)nbGeneratedDots;
    }

    private bool isOutOfViewPort()
    {
        for (int i = 0; i < nbGeneratedDots; i++)
        {
            Vector3 pos = Camera.main.WorldToViewportPoint(generatedDots[i]);
            if (pos.x < 0.1 || pos.x > 0.9 || pos.y < 0.1 || pos.y > 0.9)
                return true;
        }
        return false;
    }


}
