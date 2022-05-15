using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour, IPlayerLeft
{
    public static NetworkPlayer LocalPlayer { get; private set; }
    public PlayerRef PlayerRef { get; private set; }
    [Networked] public string DisplayName { get; set; }
    [Networked] public Color Color { get; set; }
    [Networked] public NetworkBool Ready { get; set; }
    [Networked] public NetworkBool InputEnabled { get; set; }
    [Networked] public NetworkObject Instance { get; set; }
    
    [SerializeField] public Character CharacterPrefab;
    
    [SerializeField] public Character Character;

    public override void Spawned()
    {
        Instance = Object;
        Runner.SetPlayerObject(Object.InputAuthority, Object);
        PlayerRef = Object.InputAuthority;

        //GameManager.Instance.RegisterPlayerInfoToRef(PlayerRef, this);
        if (Object.HasInputAuthority)
        {
            LocalPlayer = this;
            RPC_SetDisplayName("Player " + Object.Id);
        }
        GameManager.Instance.RegisterNetworkPlayer(PlayerRef, this);
    }

    [Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.StateAuthority)]
    public void RPC_SetDisplayName(string displayName)
    {
        DisplayName = displayName;
    }
	
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetIsReady(NetworkBool ready)
    {
        Ready = ready;
    }
    
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetColor(Color color)
    {
        Color = color;
    }

    public void PlayerLeft(PlayerRef player)
    {
        if (player == Object.InputAuthority)
        {
            Runner.Despawn(Object);
        }
    }
    
}
