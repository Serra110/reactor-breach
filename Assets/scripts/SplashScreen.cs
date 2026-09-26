using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SplashScreen : MonoBehaviour
{
    public Image logo;
    public float fadeSpeed = 1.5f;
    public float waitTime = 2f;

    private Coroutine _routine;
    private bool _loadingTitle;

    void Start()
    {
        _routine = StartCoroutine(PlaySplash());
    }

    void OnDestroy()
    {
        // FIX: o PlaySplash carregava o TitleScreen no fim. Se este objeto fosse
        // destruido entretanto (ou o utilizador mudasse de cena), a coroutine
        // continuava a correr e ainda disparava o LoadScene a meio de outra cena.
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }
    }

    System.Collections.IEnumerator PlaySplash()
    {
        yield return Fade(0, 1);

        yield return new WaitForSeconds(Mathf.Max(0f, waitTime));

        yield return Fade(1, 0);

        // Fade e um plain IEnumerator, por isso ja nao precisa de um wrapper
        // (StartCoroutine devolvia IEnumerator, o que forcava um wrapper extra).
        if (this == null || _loadingTitle)
            yield break;

        _loadingTitle = true;
        SceneManager.LoadScene("TitleScreen");
    }

    System.Collections.IEnumerator Fade(float from, float to)
    {
        if (logo == null)
            yield break;

        float t = 0;
        Color c = logo.color;
        float duration = Mathf.Max(0.01f, 1f / Mathf.Max(0.01f, fadeSpeed));

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
