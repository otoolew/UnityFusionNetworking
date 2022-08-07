using System.Collections.Generic;
using UnityEngine;
using Fusion;

namespace UnityFusionNetworking
{
	[RequireComponent(typeof(NetworkRunner))]
	[RequireComponent(typeof(NetworkEvents))]
	public sealed class GameManager : SimulationBehaviour, IPlayerJoined, IPlayerLeft
	{
		[SerializeField] private GameSession gameSessionPrefab;
		[SerializeField] private Player playerPrefab;

		private readonly Dictionary<PlayerRef, Player> playerDictionary = new Dictionary<PlayerRef, Player>(32);
		private bool gameSessionSpawned;
		
		void IPlayerJoined.PlayerJoined(PlayerRef playerRef)
		{
			if (Runner.IsServer == false)
				return;

			if (gameSessionSpawned == false)
			{
				Runner.Spawn(gameSessionPrefab);
				gameSessionSpawned = true;
			}

			var player = Runner.Spawn(playerPrefab, inputAuthority: playerRef);
			playerDictionary.Add(playerRef, player);

			Runner.SetPlayerObject(playerRef, player.Object);
		}
		
		void IPlayerLeft.PlayerLeft(PlayerRef playerRef)
		{
			if (Runner.IsServer == false)
				return;

			if (playerDictionary.TryGetValue(playerRef, out Player player) == false)
				return;

			Runner.Despawn(player.Object);
		}
	}
}