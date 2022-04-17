using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.Serialization;

public class FusionNetwork : MonoBehaviour
{
    public string MasterID;
    public static NetworkRunner MasterRunner;
    public string LocalID;
    public static NetworkRunner LocalRunner;
    
    public static List<PlayerInfo> PlayerInfoList { get; } = new List<PlayerInfo>();
    public static Dictionary<PlayerRef, PlayerInfo> PlayerInfoByPlayerRefDictionary  { get; } = new Dictionary<PlayerRef, PlayerInfo>();
    public ICollection<PlayerInfo> AllPlayerInfo => PlayerInfoByPlayerRefDictionary.Values;
    #region Events
    public FusionEvent onPlayerJoined;
    public FusionEvent onPlayerLeft;
    #endregion

    #region MonoBehaviour

    private void Awake()
    {
        DebugLogMessage.Log(Color.cyan, $"{gameObject.name} -> Awake()");
    }

    private void OnEnable()
    {
        DebugLogMessage.Log(Color.cyan, $"{gameObject.name} -> OnEnable()");
        onPlayerJoined.RegisterResponse(OnPlayerJoined);
    }

    // Start is called before the first frame update
    private void Start()
    {
        DebugLogMessage.Log(Color.cyan, $"{gameObject.name} -> Start()");
    }

    private void OnDisable()
    {
        DebugLogMessage.Log(Color.cyan, $"{gameObject.name} -> OnDisable()");
        onPlayerJoined.RemoveResponse(OnPlayerJoined);
    }

    private void OnDestroy()
    {
        DebugLogMessage.Log(Color.cyan, $"{gameObject.name} -> OnDestroy()");
    }

    #endregion
    public void OnPlayerJoined(PlayerRef player, NetworkRunner runner)
    {
        if (runner.IsServer == player)
        {
            MasterID = player.PlayerId.ToString();
            MasterRunner = runner;
        }
        if (runner.LocalPlayer == player)
        {
            LocalID = player.PlayerId.ToString();
            LocalRunner = runner;
        }
        
        
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {

    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnDisconnectedFromServer(NetworkRunner runner) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    
}
