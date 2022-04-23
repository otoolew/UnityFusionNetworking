using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    #region Scene Values
    [SerializeField] private CanvasGroup fadeScreen;
    public CanvasGroup FadeScreen { get => fadeScreen; set => fadeScreen = value; }

    [SerializeField] private float fadeDuration;
    public float FadeDuration { get => fadeDuration; set => fadeDuration = value; }

    [SerializeField] private bool isFading;
    public bool IsFading { get => isFading; set => isFading = value; }
    #endregion
    public void StartLoadingScreen()
    {
        FadeScreen.gameObject.SetActive(true);
        StartCoroutine(Fade(1));
    }
    public void FinishLoadingScreen()
    {
        StartCoroutine(Fade(0));
    }
    private IEnumerator Fade(float finalAlpha)
    {
        FadeScreen.gameObject.SetActive(true);
        isFading = true;
        FadeScreen.blocksRaycasts = true; // Blocks player Clicking on other Scene or UI GameObjects
        float fadeSpeed = Mathf.Abs(FadeScreen.alpha - finalAlpha) / fadeDuration;
        while (!Mathf.Approximately(FadeScreen.alpha, finalAlpha))
        {
            FadeScreen.alpha = Mathf.MoveTowards(FadeScreen.alpha, finalAlpha,
                fadeSpeed * Time.deltaTime);
            yield return null; //Lets the Coroutine finish
        }
        isFading = false;
        FadeScreen.blocksRaycasts = false;
        if (finalAlpha == 0)
        {
            FadeScreen.gameObject.SetActive(false);
        }
    }
}
