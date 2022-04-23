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
public delegate void FinishedLoadingDelegate(IEnumerable<NetworkObject> sceneObjects);
public class LevelManager : NetworkSceneManagerBase
{
    [SerializeField] private Level currentLevel;
    [SerializeField] private Scene currentScene;
    [SerializeField] private SceneTransitionManager sceneTransitionManager;
    [SerializeField] private SceneReference mainMenuScene;
    [SerializeField] private SceneReference lobbyScene;
    public SceneReference LobbyScene { get => lobbyScene; set => this.lobbyScene = value; }
    
    [SerializeField] private SceneReference[] levels;
    // ReSharper disable Unity.PerformanceAnalysis
    protected override IEnumerator SwitchScene(SceneRef prevScene, SceneRef newScene, FinishedLoadingDelegate finished)
    {
        GameManager.Instance.Status = GameManager.GameStatus.Loading;

        sceneTransitionManager.StartLoadingScreen();
        if (newScene <= 0)
        {
            finished(new List<NetworkObject>());
            yield break;
        }

        yield return new WaitForSeconds(1.0f);	
        /*
        if (currentScene != default)
        {
            Debug.Log($"Unloading Scene {currentScene.buildIndex}");
            yield return SceneManager.UnloadSceneAsync(currentScene);
        }
        */

        currentScene = default;
        List<NetworkObject> sceneObjects = new List<NetworkObject>();
        if (newScene >= 0)
        {
            yield return SceneManager.LoadSceneAsync(newScene);
            currentScene = SceneManager.GetSceneByBuildIndex(newScene);
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
        sceneTransitionManager.FinishLoadingScreen();
        DebugLogMessage.Log(Color.green,$"Loaded {newScene}\n SceneObject Count = {sceneObjects.Count}");
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
    public void ResetLevel()
    {
        if (currentLevel != null)
        {
            currentLevel.OnLoadMap1();
        }
        currentScene = default;
    }
}
