using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : NetworkSceneManagerBase
{
    [SerializeField] private SceneTransitionManager sceneTransitionManager;
    [SerializeField] private SceneReference mainMenuScene;
    [SerializeField] private SceneReference lobbyScene;
    [SerializeField] private SceneReference map0;
    [SerializeField] private SceneReference map1;
    
    private Scene currentLoadedScene;
    
    protected override IEnumerator SwitchScene(SceneRef prevScene, SceneRef newScene, FinishedLoadingDelegate finished)
    {
        GameManager.Instance.Status = GameManager.GameStatus.Loading;
        sceneTransitionManager.StartLoadingScreen();
        Debug.Log($"Switching Scene from {prevScene} to {newScene}");
        if (newScene <= 0)
        {
            finished(new List<NetworkObject>());
            yield break;
        }

        yield return new WaitForSeconds(1.0f);

        //Launcher.SetConnectionStatus(FusionLauncher.ConnectionStatus.Loading, "");

        yield return null;
        Debug.Log($"Start loading scene {newScene} in single peer mode");

        if (currentLoadedScene != default)
        {
            Debug.Log($"Unloading Scene {currentLoadedScene.buildIndex}");
            yield return SceneManager.UnloadSceneAsync(currentLoadedScene);
        }

        currentLoadedScene = default;
        Debug.Log($"Loading scene {newScene}");

        List<NetworkObject> sceneObjects = new List<NetworkObject>();
        if (newScene >= 0)
        {
            yield return SceneManager.LoadSceneAsync(newScene);
            currentLoadedScene = SceneManager.GetSceneByBuildIndex(newScene);
            Debug.Log($"Loaded scene {newScene}: {currentLoadedScene}");
            sceneObjects = FindNetworkObjects(currentLoadedScene, disable: false);
        }

        // Delay one frame
        yield return null;

        //Launcher.SetConnectionStatus(FusionLauncher.ConnectionStatus.Loaded, "");

        yield return new WaitForSeconds(1);

        Debug.Log($"Switched Scene from {prevScene} to {newScene} - loaded {sceneObjects.Count} scene objects");
        finished(sceneObjects);
        yield return new WaitForSeconds(1f);
        sceneTransitionManager.FinishLoadingScreen();
    }
    public void ResetLoadedScene()
    {
        currentLoadedScene = default;
    }

    public void LoadMainMenu()
    {
        if (Application.isPlaying)
        {
            SceneManager.LoadSceneAsync(mainMenuScene);
        }
    }
}
