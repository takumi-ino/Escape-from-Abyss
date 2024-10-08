using UnityEngine;
using UnityEngine.UI;

public class SceneFadeManader : MonoBehaviour
{
    public static bool isFadeInstance = false;

    public bool isFadeIn = false;
    public bool isFadeOut = false;

    public float alpha = 0.0f;
    public float fadeSpeed = 5.2f;

    void Start()
    {
        if (!isFadeInstance)
        {
            DontDestroyOnLoad(this);
            isFadeInstance = true;
        }
        else
        {
            Destroy(this);
        }
    }

    void Update()
    {
        if (isFadeIn)
        {
            alpha -= Time.deltaTime / fadeSpeed;

            if (alpha <= 0.0f)
            {
                isFadeIn = false;
                alpha = 0.0f;
            }
            
            GetComponentInChildren<Image>().color = new Color(0.0f, 0.0f, 0.0f, alpha);
        }
        else if (isFadeOut)
        {
            alpha += Time.deltaTime / fadeSpeed;

            if (alpha >= 1.0f)
            {
                isFadeOut = false;
                alpha = 0.0f;
            }
            
            GetComponentInChildren<Image>().color = new Color(0.0f, 0.0f, 0.0f, alpha);
        }
    }

    public void fadeIn()
    {
        isFadeIn = true;
        isFadeOut = false;
    }

    public void fadeOut()
    {
        isFadeOut = true;
        isFadeIn = false;
    }
}