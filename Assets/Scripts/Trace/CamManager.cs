using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CamManager : MonoBehaviour {

    public int WantedWidthCm = 5; //(taille en CM de large)
    public Text txt;

    // Use this for initialization
    void Start () {

        float dpi = DisplayMetricsAndroid.XDPI > 0 ? (DisplayMetricsAndroid.XDPI+ DisplayMetricsAndroid.YDPI)/2.0f : Screen.dpi;
        Debug.Log("DPI: " + dpi);
        float widthPix = Screen.width;
        Debug.Log("Width in pix: " + widthPix);
        float screenWidthCm = (Screen.width / dpi) * 2.54f;
        Debug.Log("Screen with in cm is " + screenWidthCm);

        txt.text = dpi.ToString();

        //Si on est plus grand que ce qu'on veut, on modifie le rapport
        if (screenWidthCm > WantedWidthCm)
        {
            Debug.Log("Screen toooo big, changing viewport");
            float rapport = WantedWidthCm / screenWidthCm;
            GetComponent<Camera>().rect = new Rect((1- rapport)/2.0f, (1 - rapport) / 2.0f, rapport, rapport);
        }         
        
    }

    float GetDPI()
    {
        AndroidJavaClass activityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject activity = activityClass.GetStatic<AndroidJavaObject>("currentActivity");

        AndroidJavaObject metrics = new AndroidJavaObject("android.util.DisplayMetrics");
        activity.Call<AndroidJavaObject>("getWindowManager").Call<AndroidJavaObject>("getDefaultDisplay").Call("getMetrics", metrics);

        return (metrics.Get<float>("xdpi") + metrics.Get<float>("ydpi")) * 0.5f;
    }


    // Update is called once per frame
    void Update () {
	
	}
}
