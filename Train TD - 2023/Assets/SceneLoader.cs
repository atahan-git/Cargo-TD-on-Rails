using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour {
    public static SceneLoader s;
    
    public SceneReference mainScene;
    
    
    [SerializeField]
    private SceneReference initialScene;
    
    public bool autoOpenProfiles;
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        Debug.Log("Constant Objects Singleton Reset");
        s = null;
    }
    
    private void Awake() {
        if (s == null) {
            s = this;
            DontDestroyOnLoad(gameObject);

            if (SceneManager.GetActiveScene().path == initialScene.ScenePath) {
                LoadScene(mainScene);
            } else {
                loadingScreen.SetActive(false);
            }
        } else if(s!= this){
            loadingScreen.SetActive(false);
            Destroy(gameObject);
        }
    }


    public GameObject loadingScreen;
    public GameObject loadingText;
    public CanvasGroup canvasGroup;

    public void ForceReloadScene() {
        LoadScene(mainScene,true);
    }

    public static float loadingProgress;
    public bool isLoading = false;
    private void LoadScene(SceneReference sceneReference, bool isForced = false) {
        if (!isLoading || isForced) {
            StopAllCoroutines();
            StartCoroutine(StartLoad(sceneReference));
        }
    }

    public float currentFadeValue = 0f;
    IEnumerator StartLoad(SceneReference sceneReference) {
        isLoading = true;
        loadingProgress = 0;
        loadingScreen.SetActive(true);
        yield return StartCoroutine(FadeLoadingScreen(currentFadeValue,1, 0.5f));

        var operation = SceneManager.LoadSceneAsync(sceneReference.ScenePath, LoadSceneMode.Single);
        while (!operation.isDone) {
            loadingProgress = operation.progress;
            loadingProgress = Mathf.Clamp(loadingProgress, 0f,1f);
            yield return null;
        }

        yield return StartCoroutine(FadeLoadingScreen(1,0, 0.5f));
        loadingScreen.SetActive(false);
        isLoading = false;
    }

    IEnumerator FadeLoadingScreen(float startValue, float targetValue, float duration)
    {
        yield return null;
        float time = 0;

        while (time < duration)
        {
            canvasGroup.alpha = Mathf.Lerp(startValue, targetValue, time / duration);
            currentFadeValue = canvasGroup.alpha;
            time += Time.unscaledDeltaTime;
            yield return null;
        }
        canvasGroup.alpha = targetValue;
        yield return null;
    }
}
