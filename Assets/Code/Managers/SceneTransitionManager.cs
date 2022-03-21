using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance;

    [SerializeField] private Animator transitionScreenAnimator;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this);
        }
        DontDestroyOnLoad(transform.parent);
    }

    /*public void ResetLastLevelsIndex()
    {
        _lastLevelIndex = 0;
    }

    public void LoadNextLevel(NetworkRunner runner)
    {
        _lastLevelIndex = _lastLevelIndex + 1 >= SceneManager.sceneCountInBuildSettings ? 1 : _lastLevelIndex + 1;
        string scenePath = System.IO.Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(_lastLevelIndex));
        runner.SetActiveScene(scenePath);
    }

    public void LoadRandomLevel(NetworkRunner runner)
    {
        int sceneIndex = Random.Range(1, SceneManager.sceneCountInBuildSettings);
        if (_lastLevelIndex == sceneIndex)
        {
            sceneIndex = sceneIndex + 1 >= SceneManager.sceneCountInBuildSettings ? sceneIndex - 1 : sceneIndex + 1;
        }
        _lastLevelIndex = sceneIndex;
        string scenePath = System.IO.Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(sceneIndex));
        runner.SetActiveScene(scenePath);
    }*/

    public void StartLoadingScreen()
    {
        transitionScreenAnimator.Play("In");
    }

    public void FinishLoadingScreen()
    {
        transitionScreenAnimator.Play("Out");
    }
}
