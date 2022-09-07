using System;
using Fusion;
using Fusion.Sockets;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Serialization;
using Behaviour = Fusion.Behaviour;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEditor;
using Fusion.Editor;
#endif

[DisallowMultipleComponent]
[ScriptHelp(BackColor = EditorHeaderBackColor.Steel)]
public class NetworkClientDebug : Behaviour
{
  
  /// <summary>
  /// Selection for how <see cref="NetworkDebugStart"/> will behave at startup.
  /// </summary>
  public enum StartModes {
    UserInterface,
    Automatic,
    Manual
  }

  /// <summary>
  /// The current stage of connection or shutdown.
  /// </summary>
  public enum Stage {
    Disconnected,
    StartingUp,
    UnloadOriginalScene,
    ConnectingServer,
    ConnectingClients,
    AllConnected,
  }

  /// <summary>
  /// Supply a Prefab or a scene object which has the <see cref="NetworkRunner"/> component on it,
  /// as well as any runner dependent components which implement <see cref="INetworkRunnerCallbacks"/>,
  /// such as <see cref="NetworkEvents"/> or your own custom INetworkInput implementations.
  /// </summary>
  [InlineHelp]
  [WarnIf(nameof(RunnerPrefab), 0, "No " + nameof(RunnerPrefab) + " supplied. Will search for a " + nameof(NetworkRunner) + " in the scene at startup.")]
  public NetworkRunner RunnerPrefab;

  /// <summary>
  /// The port that server/host <see cref="NetworkRunner"/> will use.
  /// </summary>
  [InlineHelp]
  public ushort ServerPort = 27015;

  /// <summary>
  /// The default room name to use when connecting to photon cloud.
  /// </summary>
  [InlineHelp]
  public string DefaultRoomName = ""; // empty/null means Random Room Name

  /// <summary>
  /// Will automatically enable <see cref="FusionStats"/> once peers have finished connecting.
  /// </summary>
  [InlineHelp]
  public bool AlwaysShowStats = false;

  [NonSerialized]
  NetworkRunner _server;

  /// <summary>
  /// The Scene that will be loaded after network shutdown completes (all peers have disconnected).
  /// If this field is null or invalid, will be set to the current scene when <see cref="NetworkDebugStart"/> runs Awake().
  /// </summary>
  [InlineHelp]
  [ScenePath]
  [MultiPropertyDrawersFix]
  public string InitialScenePath;
  static string _initialScenePath;

  /// <summary>
  /// Indicates which step of the startup process <see cref="NetworkDebugStart"/> is currently in.
  /// </summary>
  [InlineHelp]
  [SerializeField]
  [EditorDisabled]
  [MultiPropertyDrawersFix]
  protected Stage _currentStage;

  /// <summary>
  /// Indicates which step of the startup process <see cref="NetworkDebugStart"/> is currently in.
  /// </summary>
  public Stage CurrentStage {
    get => _currentStage;
    internal set {
      _currentStage = value;
    #if UNITY_EDITOR
      // Hack to force an inspector refresh when this value changes, as it affects which buttons are shown.
      EditorUtility.SetDirty(this);
    #endif
    }
  }

  /// <summary>
  /// The index number used for the last created peer.
  /// </summary>
  public int LastCreatedClientIndex { get; internal set; }

  /// <summary>
  /// The server mode that was used for initial startup. Used to inform UI which client modes should be available.
  /// </summary>
  public GameMode CurrentServerMode { get; internal set; }

  protected bool CanAddClients          => CurrentStage == Stage.AllConnected && CurrentServerMode > 0 && CurrentServerMode != GameMode.Shared && CurrentServerMode != GameMode.Single;
  protected bool CanAddSharedClients    => CurrentStage == Stage.AllConnected && CurrentServerMode > 0 && CurrentServerMode == GameMode.Shared;
  protected bool IsShutdown             => CurrentStage == Stage.Disconnected;
  protected bool IsShutdownAndMultiPeer => CurrentStage == Stage.Disconnected && UsingMultiPeerMode;

