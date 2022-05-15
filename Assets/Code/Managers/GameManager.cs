using System;
using System.Collections.Generic;
using System.Linq;
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
	
	[SerializeField, HideInInspector] private Session session;
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
	[SerializeField] private Session sessionPrefab;
	[SerializeField] private ErrorBox errorBox;
	[SerializeField] private bool sharedMode;

	[Space(10)]
	[SerializeField] private bool _autoConnect;
	[SerializeField] private SessionProps _autoSession = new SessionProps();
	
	private Action<List<SessionInfo>> onSessionListUpdated;

	private string lobbyId;

	private readonly Dictionary<PlayerRef, NetworkPlayer> playerRegistry = new Dictionary<PlayerRef, NetworkPlayer>();
	public IList<NetworkPlayer> PlayerInfoList => playerRegistry.Values.ToList();
	
	public FusionEvent onPlayerJoined;
	public FusionEvent onPlayerLeft;
	public FusionEvent onShutdown;
	public FusionEvent onDisconnect;
	public FusionEvent onSceneLoaded;
	[SerializeField] private GameObject exitCanvas;

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
		onPlayerLeft.RegisterResponse(PlayerDisconnected);
	}
	private void OnDisable()
	{
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

			playerRegistry.Clear();
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
	
	private void SetConnectionStatus(ConnectionStatus status, string reason="")
	{
		if (ConnectionStatus == status)
			return;
		ConnectionStatus = status;

		if (!string.IsNullOrWhiteSpace(reason) && reason != "Ok")
		{
			errorBox.Show(status,reason);
		}
		
		//Debug.Log($"ConnectionStatus = {status} {reason}");
	}
	#endregion

	#region Session
	public void JoinSession(SessionInfo info)
	{
		SessionProps props = new SessionProps(info.Properties);
		props.PlayerLimit = info.MaxPlayers;
		props.RoomName = info.Name;
		StartSession(sharedMode ? GameMode.Shared : GameMode.Client, props);
	}
	
	public void CreateSession(SessionProps props)
	{
		StartSession(sharedMode ? GameMode.Shared : GameMode.Host, props, !sharedMode);
	}
	
	private void StartSession(GameMode mode, SessionProps props, bool disableClientSessionCreation=true)
	{
		Connect();

		SetConnectionStatus(ConnectionStatus.Starting);

		Debug.Log($"Starting Game Session {props.RoomName}, Player Limit {props.PlayerLimit}, Server Game Mode: {mode}");
		runnerInstance.ProvideInput = mode != GameMode.Server;
		runnerInstance.StartGame(new StartGameArgs
		{
			GameMode = mode,
			CustomLobbyName = lobbyId,
			SceneObjectProvider = LevelManager,
			SessionName = props.RoomName,
			PlayerCount = props.PlayerLimit,
			SessionProperties = props.Properties,
			DisableClientSessionCreation = disableClientSessionCreation
		});
	}

	public async Task EnterLobby(string lobbyId, Action<List<SessionInfo>> onSessionListUpdated)
	{
		Connect();

		this.lobbyId = lobbyId;
		this.onSessionListUpdated = onSessionListUpdated;

		SetConnectionStatus(ConnectionStatus.EnteringLobby);
		var result = await runnerInstance.JoinSessionLobby(SessionLobby.Custom, lobbyId);

		if (!result.Ok) {
			this.onSessionListUpdated = null;
			SetConnectionStatus(ConnectionStatus.Failed);
			onSessionListUpdated(null);
		}
	}
	
	private async Task LeaveRoomAsync()
	{
		await ShutdownRunner();
	}
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
		exitCanvas.SetActive(false);
	}
	public void ExitGame()
	{
		_ = ShutdownRunner();
		Application.Quit();
	}
	#endregion Session

	#region Player

	public void RegisterNetworkPlayer(PlayerRef playerRef, NetworkPlayer player)
	{
		Debug.Log($"Registering {playerRef} {player}");
		playerRegistry[playerRef] = player;
		player.transform.SetParent(runnerInstance.transform);
		
		// If a Session is in Progress
		if (Session.Level != null)
		{ 
			Session.Level.SpawnPlayerCharacter(player, true);
		}
	}

	public NetworkPlayer GetNetworkPlayer(PlayerRef playerRef = default)
	{
		if (!runnerInstance)
		{
			return null;
		}

		if (playerRef == default)
		{
			playerRef = runnerInstance.LocalPlayer;
		}
		playerRegistry.TryGetValue(playerRef, out NetworkPlayer player);
		
		return player;
	}

	public bool TryGetPlayerCharacter(out Character character, PlayerRef playerRef = default)
	{
		character = null;
		if (!runnerInstance)
		{
			return false;
		}

		if (playerRef == default)
		{
			playerRef = runnerInstance.LocalPlayer;
		}

		if (playerRegistry.TryGetValue(playerRef, out NetworkPlayer player))
		{
			if (player.Character != null)
			{
				character = player.Character;
				return true;
			}
		}
		
		return false;
	}
	
	#endregion

	#region Level Management

	public bool TryGetLevel(out Level level)
	{
		if (Session)
		{
			level = Session.Level;
			return true;
		}
		level = null;
		return false;
	}
	
	#endregion

	
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
		Debug.Log($"Player {playerRef} Joined!");
		if (session == null && IsMaster)
		{
			Debug.Log("Spawning world");
			session = runner.Spawn(sessionPrefab, Vector3.zero, Quaternion.identity);
			session.gameObject.name = "Session";
		}

		
		if (runner.IsServer || runner.Topology == SimulationConfig.Topologies.Shared && playerRef == runner.LocalPlayer)
		{
			DebugLogMessage.Log(Color.green,$"Spawn PlayerInfo {playerRef}");
			runner.Spawn(playerInfoPrefab, Vector3.zero, Quaternion.identity, playerRef);
		}
		
		SetConnectionStatus(ConnectionStatus.Started);
	}

	public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
	{
		Debug.Log($"{player.PlayerId} disconnected.");
		playerRegistry.Remove(player);
	}
	
	public void PlayerDisconnected(PlayerRef player, NetworkRunner runner)
	{
		runner.Despawn(runner.GetPlayerObject(player).GetComponent<NetworkPlayer>().Instance);
		runner.Despawn(runner.GetPlayerObject(player));
	}
	public void OnShutdown(NetworkRunner runner, ShutdownReason reason)
	{
		Debug.Log($"OnShutdown {reason}");
		SetConnectionStatus(ConnectionStatus.Disconnected, reason.ToString());

		if(this.runnerInstance!=null && runnerInstance.gameObject)
			Destroy(this.runnerInstance.gameObject);

		playerRegistry.Clear(); runnerInstance = null;
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

	public void OnInput(NetworkRunner runner, NetworkInput input)
	{
		/*// Persistent button flags like GetKey should be read when needed so they always have the actual state for this tick
		_data.ButtonFlags |= Input.GetKey( KeyCode.W ) ? ButtonFlag.FORWARD : 0;
		_data.ButtonFlags |= Input.GetKey( KeyCode.A ) ? ButtonFlag.LEFT : 0;
		_data.ButtonFlags |= Input.GetKey( KeyCode.S ) ? ButtonFlag.BACKWARD : 0;
		_data.ButtonFlags |= Input.GetKey( KeyCode.D ) ? ButtonFlag.RIGHT : 0;

		input.Set( _data );

		// Clear the flags so they don't spill over into the next tick unless they're still valid input.
		_data.ButtonFlags = 0;*/
	}
	public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
	{
		SetConnectionStatus(ConnectionStatus.InLobby);
		onSessionListUpdated?.Invoke(sessionList);
	}
	public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
	public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
	public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
	public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }
	public void OnSceneLoadDone(NetworkRunner runner) { }
	public void OnSceneLoadStart(NetworkRunner runner) { }
	public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
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