using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using GameUI;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// This is the main entry point for the application. App is a singleton created when the game is launched.
/// Access it anywhere using `App.Instance`
/// </summary>

[RequireComponent(typeof(NetworkSceneManagerBase))]
public class App : MonoBehaviour, INetworkRunnerCallbacks
{
	/*[SerializeField] private SceneReference _introScene;
	[SerializeField] private Player _playerPrefab;
	[SerializeField] private Session _sessionPrefab;
	[SerializeField] private ErrorBox _errorBox;
	[SerializeField] private bool _sharedMode;

	[Space(10)]
	[SerializeField] private bool _autoConnect;
	[SerializeField] private SessionProps _autoSession = new SessionProps();
	
	private NetworkRunner _runner;
	private NetworkSceneManagerBase _loader;
	private Action<List<SessionInfo>> _onSessionListUpdated;
	private readonly Dictionary<PlayerRef, Player> _players = new Dictionary<PlayerRef, Player>();
	private InputData _data;
	private Session _session;
	private string _lobbyId;

	private static App _instance;

	public static App Instance
	{
		get
		{
			if (_instance == null)
				_instance = FindObjectOfType<App>();
			return _instance;
		}
	}
	
	public ConnectionStatus ConnectionStatus { get; private set; }
	public ICollection<Player> Players => _players.Values;
	public bool IsMaster => _runner != null && (_runner.IsServer || _runner.IsSharedModeMasterClient);

	private void Awake()
	{
		if (_instance == null)
			_instance = this;
		
		if(_instance!=this)
		{
			Destroy(gameObject);
		}
		else if(_loader==null)
		{
			_loader = GetComponent<NetworkSceneManagerBase>();
		
			DontDestroyOnLoad(gameObject);

			if (_autoConnect)
			{
				StartSession( _sharedMode ? GameMode.Shared : GameMode.AutoHostOrClient, _autoSession, false);
			}
			else
			{
				SceneManager.LoadSceneAsync( _introScene );
			}
		}
	}

	private void Connect()
	{
		if (_runner == null)
		{
			SetConnectionStatus(ConnectionStatus.Connecting);
			GameObject go = new GameObject("Session");
			go.transform.SetParent(transform);

			_players.Clear();
			_runner = go.AddComponent<NetworkRunner>();
			_runner.AddCallbacks(this);
		}
	}

	public void Disconnect()
	{
		if (_runner != null)
		{
			SetConnectionStatus(ConnectionStatus.Disconnected);
			_runner.Shutdown();
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

	private void StartSession(GameMode mode, SessionProps props, bool disableClientSessionCreation=true)
	{
		Connect();

		SetConnectionStatus(ConnectionStatus.Starting);

		Debug.Log($"Starting game with session {props.RoomName}, player limit {props.PlayerLimit}");
		_runner.ProvideInput = mode != GameMode.Server;
		_runner.StartGame(new StartGameArgs
		{
			GameMode = mode,
			CustomLobbyName = _lobbyId,
			SceneObjectProvider = _loader,
			SessionName = props.RoomName,
			PlayerCount = props.PlayerLimit,
			SessionProperties = props.Properties,
			DisableClientSessionCreation = disableClientSessionCreation
		});
	}

	public async Task EnterLobby(string lobbyId, Action<List<SessionInfo>> onSessionListUpdated)
	{
		Connect();

		_lobbyId = lobbyId;
		_onSessionListUpdated = onSessionListUpdated;

		SetConnectionStatus(ConnectionStatus.EnteringLobby);
		var result = await _runner.JoinSessionLobby(SessionLobby.Custom, lobbyId);

		if (!result.Ok) {
			_onSessionListUpdated = null;
			SetConnectionStatus(ConnectionStatus.Failed);
			onSessionListUpdated(null);
		}
	}

	public Session Session
	{
		get => _session;
		set { _session = value; _session.transform.SetParent(_runner.transform); }
	}

	public void SetPlayer(PlayerRef playerRef, Player player)
	{
		_players[playerRef] = player;
		player.transform.SetParent(_runner.transform);
		if (Session.Map != null)
		{ // Late join
			Session.Map.SpawnAvatar(player, true);
		}
	}

	public Player GetPlayer(PlayerRef ply=default)
	{
		if (!_runner)
			return null;
		if (ply == default)
			ply = _runner.LocalPlayer;
		_players.TryGetValue(ply, out Player player);
		return player;
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
	
	/// <summary>
	/// Fusion Event Handlers
	/// </summary>
	#region Fusion Event Handlers
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
		if ( _session==null && IsMaster)
		{
			Debug.Log("Spawning world");
			_session = runner.Spawn(_sessionPrefab, Vector3.zero, Quaternion.identity);
		}

		if (runner.IsServer || runner.Topology == SimulationConfig.Topologies.Shared && playerRef == runner.LocalPlayer)
		{
			Debug.Log("Spawning player");
			runner.Spawn(_playerPrefab, Vector3.zero, Quaternion.identity, playerRef);
		}

		SetConnectionStatus(ConnectionStatus.Started);
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

		if(_runner!=null && _runner.gameObject)
			Destroy(_runner.gameObject);

		_players.Clear();
		_runner = null;
		_session = null;

		if(Application.isPlaying)
			SceneManager.LoadSceneAsync(_introScene);
	}

	public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
	{
		request.Accept();
	}

	private void Update() 
	{
		// Check events like KeyDown or KeyUp in Unity's update. They might be missed otherwise because they're only true for 1 frame
		_data.ButtonFlags |= Input.GetKeyDown( KeyCode.R ) ? ButtonFlag.RESPAWN : 0;
	}
	
	public void OnInput(NetworkRunner runner, NetworkInput input)
	{
		// Persistent button flags like GetKey should be read when needed so they always have the actual state for this tick
		_data.ButtonFlags |= Input.GetKey( KeyCode.W ) ? ButtonFlag.FORWARD : 0;
		_data.ButtonFlags |= Input.GetKey( KeyCode.A ) ? ButtonFlag.LEFT : 0;
		_data.ButtonFlags |= Input.GetKey( KeyCode.S ) ? ButtonFlag.BACKWARD : 0;
		_data.ButtonFlags |= Input.GetKey( KeyCode.D ) ? ButtonFlag.RIGHT : 0;

		input.Set( _data );

		// Clear the flags so they don't spill over into the next tick unless they're still valid input.
		_data.ButtonFlags = 0;
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
	

	#endregion*/

	public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
	{
		throw new NotImplementedException();
	}

	public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
	{
		throw new NotImplementedException();
	}

	public void OnInput(NetworkRunner runner, NetworkInput input)
	{
		throw new NotImplementedException();
	}

	public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
	{
		throw new NotImplementedException();
	}

	public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
	{
		throw new NotImplementedException();
	}

	public void OnConnectedToServer(NetworkRunner runner)
	{
		throw new NotImplementedException();
	}

	public void OnDisconnectedFromServer(NetworkRunner runner)
	{
		throw new NotImplementedException();
	}

	public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
	{
		throw new NotImplementedException();
	}

	public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
	{
		throw new NotImplementedException();
	}

	public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
	{
		throw new NotImplementedException();
	}

	public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
	{
		throw new NotImplementedException();
	}

	public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
	{
		throw new NotImplementedException();
	}

	public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data)
	{
		throw new NotImplementedException();
	}

	public void OnSceneLoadDone(NetworkRunner runner)
	{
		throw new NotImplementedException();
	}

	public void OnSceneLoadStart(NetworkRunner runner)
	{
		throw new NotImplementedException();
	}
}