  protected bool UsingMultiPeerMode => NetworkProjectConfig.Global.PeerMode == NetworkProjectConfig.PeerModes.Multiple;
    public virtual void StartServerPlusClients(int clientCount) {
        if (NetworkProjectConfig.Global.PeerMode == NetworkProjectConfig.PeerModes.Multiple) {
            if (TryGetSceneRef(out var sceneRef)) {
                StartCoroutine(StartWithClients(GameMode.Server, sceneRef, clientCount));
            }
        } else {
            Debug.LogWarning($"Unable to start multiple {nameof(NetworkRunner)}s in Unique Instance mode.");
        }
    }
    protected bool TryGetSceneRef(out SceneRef sceneRef) {
        var activeScene = SceneManager.GetActiveScene();
        if (activeScene.buildIndex < 0 || activeScene.buildIndex >= SceneManager.sceneCountInBuildSettings) {
            sceneRef = default;
            return false;
        } else {
            sceneRef = activeScene.buildIndex;
            return true;
        }
    }
    
  protected IEnumerator StartWithClients(GameMode serverMode, SceneRef sceneRef, int clientCount) 
  {
    // Avoid double clicks or disallow multiple startup calls.
    if (CurrentStage != Stage.Disconnected) {
      yield break;
    }

    bool includesServerStart = serverMode != GameMode.Shared && serverMode != GameMode.Client && serverMode != GameMode.AutoHostOrClient;

    if (!includesServerStart && clientCount == 0) {
      Debug.LogError($"{nameof(GameMode)} is set to {serverMode}, and {nameof(clientCount)} is set to zero. Starting no network runners.");
      yield break;
    }

    CurrentStage = Stage.StartingUp;

    var currentScene = SceneManager.GetActiveScene();

    // must have a runner
    if (!RunnerPrefab) {
      Debug.LogError($"{nameof(RunnerPrefab)} not set, can't perform debug start.");
      yield break;
    }

    // Clone the RunnerPrefab so we can safely delete the startup scene (the prefab might be part of it, rather than an asset).
    RunnerPrefab = Instantiate(RunnerPrefab);
    DontDestroyOnLoad(RunnerPrefab);
    RunnerPrefab.name = "Temporary Runner Prefab";

    // Single-peer can't start more than one peer. Validate clientCount to make sure it complies with current PeerMode.
    var config = NetworkProjectConfig.Global;
    if (config.PeerMode != NetworkProjectConfig.PeerModes.Multiple) {
      int maxClientsAllowed = includesServerStart ? 0 : 1;
      if (clientCount > maxClientsAllowed) {
        Debug.LogWarning($"Instance mode must be set to {nameof(NetworkProjectConfig.PeerModes.Multiple)} to perform a debug start multiple peers. Restricting client count to {maxClientsAllowed}.");
        clientCount = maxClientsAllowed;
      }
    }

    // If NDS is starting more than 1 shared or auto client, they need to use the same Session Name, otherwise, they will end up on different Rooms
    // as Fusion creates a Random Session Name when no name is passed on the args
    if ((serverMode == GameMode.Shared || serverMode == GameMode.AutoHostOrClient) && clientCount > 1 && config.PeerMode == NetworkProjectConfig.PeerModes.Multiple) {
      DefaultRoomName = string.IsNullOrEmpty(DefaultRoomName) == false ? DefaultRoomName : Guid.NewGuid().ToString();
    }

    if (gameObject.transform.parent) {
      Debug.LogWarning($"{nameof(NetworkDebugStart)} can't be a child game object, un-parenting.");
      gameObject.transform.parent = null;
    }

    DontDestroyOnLoad(gameObject);
    CurrentServerMode = serverMode;

    // start server, just take address from it
    if (includesServerStart) {
      _server = Instantiate(RunnerPrefab);
      _server.name = serverMode.ToString();

      var serverTask = InitializeNetworkRunner(_server, serverMode, NetAddress.Any(ServerPort), sceneRef, (runner) => {
      #if FUSION_DEV
          var name = _server.name; // closures do not capture values, need a local var to save it
          Debug.Log($"Server NetworkRunner '{name}' started.");
      #endif
        // this action is called after InitializeNetworkRunner for the server has completed startup
        StartCoroutine(StartClients(clientCount, serverMode, sceneRef));
      });

      while(serverTask.IsCompleted == false) {
        yield return new WaitForEndOfFrame();
      }

      if (serverTask.IsFaulted) {
        ShutdownAll();
      }

    } else {
      StartCoroutine(StartClients(clientCount, serverMode, sceneRef));
    }

    // Add stats last, so any event systems that may be getting created are already in place.
    if (includesServerStart && AlwaysShowStats && serverMode != GameMode.Shared) {
      FusionStats.Create(runner: _server, screenLayout: FusionStats.DefaultLayouts.Left, objectLayout: FusionStats.DefaultLayouts.Left);
    }

  }
  protected IEnumerator StartClients(int clientCount, GameMode serverMode, SceneRef sceneRef = default) {

    var clientTasks = new List<Task>();

    CurrentStage = Stage.ConnectingClients;

    for (int i = 0; i < clientCount; ++i) {
      var clientTask =  AddClient(serverMode, sceneRef);
      clientTasks.Add(clientTask);
    }

    bool done;
    do {
      done = true;

      // yield until all tasks report as completed
      foreach (var task in clientTasks) {
        done &= task.IsCompleted;

        if (done == false) {
          break;
        }

        if (task.IsFaulted) {
          Debug.LogWarning(task.Exception);
        }
      }
      yield return new WaitForSeconds(0.5f);

    } while (done == false);

    CurrentStage = Stage.AllConnected;
    //ForceGUIUpdate();
  }
  protected virtual Task InitializeNetworkRunner(NetworkRunner runner, GameMode gameMode, NetAddress address, SceneRef scene, Action<NetworkRunner> initialized) {

    var sceneManager = runner.GetComponents(typeof(MonoBehaviour)).OfType<INetworkSceneManager>().FirstOrDefault();
    if (sceneManager == null) {
      Debug.Log($"NetworkRunner does not have any component implementing {nameof(INetworkSceneManager)} interface, adding {nameof(NetworkSceneManagerDefault)}.", runner);
      sceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>();
    }

    return runner.StartGame(new StartGameArgs {
      GameMode = gameMode,
      Address = address,
      Scene = scene,
      SessionName = DefaultRoomName,
      Initialized = initialized,
      SceneManager = sceneManager,
      ObjectPool = runner.GetComponent<INetworkObjectPool>(),
    });
  }
  
