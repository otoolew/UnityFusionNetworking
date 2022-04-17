//--------------------------------------------------------------------------------------------------------------------//
// Author:  Bill O'Toole
// Date:    February 20, 2022
//--------------------------------------------------------------------------------------------------------------------//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using GameUI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
public class GameManager : MonoBehaviour, INetworkRunnerCallbacks
{
	#region Singleton ------------------------------------------------
	private static GameManager instance;
	public static GameManager Instance
	{
		get
		{
			if (instance == null)
				instance = FindObjectOfType<GameManager>();
			return instance;
		}
	}
	
	public static NetworkRunner MasterRunner;
	
	#endregion --------------------------------------------------------
	
	#region Network Runner --------------------------------------------
	
	private NetworkRunner runnerInstance;

	[SerializeField] private ConnectionStatus connectionStatus;
	public ConnectionStatus ConnectionStatus { get => connectionStatus; private set => connectionStatus = value; }
	
	[SerializeField] private Session session;
	public Session Session 
	{ 
		get => session; 
		set { session = value; session.transform.SetParent(runnerInstance.transform); } 
	}
	
	[SerializeField] private NetworkPrefabRef playerInfoPrefab;
	public NetworkPrefabRef PlayerInfoPrefab { get => playerInfoPrefab; set => playerInfoPrefab = value; }
	#endregion -------------------------------------------------------
	
	#region GameStatus
	public enum GameStatus
	{
		Lobby,
		Playing,
		Loading
	}
	[SerializeField] private GameStatus status;
	public GameStatus Status { get => status; set => status = value; }
	
	#endregion

	#region LevelManager
	[Space]
	public LevelManager LevelManager;
	#endregion
	public bool IsMaster => runnerInstance != null && (runnerInstance.IsServer || runnerInstance.IsSharedModeMasterClient);
	[SerializeField] private Session _sessionPrefab;
	[SerializeField] private ErrorBox _errorBox;
	[SerializeField] private bool _sharedMode;

	[Space(10)]
	[SerializeField] private bool _autoConnect;
	[SerializeField] private SessionProps _autoSession = new SessionProps();
	
	private Action<List<SessionInfo>> _onSessionListUpdated;

	private CharacterInputData _data;
	
	private string _lobbyId;

	private readonly Dictionary<PlayerRef, PlayerInfo> playerDictionary = new Dictionary<PlayerRef, PlayerInfo>();
	public ICollection<PlayerInfo> AllPlayerInfo => playerDictionary.Values;
	
	public FusionEvent onPlayerJoined;
	public FusionEvent onPlayerLeft;
	public FusionEvent onShutdown;
	public FusionEvent onDisconnect;
	public FusionEvent onSceneLoaded;
	[SerializeField] private GameObject _exitCanvas;

	#region MonoBehaviour
	
