using System.Collections;
using System.Collections.Generic;
using Fusion;
using JetBrains.Annotations;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static List<PlayerInfo> PlayerInfoList { get; } = new List<PlayerInfo>();

    private static Queue<PlayerInfo> PlayerJoinQueue { get; } = new Queue<PlayerInfo>();

    private  Dictionary<PlayerRef, PlayerInfo> PlayerInfoDictionary  { get; } = new Dictionary<PlayerRef, PlayerInfo>();
    public ICollection<PlayerInfo> AllPlayerInfo => PlayerInfoDictionary.Values;

    public FusionEvent onPlayerJoined;

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

    #region List Handing

    private void AddPlayerInfo(PlayerRef playerRef, PlayerInfo playerInfo)
    {
        if (PlayerInfoDictionary.ContainsKey(playerRef))
        {
            DebugLogMessage.Log(Color.white, $"{gameObject.name} -> OnDestroy()");
        }
    }

    #endregion
    #region Behaviour
    private void OnPlayerJoined(PlayerRef player, NetworkRunner runner)
    {
        
    }

    #endregion
}