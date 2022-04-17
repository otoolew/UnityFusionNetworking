using System.Text;
using UIComponents;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GameUI.Staging
{
	public class Staging : MonoBehaviour
	{
		[SerializeField] private GridBuilder playerGrid;
		[SerializeField] private PlayerListItem _playerListItemPrefab;
		[SerializeField] private Slider _sliderR;
		[SerializeField] private Slider _sliderG;
		[SerializeField] private Slider _sliderB;
		[SerializeField] private Image _color;
		[SerializeField] private Button _startButton;
		[SerializeField] private Text _startLabel;
		[SerializeField] private Text _sessionInfo;
		[SerializeField] private GameObject _playerReady;

		private float _sessionRefresh;

		private void Awake()
		{
			GameManager.Instance.GetPlayer()?.RPC_SetIsReady(false);
			_playerReady.SetActive(false);
		}

		private void UpdateSessionInfo()
		{
			Session s = GameManager.Instance.Session;
			StringBuilder sb = new StringBuilder();
			if (s != null)
			{
				sb.AppendLine($"Session Name: {s.Info.Name}");
				sb.AppendLine($"Region: {s.Info.Region}");
				sb.AppendLine($"Game Type: {s.Props.PlayMode}");
				sb.AppendLine($"Map: {s.Props.StartMap}");
			}
			_sessionInfo.text = sb.ToString();
		}

		void Update()
		{
			int count = 0;
			int ready = 0;
			playerGrid.BeginUpdate();
			foreach (PlayerInfo ply in GameManager.Instance.AllPlayerInfo)
			{
				playerGrid.AddRow(_playerListItemPrefab, item => item.Setup(ply));
				count++;
				if (ply.Ready)
					ready++;
			}

			string wait = null;
			if (ready < count)
				wait = $"Waiting for {count - ready} of {count} players";
			else if (!GameManager.Instance.IsMaster)
				wait = "Waiting for master to start";

			_startButton.enabled = wait==null;
			_startLabel.text = wait ?? "Start";
	  
			playerGrid.EndUpdate();

			if (_sessionRefresh <= 0)
			{
				UpdateSessionInfo();
				_sessionRefresh = 2.0f;
			}
			_sessionRefresh -= Time.deltaTime;
		}

		public void OnStart()
		{
			SessionProps props = GameManager.Instance.Session.Props;
			GameManager.Instance.Session.LoadMap(props.StartMap);
		}

		public void OnToggleIsReady()
		{
			PlayerInfo ply = GameManager.Instance.GetPlayer();
			_playerReady.SetActive(!ply.Ready);
			ply.RPC_SetIsReady(!ply.Ready);
		}

		public void OnDisplayNameChanged(string name)
		{
			PlayerInfo ply = GameManager.Instance.GetPlayer();
			ply.RPC_SetDisplayName(name);
		}
	
		public void OnColorUpdated()
		{
			PlayerInfo ply = GameManager.Instance.GetPlayer();
			Color c = new Color(_sliderR.value, _sliderG.value, _sliderB.value);
			_color.color = c;
			ply.RPC_SetColor( c);
		}

		public void OnDisconnect()
		{
			GameManager.Instance.Disconnect();
		}
	}
}
