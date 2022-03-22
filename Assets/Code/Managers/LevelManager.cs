using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public enum MapIndex 
{
    MainMenu,
    Lobby,
    Map0,
    Map1,
};

public class LevelManager : NetworkSceneManagerBase
{
    [SerializeField] private Scene currentScene;
    public Scene CurrentScene { get => currentScene; set => this.currentScene = value; }
    
    [SerializeField] private SceneTransitionManager sceneTransitionManager;
    [SerializeField] private SceneReference mainMenuScene;
    [SerializeField] private SceneReference lobbyScene;
    public SceneReference LobbyScene { get => lobbyScene; set => this.lobbyScene = value; }
    
    [SerializeField] private SceneReference[] levels;
    protected override IEnumerator SwitchScene(SceneRef prevScene, SceneRef newScene, FinishedLoadingDelegate finished)
    {
        GameManager.Instance.Status = GameManager.GameStatus.Loading;
        Debug.Log($"Switching Scene from {prevScene} to {newScene}");

        sceneTransitionManager.StartLoadingScreen();
        if (newScene <= 0)
        {
            finished(new List<NetworkObject>());
            yield break;
        }

        yield return new WaitForSeconds(1.0f);	
        if (currentScene != default)
        {
            Debug.Log($"Unloading Scene {currentScene.buildIndex}");
            yield return SceneManager.UnloadSceneAsync(currentScene);
        }
        Debug.Log($"Loading scene {newScene}");
        currentScene = default;
        List<NetworkObject> sceneObjects = new List<NetworkObject>();
        if (newScene >= 0)
        {
            yield return SceneManager.LoadSceneAsync(newScene);
            currentScene = SceneManager.GetSceneByBuildIndex(newScene);
            Debug.Log($"Loaded scene {newScene}: {currentScene}");
            sceneObjects = FindNetworkObjects(currentScene, disable: false);
        }
        
        /*string path;
        switch ((MapIndex)(int)newScene)
        {
            case MapIndex.MainMenu: path = mainMenuScene; break;
            case MapIndex.Lobby: path = lobbyScene; break;
            default: path = levels[newScene - (int)MapIndex.Map0]; break;
        }	
        yield return SceneManager.LoadSceneAsync(path, LoadSceneMode.Single);
        var loadedScene = SceneManager.GetSceneByPath( path );
        Debug.Log($"Loaded scene {path}: {loadedScene}");
        sceneObjects = FindNetworkObjects(loadedScene, disable: false);*/

        // Delay one frame
        yield return null;
        yield return new WaitForSeconds(1);
        finished(sceneObjects);

        Debug.Log($"Switched Scene from {prevScene} to {newScene} - loaded {sceneObjects.Count} scene objects");

        sceneTransitionManager.FinishLoadingScreen();
    }
    public void ResetLoadedScene()
    {
        currentScene = default;
    }

    public void LoadMainMenu()
    {
        if (Application.isPlaying)
        {
            SceneManager.LoadSceneAsync(mainMenuScene);
        }
    }
    public void LoadLobbyMenu()
    {
        if (Application.isPlaying)
        {
            SceneManager.LoadSceneAsync(lobbyScene);
        }
    }
}
