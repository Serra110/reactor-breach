using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SplashScreen : MonoBehaviour
{
    public Image logo;
    public float fadeSpeed = 1.5f;
    public float waitTime = 2f;

    void Start()
    {
        StartCoroutine(PlaySplash());
    }

    System.Collections.IEnumerator PlaySplash()
    {
        yield return StartCoroutine(Fade(0, 1));

        yield return new WaitForSeconds(waitTime);

        yield return StartCoroutine(Fade(1, 0));

        SceneManager.LoadScene("TitleScreen");
    }

    System.Collections.IEnumerator Fade(float from, float to)
    {
        float t = 0;
        Color c = logo.color;
        float duration = 1f / fadeSpeed;

        while (t < duration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(from, to, t / duration);
            logo.color = new Color(c.r, c.g, c.b, alpha);
            yield return null;
        }

        logo.color = new Color(c.r, c.g, c.b, to);
    }
}