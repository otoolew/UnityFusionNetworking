using UIComponents;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI.Staging
{
	public class PlayerListItem : GridCell
	{
		[SerializeField] private Text _name;
		[SerializeField] private Image _color;
		[SerializeField] private GameObject _ready;

		public void Setup(PlayerInfo playerInfo)
		{
			_name.text = playerInfo.DisplayName;
			_color.color = playerInfo.Color;
			_ready.SetActive(playerInfo.Ready);
		}
	}
}