	private void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}

		if(instance != this)
		{
			Destroy(gameObject);
		}
		DontDestroyOnLoad(gameObject);
	}
	
	private void OnEnable()
	{
		SceneManager.sceneUnloaded += SceneManagerOnsceneUnloaded;
		SceneManager.sceneLoaded += SceneManagerOnsceneLoaded;
		SceneManager.activeSceneChanged += SceneManagerOnactiveSceneChanged;
		onPlayerLeft.RegisterResponse(PlayerDisconnected);
	}
	private void OnDisable()
	{
		SceneManager.sceneUnloaded -= SceneManagerOnsceneUnloaded;
		SceneManager.sceneLoaded -= SceneManagerOnsceneLoaded;
		SceneManager.activeSceneChanged -= SceneManagerOnactiveSceneChanged;
		onPlayerLeft.RemoveResponse(PlayerDisconnected);
	}
	#endregion
	
	#region Network Connection
	private void Connect()
	{
		if (runnerInstance == null)
		{
			SetConnectionStatus(ConnectionStatus.Connecting);
			GameObject go = new GameObject("NetworkRunner");
			go.transform.SetParent(transform);

			playerDictionary.Clear();
			runnerInstance = go.AddComponent<NetworkRunner>();
			runnerInstance.AddCallbacks(this);
		}
	}

	public void Disconnect()
	{
		if (runnerInstance != null)
		{
			SetConnectionStatus(ConnectionStatus.Disconnected);
			runnerInstance.Shutdown();
		}
	}

	public void JoinSession(SessionInfo info)
	{
		SessionProps props = new SessionProps(info.Properties)
		{
			PlayerLimit = info.MaxPlayers,
			RoomName = info.Name
		};
		StartSession(_sharedMode ? GameMode.Shared : GameMode.Client, props);
	}
	
	public void CreateSession(SessionProps props)
	{
		StartSession(_sharedMode ? GameMode.Shared : GameMode.Host, props);
	}
	private void SetConnectionStatus(ConnectionStatus status, string reason="")
	{
		if (ConnectionStatus == status)
			return;
		ConnectionStatus = status;

		if (!string.IsNullOrWhiteSpace(reason) && reason != "Ok")
		{
			_errorBox.Show(status,reason);
		}
		
		Debug.Log($"ConnectionStatus = {status} {reason}");
	}

	public void PrintLocalRunnerInfo()
	{
		DebugLogMessage.Log($"Local Runner Player[{FusionNetwork.LocalRunner}]");
	}
	#endregion

	#region Session
	
	private void StartSession(GameMode mode, SessionProps props, bool disableClientSessionCreation=true)
	{
		Connect();

		SetConnectionStatus(ConnectionStatus.Starting);

		Debug.Log($"Starting Game Session {props.RoomName}, Player Limit {props.PlayerLimit}, Server Game Mode: {mode}");
		runnerInstance.ProvideInput = mode != GameMode.Server;
		runnerInstance.StartGame(new StartGameArgs
		{
			GameMode = mode,
			CustomLobbyName = _lobbyId,
			Scene = SceneManager.GetActiveScene().buildIndex,
			SceneObjectProvider = LevelManager,
			SessionName = props.RoomName,
			PlayerCount = props.PlayerLimit,
			SessionProperties = props.Properties,
			DisableClientSessionCreation = disableClientSessionCreation
		});
	}

	public async Task EnterLobby(string lobbyId, Action<List<SessionInfo>> onSessionListUpdated)
	{
		Debug.Log("Entering Lobby");
		Connect();

		_lobbyId = lobbyId;
		_onSessionListUpdated = onSessionListUpdated;

		SetConnectionStatus(ConnectionStatus.EnteringLobby);
		var result = await runnerInstance.JoinSessionLobby(SessionLobby.Custom, lobbyId);

		if (!result.Ok) {
			_onSessionListUpdated = null;
			SetConnectionStatus(ConnectionStatus.Failed);
			onSessionListUpdated(null);
		}
	}
	
	private async Task LeaveRoomAsync()
	{
		await ShutdownRunner();
	}
	
	#endregion

	#region Player

	public void SetPlayer(PlayerRef playerRef, PlayerInfo playerInfo)
	{
		Debug.Log($"SetPlayer {playerRef} {playerInfo}");
		playerDictionary[playerRef] = playerInfo;
		playerInfo.transform.SetParent(runnerInstance.transform);
		
		if (Session.Level != null)
		{ // Late join
			Session.Level.SpawnCharacter(playerInfo, true);
		}
	}

	public PlayerInfo GetPlayer(PlayerRef ply=default)
	{
		if (!runnerInstance)
			return null;
		if (ply == default)
			ply = runnerInstance.LocalPlayer;
		playerDictionary.TryGetValue(ply, out PlayerInfo playerInfo);
		return playerInfo;
	}
	public bool GetPlayerCharacter(out Character character, PlayerRef ply=default)
	{
		character = null; 
		if (!runnerInstance)
		{
			return false;
		}

		if (ply == default)
		{
			ply = runnerInstance.LocalPlayer;
		}

		if (playerDictionary.TryGetValue(ply, out PlayerInfo playerInfo))
		{
			character = playerInfo.Character;
			return true;
		}

		return false;
	}
	#endregion

	public void LeaveRoom()
	{
		_ = LeaveRoomAsync();
	}
	
	public void DisconnectedFromSession(PlayerRef player, NetworkRunner runner)
	{
		Debug.Log("Disconnected from the session");
		ExitSession();
	}
	
	public void ExitSession()
	{
		_ = ShutdownRunner();
		LevelManager.ResetLoadedScene();
		SceneManager.LoadScene(0);
		_exitCanvas.SetActive(false);
	}
	public void ExitGame()
	{
		_ = ShutdownRunner();
		Application.Quit();
	}
	
	#region INetworkRunnerCallbacks
	public void OnConnectedToServer(NetworkRunner runner)
	{
		Debug.Log("Connected to server");
		SetConnectionStatus(ConnectionStatus.Connected);
	}
	public void OnDisconnectedFromServer(NetworkRunner runner)
	{
		Debug.Log("Disconnected from server");
		Disconnect();
	}
	public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
	{
		Debug.Log($"Connect failed {reason}");
		Disconnect();
		SetConnectionStatus(ConnectionStatus.Failed, reason.ToString());
	}
	public void OnPlayerJoined(NetworkRunner runner, PlayerRef playerRef)
	{
		if (session == null && IsMaster)
		{
			Debug.Log("Spawning world");
			session = runner.Spawn(_sessionPrefab, Vector3.zero, Quaternion.identity);
			session.gameObject.name = "Session";
			MasterRunner = runner;
		}

		if (IsMaster)
		{
			DebugLogMessage.Log(Color.green,$"Player {playerRef} Joined!");
			runner.Spawn(playerInfoPrefab, Vector3.zero, Quaternion.identity, inputAuthority: playerRef);
		}
		
		SetConnectionStatus(ConnectionStatus.Started);
		onPlayerJoined?.Raise(playerRef, runner);
	}

	public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
	{
		Debug.Log($"{player.PlayerId} disconnected.");
		playerDictionary.Remove(player);
	}
	
	public void PlayerDisconnected(PlayerRef player, NetworkRunner runner)
	{
		runner.Despawn(runner.GetPlayerObject(player).GetComponent<PlayerInfo>().Instance);
		runner.Despawn(runner.GetPlayerObject(player));
	}
	public void OnShutdown(NetworkRunner runner, ShutdownReason reason)
	{
		Debug.Log($"OnShutdown {reason}");
		SetConnectionStatus(ConnectionStatus.Disconnected, reason.ToString());

		if(this.runnerInstance!=null && this.runnerInstance.gameObject)
			Destroy(this.runnerInstance.gameObject);

		playerDictionary.Clear();
		this.runnerInstance = null;
		session = null;

		LevelManager.LoadMainMenu();
		onShutdown?.Raise(runner: runner);
	}
	
	private async Task ShutdownRunner()
	{
		await runnerInstance?.Shutdown(destroyGameObject: false);
		Status = GameStatus.Lobby;
	}
	
	public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
	{
		request.Accept();
	}
	public void OnInput(NetworkRunner runner, NetworkInput input) { }
	public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
	{
		SetConnectionStatus(ConnectionStatus.InLobby);
		_onSessionListUpdated?.Invoke(sessionList);
	}
	public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
	public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
	public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
	public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }
	public void OnSceneLoadDone(NetworkRunner runner) { }
	public void OnSceneLoadStart(NetworkRunner runner) { }
	public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
	#endregion

	#region Scene Management
	private void SceneManagerOnactiveSceneChanged(Scene previousScene, Scene newScene)
	{
		Debug.Log($"Changed from Scene {previousScene.name} to {newScene.name}");
	}

	private void SceneManagerOnsceneUnloaded(Scene sceneArg)
	{
		Debug.Log($"Unloading Scene {sceneArg.name}");
	}

	private void SceneManagerOnsceneLoaded(Scene sceneArg, LoadSceneMode loadMode)
	{
		Debug.Log($"Scene {sceneArg.name} Loaded");
	}
	#endregion
	
	#region Editor

	private void OnValidate()
	{
		if (LevelManager == null)
		{
			LevelManager = GetComponent<LevelManager>();
		}
	}

	#endregion
}

public enum ConnectionStatus
{
    Disconnected,
    Connecting,
    Connected,
    Failed,
    EnteringLobby,
    InLobby,
    Starting,
    Started
}