using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// Session gets created when a game session starts and exists in only one instance.
/// It survives scene loading and can be used to control game-logic inside a session, across scenes.
/// </summary>

public class Session : NetworkBehaviour
{
	[Networked] public TickTimer PostLoadCountDown { get; set; }
	public SessionProps Props => new SessionProps(Runner.SessionInfo.Properties);
	public SessionInfo Info => Runner.SessionInfo;
	public Level Level { get; set; }

	private readonly HashSet<PlayerRef> finishedLoading = new HashSet<PlayerRef>();

	public override void Spawned()
	{
		GameManager.Instance.Session = this;
		if (Object.HasStateAuthority && (Runner.CurrentScene == 0 || Runner.CurrentScene == SceneRef.None))
		{
			SessionProps props = new SessionProps(Runner.SessionInfo.Properties);
			if (props.SkipStaging)
				LoadMap(props.StartMap);
			else
				Runner.SetActiveScene((int)MapIndex.Lobby);
		}
	}

	[Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
	public void RPC_FinishedLoadingLevel(PlayerRef playerRef)
	{
		finishedLoading.Add(playerRef);
		if (finishedLoading.Count >= GameManager.Instance.PlayerInfoList.Count)
		{
			PostLoadCountDown = TickTimer.CreateFromSeconds(Runner,3);
		}
	}

	public override void FixedUpdateNetwork()
	{
		if (PostLoadCountDown.Expired(Runner))
		{
			PostLoadCountDown = TickTimer.None;
			foreach (NetworkPlayer playerInfo in GameManager.Instance.PlayerInfoList)
				playerInfo.InputEnabled = true;
		}
	}

	public void LoadMap(MapIndex mapIndex)
	{
		finishedLoading.Clear();
		foreach (NetworkPlayer playerInfo in GameManager.Instance.PlayerInfoList)
			playerInfo.InputEnabled = false;
		Runner.SetActiveScene((int)mapIndex);
	}
}