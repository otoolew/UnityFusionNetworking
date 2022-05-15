using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Level : SimulationBehaviour, ISpawned
{
	[SerializeField] private Character characterPrefab;
	[SerializeField] private Text countdownMessageText;
	[SerializeField] private Transform[] spawnPoints;

	private Dictionary<NetworkPlayer, Character> playerCharacterDictionary = new Dictionary<NetworkPlayer, Character>();
	
	public void Spawned()
	{
		Debug.Log("Level spawned");
		//GameManager.Instance.LevelManager.cu
		// Spawn player avatars
		foreach(NetworkPlayer player in GameManager.Instance.PlayerInfoList)
		{
			SpawnPlayerCharacter(player, false);
		}
		// Tell the master that we're done loading
		GameManager.Instance.Session.RPC_FinishedLoadingLevel(Runner.LocalPlayer);
		// Show the countdown message
		countdownMessageText.gameObject.SetActive(true);

		GameManager.Instance.Session.Level = this;
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
	
	public void SpawnPlayerCharacter(NetworkPlayer playerInfo, bool lateJoiner)
	{
		if (playerCharacterDictionary.ContainsKey(playerInfo))
		{
			DebugLogMessage.Log(Color.red,$"Character exists for {playerInfo.DisplayName}");
			return;
		}
		if (playerInfo.Object.HasStateAuthority) // We have StateAuth over the player if we are the host or if we're the player self in shared mode
		{
			DebugLogMessage.Log(Color.green,$"Spawning Character for {playerInfo.DisplayName} with input auth {playerInfo.Object.InputAuthority}");
			// Note: This only works if the number of spawnpoints in the map matches the maximum number of players - otherwise there's a risk of spawning multiple players in the same location.
			// For example, with 4 spawnpoints and a 5 player limit, the first player will get index 4 (max-1) and the second will get index 0, and both will then use the first spawn point.
			Transform t = spawnPoints[((int)playerInfo.Object.InputAuthority) % spawnPoints.Length];
			Character character = Runner.Spawn(playerInfo.CharacterPrefab, t.position, t.rotation, playerInfo.Object.InputAuthority);
			
			playerCharacterDictionary[playerInfo] = character;
			playerInfo.InputEnabled = lateJoiner;
			playerInfo.Character = character;
			playerInfo.Character.CharacterState = CharacterState.ACTIVE;
		}
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
