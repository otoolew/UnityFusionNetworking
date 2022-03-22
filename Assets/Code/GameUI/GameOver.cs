using UnityEngine;

namespace GameUI
{
	public class GameOver : MonoBehaviour
	{
		public void OnContinue()
		{
			GameManager.Instance.Session.LoadMap(MapIndex.Lobby);
		}
	}
}