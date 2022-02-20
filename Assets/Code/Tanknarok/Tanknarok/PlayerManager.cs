using System.Collections.Generic;
using UnityEngine;
using Fusion;

namespace FusionExamples.Tanknarok
{
	public class PlayerManager : MonoBehaviour
	{		
		private static List<TankPlayer> _allPlayers = new List<TankPlayer>();
		public static List<TankPlayer> allPlayers => _allPlayers;

		private static Queue<TankPlayer> _playerQueue = new Queue<TankPlayer>();

		static private CameraStrategy _cameraStrategy;
		static private CameraStrategy CameraStrategy
		{
			get
			{
				if (_cameraStrategy == null)
					_cameraStrategy = FindObjectOfType<CameraStrategy>(true);
				return _cameraStrategy;
			}
		}

		public static void HandleNewPlayers()
		{
			if (_playerQueue.Count > 0)
			{
				TankPlayer player = _playerQueue.Dequeue();

				CameraStrategy.AddTarget(player.gameObject);
				
				player.Respawn(0);
			}
		}

		public static int PlayersAlive()
		{
			int playersAlive = 0;
			for (int i = 0; i < _allPlayers.Count; i++)
			{
				if (_allPlayers[i].isActivated || _allPlayers[i].lives>0)
					playersAlive++;
			}

			return playersAlive;
		}

		public static TankPlayer GetFirstAlivePlayer()
		{
			for (int i = 0; i < _allPlayers.Count; i++)
			{
				if (_allPlayers[i].isActivated)
					return _allPlayers[i];
			}

			return null;
		}

		public static void AddPlayer(TankPlayer player)
		{
			Debug.Log("Player Added");

			int insertIndex = _allPlayers.Count;
			// Sort the player list when adding players
			for (int i = 0; i < _allPlayers.Count; i++)
			{
				if (_allPlayers[i].playerID > player.playerID)
				{
					insertIndex = i;
					break;
				}
			}

			_allPlayers.Insert(insertIndex, player);
			_playerQueue.Enqueue(player);
		}

		public static void RemovePlayer(TankPlayer player)
		{
			if (player==null || !_allPlayers.Contains(player))
				return;

			Debug.Log("Player Removed " + player.playerID);

			_allPlayers.Remove(player);
			if(CameraStrategy) // FindObject May return null on shutdown, so let's avoid that NPE
				CameraStrategy.RemoveTarget(player.gameObject);
		}

		public static void ResetPlayerManager()
		{
			Debug.Log("Clearing Player Manager");
			allPlayers.Clear();
			if(CameraStrategy) // FindObject May return null on shutdown, so let's avoid that NPE
				CameraStrategy.RemoveAll();
			TankPlayer.local = null;
		}

		public static TankPlayer GetPlayerFromID(int id)
		{
			foreach (TankPlayer player in _allPlayers)
			{
				if (player.playerID == id)
					return player;
			}

			return null;
		}

		public static TankPlayer Get(PlayerRef playerRef)
		{
			for (int i = _allPlayers.Count - 1; i >= 0; i--)
			{
				if (_allPlayers[i] == null || _allPlayers[i].Object == null)
				{
					_allPlayers.RemoveAt(i);
					Debug.Log("Removing null player");
				}
				else if (_allPlayers[i].Object.InputAuthority == playerRef)
					return _allPlayers[i];
			}

			return null;
		}
	}
}