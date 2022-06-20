using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GameUI.Intro
{
	public class NewSessionPanel : MonoBehaviour
	{
		[SerializeField] private InputField inputName;
		[SerializeField] private Text textMaxPlayers;
		[SerializeField] private Toggle toggleMap1;
		[SerializeField] private Toggle toggleMap2;
		[SerializeField] private Button createGameButton;
		private int maxPly = 4;
		private PlayMode playMode;

		private void OnEnable()
		{
			createGameButton.interactable = true;
		}

		public void Show(PlayMode mode)
		{
			gameObject.SetActive(true);
			playMode = mode;
			UpdateUI();
		}

		public void Hide()
		{
			gameObject.SetActive(false);
		}

		public void OnDecreaseMaxPlayers()
		{
			if(maxPly>2)
				maxPly--;
			UpdateUI();
		}
		public void OnIncreaseMaxPlayers()
		{
			if(maxPly<16)
				maxPly++;
			UpdateUI();
		}

		public void OnEditText()
		{
			UpdateUI();
		}

		private void UpdateUI()
		{
			textMaxPlayers.text = $"Max Players: {maxPly}";
			if(!toggleMap1.isOn && !toggleMap2.isOn)
				toggleMap1.isOn = true;
			if(string.IsNullOrWhiteSpace(inputName.text))
				inputName.text = "Test Room";
		}
		
		public void OnCreateSession()
		{
			SessionProps props = new SessionProps
			{
				StartMap = toggleMap1.isOn ? LevelIndex.Map0 : LevelIndex.Map1,
				PlayMode = playMode,
				PlayerLimit = maxPly,
				RoomName = inputName.text
			};
			GameManager.Instance.CreateSession(props);
			
		}
		
	}
}