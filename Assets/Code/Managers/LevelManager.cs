using System.Collections;
using System.Collections.Generic;
using Fusion;
using FusionExamples.FusionHelpers;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : NetworkSceneManagerBase
{
    [HideInInspector]
    public FusionLauncher Launcher;
    [SerializeField] private SceneTransitionManager sceneTransitionManager;
    private Scene _loadedScene;

    public void ResetLoadedScene()
    {
        //sceneTransitionManager.ResetLastLevelsIndex();
        _loadedScene = default;
    }

    protected override IEnumerator SwitchScene(SceneRef prevScene, SceneRef newScene, FinishedLoadingDelegate finished)
    {
        //GameManager.Instance.SetGameState(GameManager.GameState.Loading);
        sceneTransitionManager.StartLoadingScreen();
        Debug.Log($"Switching Scene from {prevScene} to {newScene}");
        if (newScene <= 0)
        {
            finished(new List<NetworkObject>());
            yield break;
        }

        yield return new WaitForSeconds(1.0f);

        Launcher.SetConnectionStatus(FusionLauncher.ConnectionStatus.Loading, "");

        yield return null;
        Debug.Log($"Start loading scene {newScene} in single peer mode");

        if (_loadedScene != default)
        {
            Debug.Log($"Unloading Scene {_loadedScene.buildIndex}");
            yield return SceneManager.UnloadSceneAsync(_loadedScene);
        }

        _loadedScene = default;
        Debug.Log($"Loading scene {newScene}");

        List<NetworkObject> sceneObjects = new List<NetworkObject>();
        if (newScene >= 0)
        {
            yield return SceneManager.LoadSceneAsync(newScene);
            _loadedScene = SceneManager.GetSceneByBuildIndex(newScene);
            Debug.Log($"Loaded scene {newScene}: {_loadedScene}");
            sceneObjects = FindNetworkObjects(_loadedScene, disable: false);
        }

        // Delay one frame
        yield return null;

        Launcher.SetConnectionStatus(FusionLauncher.ConnectionStatus.Loaded, "");

        yield return new WaitForSeconds(1);

        Debug.Log($"Switched Scene from {prevScene} to {newScene} - loaded {sceneObjects.Count} scene objects");
        finished(sceneObjects);
        yield return new WaitForSeconds(1f);
       // _loadingManager.FinishLoadingScreen();
    }
}
