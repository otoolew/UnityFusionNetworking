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
	#endregion --------------------------------------------------------
	
	#region Network Runner --------------------------------------------
	
	private NetworkRunner runner;
	
	public static NetworkRunner LocalRunner;
	
	/*
	[SerializeField] private FusionNetwork fusionNetwork;
	public FusionNetwork FusionNetwork { get => fusionNetwork; set => this.fusionNetwork = value; }*/
	
	[SerializeField] private ConnectionStatus connectionStatus;
	public ConnectionStatus ConnectionStatus { get => connectionStatus; private set => connectionStatus = value; }
	
	[SerializeField] private Session session;
	public Session Session 
	{ 
		get => session; 
		set { session = value; session.transform.SetParent(runner.transform); } 
	}
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

	#region NetworkAssets
	public NetworkPrefabRef Player;
	#endregion
	
	//[SerializeField] private SceneReference _introScene;
	//[SerializeField] private Player _playerPrefab;
	[SerializeField] private Session _sessionPrefab;
	[SerializeField] private ErrorBox _errorBox;
	[SerializeField] private bool _sharedMode;

	[Space(10)]
	[SerializeField] private bool _autoConnect;
	[SerializeField] private SessionProps _autoSession = new SessionProps();
	
	private Action<List<SessionInfo>> _onSessionListUpdated;
	private readonly Dictionary<PlayerRef, Player> _players = new Dictionary<PlayerRef, Player>();
	private CharacterInputData _data;
	
	private string _lobbyId;
	

	
	public ICollection<Player> Players => _players.Values;
	public bool IsMaster => runner != null && (runner.IsServer || runner.IsSharedModeMasterClient);
	
	public FusionEvent onPlayerJoined;
	public FusionEvent onPlayerLeft;
	public FusionEvent onShutdown;
	public FusionEvent onDisconnect;
	public FusionEvent onSceneLoaded;
	[SerializeField] private GameObject _exitCanvas;
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
	
	#region Network Connection
	private void Connect()
	{
		if (runner == null)
		{
			SetConnectionStatus(ConnectionStatus.Connecting);
			GameObject go = new GameObject("NetworkRunner");
			go.transform.SetParent(transform);

			_players.Clear();
			runner = go.AddComponent<NetworkRunner>();
			runner.AddCallbacks(this);
		}
	}

	public void Disconnect()
	{
		if (runner != null)
		{
			SetConnectionStatus(ConnectionStatus.Disconnected);
			runner.Shutdown();
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
		Debug.Log($"Local Runner Player[{LocalRunner.LocalPlayer.PlayerId}]");
	}
	#endregion
	private void StartSession(GameMode mode, SessionProps props, bool disableClientSessionCreation=true)
	{
		Connect();

		SetConnectionStatus(ConnectionStatus.Starting);

		Debug.Log($"Starting Game Session {props.RoomName}, Player Limit {props.PlayerLimit}, Server Game Mode: {mode}");
		runner.ProvideInput = mode != GameMode.Server;
		runner.StartGame(new StartGameArgs
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
		var result = await runner.JoinSessionLobby(SessionLobby.Custom, lobbyId);

		if (!result.Ok) {
			_onSessionListUpdated = null;
			SetConnectionStatus(ConnectionStatus.Failed);
			onSessionListUpdated(null);
		}
	}

	public void SetPlayer(PlayerRef playerRef, Player player)
	{
		Debug.Log($"SetPlayer {playerRef} {player}");
		_players[playerRef] = player;
		player.transform.SetParent(runner.transform);
		
		if (Session.Level != null)
		{ // Late join
			Session.Level.SpawnAvatar(player, true);
		}
	}

	public Player GetPlayer(PlayerRef ply=default)
	{
		if (!runner)
			return null;
		if (ply == default)
			ply = runner.LocalPlayer;
		_players.TryGetValue(ply, out Player player);
		return player;
	}
	public void PlayerDisconnected(PlayerRef player, NetworkRunner runner)
	{
		runner.Despawn(runner.GetPlayerObject(player).GetComponent<Player>().Instance);
		runner.Despawn(runner.GetPlayerObject(player));
	}
	
	
	//Called by button
	public void LeaveRoom()
	{
		_ = LeaveRoomAsync();
	}

	private async Task LeaveRoomAsync()
	{
		await ShutdownRunner();
	}
	
	private async Task ShutdownRunner()
	{
		await runner?.Shutdown(destroyGameObject: false);
		Status = GameStatus.Lobby;
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
		Debug.Log($"Player {playerRef} Joined!");
		if (session == null && IsMaster)
		{
			Debug.Log("Spawning world");
			session = runner.Spawn(_sessionPrefab, Vector3.zero, Quaternion.identity);
			session.gameObject.name = "Session";
		}

		if (runner.IsServer || runner.Topology == SimulationConfig.Topologies.Shared && playerRef == runner.LocalPlayer)
		{
			Debug.Log("Spawning player");
			runner.Spawn(Player, Vector3.zero, Quaternion.identity, playerRef);
		}

		SetConnectionStatus(ConnectionStatus.Started);
		onPlayerJoined?.Raise(playerRef, runner);
	}
	public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
	{
		Debug.Log($"{player.PlayerId} disconnected.");
		_players.Remove(player);
	}
	public void OnShutdown(NetworkRunner runner, ShutdownReason reason)
	{
		Debug.Log($"OnShutdown {reason}");
		SetConnectionStatus(ConnectionStatus.Disconnected, reason.ToString());

		if(this.runner!=null && this.runner.gameObject)
			Destroy(this.runner.gameObject);

		_players.Clear();
		this.runner = null;
		session = null;

		LevelManager.LoadMainMenu();
		onShutdown?.Raise(runner: runner);
	}
	public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
	{
		request.Accept();
	}
	public void OnInput(NetworkRunner runner, NetworkInput input)
	{
		/*// Persistent button flags like GetKey should be read when needed so they always have the actual state for this tick
		_data.Buttons |= Input.GetKey( KeyCode.W ) ? CharacterButton.FORWARD: 0;
		_data.Buttons |= Input.GetKey( KeyCode.A ) ? CharacterButton.LEFT : 0;
		_data.Buttons |= Input.GetKey( KeyCode.S ) ? CharacterButton.BACKWARD : 0;
		_data.Buttons |= Input.GetKey( KeyCode.D ) ? CharacterButton.RIGHT : 0;

		input.Set( _data );
		// Clear the flags so they don't spill over into the next tick unless they're still valid input.
		_data.Buttons = 0;*/
	}
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