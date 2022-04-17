using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Level : SimulationBehaviour, ISpawned
{
	[SerializeField] private Text countdownMessageText;
	[SerializeField] private Transform[] spawnPoints;

	private Dictionary<PlayerInfo, Character> playerCharacters = new Dictionary<PlayerInfo, Character>();
	
	public void Spawned()
	{
		Debug.Log("Level spawned");
		// Spawn player avatars
		foreach(PlayerInfo player in GameManager.Instance.AllPlayerInfo)
		{
			SpawnCharacter(player, false);
		}
		// Tell the master that we're done loading
		GameManager.Instance.Session.RPC_FinishedLoadingLevel(Runner.LocalPlayer);
		// Show the countdown message
		countdownMessageText.gameObject.SetActive(true);

		GameManager.Instance.Session.Level = this;
	}
	
	public void SpawnCharacter(PlayerInfo playerInfo, bool lateJoiner)
	{
		if (playerCharacters.ContainsKey(playerInfo))
			return;
		if (playerInfo.Object.HasStateAuthority) // We have StateAuth over the player if we are the host or if we're the player self in shared mode
		{
			Debug.Log($"Spawning avatar for player {playerInfo.DisplayName} with input auth {playerInfo.Object.InputAuthority}");
			// Note: This only works if the number of spawnpoints in the map matches the maximum number of players - otherwise there's a risk of spawning multiple players in the same location.
			// For example, with 4 spawnpoints and a 5 player limit, the first player will get index 4 (max-1) and the second will get index 0, and both will then use the first spawn point.
			Transform t = spawnPoints[((int)playerInfo.Object.InputAuthority) % spawnPoints.Length];
			Character character = Runner.Spawn(playerInfo.Character, t.position, t.rotation, playerInfo.Object.InputAuthority);
			playerCharacters[playerInfo] = character;
			playerInfo.InputEnabled = lateJoiner;
			character.CurrentState = Character.State.ACTIVE;
		}
	}

	public override void FixedUpdateNetwork()
	{
		// Update the countdown message
		Session session = GameManager.Instance.Session;
		if (session.PostLoadCountDown.Expired(Runner))
			countdownMessageText.gameObject.SetActive(false);
		else if (session.PostLoadCountDown.IsRunning)
			countdownMessageText.text = Mathf.CeilToInt(session.PostLoadCountDown.RemainingTime(Runner)??0 ).ToString();
	}

	/// <summary>
	/// UI hooks
	/// </summary>

	public void OnDisconnect()
	{
		GameManager.Instance.Disconnect();
	}

	public void OnLoadMap1()
	{
		GameManager.Instance.Session.LoadMap(MapIndex.Map1);
	}

	public void OnGameOver()
	{
		GameManager.Instance.Session.LoadMap(MapIndex.MainMenu);
	}
}
