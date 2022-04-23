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
		public void Setup(NetworkPlayer player)
		{
			_name.text = player.DisplayName;
			_color.color = player.Color;
			_ready.SetActive(player.Ready);
		}
	}
}