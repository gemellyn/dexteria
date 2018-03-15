using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/**
 * Pour utiliser le manager de difficulté :
 * 1) appeler setActivity avec le nom du joueur et l'activité qu'on souhaite activer 
 *    les datas sont chargées et le modèle se met à jour automatiquement
 * 2) appeler getDiffParams avec le bon numéro de niveau (on commence à 0 puis on incrémente à chaque fois
 *    que le joueur gagne ou perd le challenge0) pour avoir les paramètres de difficulté
 * 3) appeler addTry pour ajouter un nouvel du joueur au modèle et le sauver 
 */

[RequireComponent(typeof(GameDifficultyModel))]
public class GameDifficultyManager : MonoBehaviour {

    public enum GDActivityEnum
    {
        TRACE,
        SIMON
    }

    abstract class GDActivity
    {
        public GDActivityEnum ActivityEnum;
        public string Name;
        public abstract double[] getParams(GameDifficultyModel model, double difficulty);
    }

    class TraceActivity : GDActivity
    {
        public TraceActivity()
        {
            Name = "Trace";
            ActivityEnum = GDActivityEnum.TRACE;
        }

        public override double[] getParams(GameDifficultyModel model, double difficulty)
        {
            //Deux params : temps et complexité du tracé
            double[] vars = new double[2];
            vars[0] = 0; //celle la on sait pas on va la déterminer avec le modèle
            vars[1] = difficulty; //On met la complexité à la meem valeur que la diff voulue
            vars[0] = model.getDiffParameter(difficulty, vars, 0); //On utilise le modèle pour choisir la seul variable pas bloquée
            return vars;
        }
    }

    class SimonActivity : GDActivity
    {
        public SimonActivity()
        {
            Name = "Simon";
            ActivityEnum = GDActivityEnum.SIMON;
        }
                
        public override double[] getParams(GameDifficultyModel model, double difficulty)
        {
            //Juste complexité du rythme
            double[] vars = new double[1];
            vars[0] = model.getDiffParameter(difficulty); 
            return vars;
        }
    }

    public AnimationCurve DifficultyCurveLearning; //Courbe au début progressive
    public int NbStepsLearning; //Nombre d'essais pour l'apprentissage
    public AnimationCurve [] DifficultyCurvePlaying; //Courbe en jeu, qu'on répète, après l'apprentissage
    public int NbStepsPlaying; //Nombre d'essais pour le jeu (on répète, sert à scaler la courbe)
    private int DiffCurvePlayingChosen = 0; //La courbe de difficulté qu'on utiliser pour la phase après learning
  
    GameDifficultyModel Model;
    GDActivity Activity;

    private void setDiffCurve(int diffCurve)
    {
        DiffCurvePlayingChosen = diffCurve;
    }
    
    /**
     * Permet de sélectionner l'activite à débuter
     * On met à jour le modèle et la courbe de difficulté
     */
    public void setActivity(string playerId, GDActivityEnum activity)
    {
        //Si c'est la meme, on ne touche a rien
        if (activity == Activity.ActivityEnum)
            return;

        switch (activity)
        {
            case GDActivityEnum.TRACE: Activity = new TraceActivity(); break;
            case GDActivityEnum.SIMON: Activity = new SimonActivity(); break;
            default: break;
        }

        //On met à jour le profil
        Model.setProfile(playerId, Activity.Name);

        //On tire une courbe de difficulté au hasard
        setDiffCurve(Random.Range(0, DifficultyCurvePlaying.Length));
    }
    
    /**
     * Ajoute un essai au modèle et le sauve
     * diffVars : valeur des paramètres du challenge que le joueur vient de tenter de réussir
     *            utiliser toujours le même ordre pour les différents paramètres d'un meme challenge
     * win : si le joueur a réussi ou pas son essai
     */
    public void addTry(double[] diffVars, bool win)
    {
        Model.addTry(diffVars, win, true, true);
    }

    /**
     * Donne la valeur des paramètres de l'activité,
     * en fonction du numéro de niveau.
     * On part du niveau 0, et à chaque nouveau challenge, demander
     * par exemple un nouveau niveau incrémenté.
     */
    public double[] getDiffParams(int numLevel)
    {
        //on regarde dans quelle courbe on tombe
        AnimationCurve ac = DifficultyCurveLearning;
        int numStepInCurve = numLevel;
        int nbStepOfCurve = NbStepsLearning;
        if (numLevel >= NbStepsLearning)
        {
            ac = DifficultyCurvePlaying[0];
            numStepInCurve -= NbStepsLearning;
            numStepInCurve = numStepInCurve % NbStepsPlaying;
            nbStepOfCurve = NbStepsPlaying;
        }

        //On récup la difficulté voulue
        double difficulty = ac.Evaluate((float)numStepInCurve / (float)nbStepOfCurve);

        //On construit le tableau en fonciton de l'activité
        return Activity.getParams(Model, difficulty);
    }



}