  [BehaviourButtonAction("Add Additional Client", conditionMember: nameof(CanAddClients))]
  public void AddClient() {
    if (TryGetSceneRef(out var sceneRef)) {
      AddClient(GameMode.Client, sceneRef);
    }
  }
  public void ShutdownAll() {
    foreach (var runner in NetworkRunner.Instances.ToList()) {
      if (runner != null && runner.IsRunning) {
        runner.Shutdown();
      }
    }

    SceneManager.LoadSceneAsync(_initialScenePath);
    // Destroy our DontDestroyOnLoad objects to finish the reset
    Destroy(RunnerPrefab.gameObject);
    Destroy(gameObject);
    CurrentStage = Stage.Disconnected;
    CurrentServerMode = 0;
  }
  public Task AddClient(GameMode serverMode, SceneRef sceneRef) {
    var client = Instantiate(RunnerPrefab);
    DontDestroyOnLoad(client);

    client.name = $"Client {(Char)(65 + LastCreatedClientIndex++)}";

    // if server mode is Shared or AutoHostOrClient, then game client mode is the same as the server, otherwise it is client
    var mode = GameMode.Client;
    switch (serverMode) {
      case GameMode.Shared:
      case GameMode.AutoHostOrClient:
        mode = serverMode;
        break;
    }

#if FUSION_DEV
        var clientTask = InitializeNetworkRunner(client, mode, NetAddress.Any(), sceneRef, (runner) => {
          var name = client.name; // closures do not capture values, need a local var to save it
          Debug.Log($"Client NetworkRunner '{name}' started.");
        });
#else
    var clientTask = InitializeNetworkRunner(client, mode, NetAddress.Any(), sceneRef, null);
#endif

    //clientTasks.Add(clientTask);

    // Add stats last, so that event systems that may be getting created are already in place.
    if (AlwaysShowStats && LastCreatedClientIndex == 0) {
      FusionStats.Create(runner: client, screenLayout: FusionStats.DefaultLayouts.Right, objectLayout: FusionStats.DefaultLayouts.Right);
    }

    return clientTask;
  }
}
