using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GameDifficultyModel))]
public class FakePlayer : MonoBehaviour {

    GameDifficultyModel gd;

    public AnimationCurve diffFromParameter;
    public AnimationCurve parameterWithTime;
    public int nbSamples = 100;

	// Use this for initialization
	void Start () {
        gd = GetComponent<GameDifficultyModel>();
        gd.setProfile("fakePlayer","fakeActivity");
        simulate();
        gd.updateModel(true);
        Debug.Log("Model quality : "+gd.getModelQuality());
	}

    void simulate()
    {
        Debug.Log("Simulation d'essais");
        for(int i=0;i< nbSamples; i++)
        {
            float t = (float)i / (float)nbSamples;
            float diffParam = parameterWithTime.Evaluate(t);
            float diffObj = diffFromParameter.Evaluate(diffParam);

            bool res = !(Random.Range(0.0f, 1.0f) < diffObj);
            double[] vars = new double[1];
            vars[0] = diffParam;
            gd.addTry(vars, res,false,false);
        }
        Debug.Log("Simule et ajoute " + nbSamples + " tries");

        gd.saveToDisk();
    }
}
