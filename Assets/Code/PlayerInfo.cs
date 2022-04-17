using Fusion;
using UnityEngine;

/// <summary>
/// Player is a network object that represents a players core data. One instance is spawned
/// for each player when the game session starts and it lives until the session ends.
/// This is not the visual representation of the player.
/// </summary>
public class PlayerInfo : NetworkBehaviour
{
	[SerializeField] public Character Character;
	[Networked] public string DisplayName { get; set; }
	[Networked] public Color Color { get; set; }
	[Networked] public NetworkBool Ready { get; set; }
	[Networked] public NetworkBool InputEnabled { get; set; }
	[Networked] public NetworkObject Instance { get; set; }

	public FusionEvent OnPlayerDataSpawnedEvent;
	
	public override void Spawned()
	{
		if (Object.HasInputAuthority)
		{
			RPC_SetDisplayName("Player " + Object.Id);
		}


		DontDestroyOnLoad(this);
		Runner.SetPlayerObject(Object.InputAuthority, Object);
		OnPlayerDataSpawnedEvent?.Raise(Object.InputAuthority, Runner);
		
		GameManager.Instance.SetPlayer(Object.InputAuthority, this);
	}

	public static void OnDisplayNameUpdate(Changed<PlayerInfo> changed)
	{
		changed.Behaviour.OnPlayerDataSpawnedEvent?.Raise(changed.Behaviour.Object.InputAuthority, changed.Behaviour.Runner);
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
}