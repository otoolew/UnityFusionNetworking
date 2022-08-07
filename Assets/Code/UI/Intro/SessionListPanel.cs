using System;
using System.Collections.Generic;
using Fusion;
//using UIComponents;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI.Intro
{
	public class SessionListPanel : MonoBehaviour
	{
		[SerializeField] private Text _header;
		[SerializeField] private NewSessionPanel _newSessionPanel;
		//[SerializeField] private GridBuilder _sessionGrid;
		[SerializeField] private SessionListItem _sessionListItemPrefab;
		[SerializeField] private Text _error;

		private PlayMode _playMode;
		
		/// <summary>
		/// It’s clear that async void methods have several disadvantages compared to async Task methods, but they’re quite useful in one particular case: asynchronous event handlers.
		/// </summary>
		/// <param name="mode"></param>
		public async void Show(PlayMode mode)
		{
			gameObject.SetActive(true);
			_playMode = mode;
			_error.text = "";
			_header.text = $"{mode} Lobby";
			OnSessionListUpdated(new List<SessionInfo>());

			/*
			try
			{
				await GameManager.Instance.EnterLobby($"GameMode{mode}", OnSessionListUpdated);
			}
			catch (AggregateException e)
			{
				foreach (var innerException in e.InnerExceptions)
				{
					DebugLogMessage.Log(Color.red, $"{innerException.Message}\nSessionListPanel.Show(Playmode) Failed");
				}
			}*/
		}

		public void Hide()
		{
			_newSessionPanel.Hide();
			gameObject.SetActive(false);
			//GameManager.Instance.Disconnect();
		}

		public void OnSessionListUpdated(List<SessionInfo> sessions)
		{
			//_sessionGrid.BeginUpdate();
			if (sessions != null)
			{
				foreach (SessionInfo info in sessions)
				{
					//_sessionGrid.AddRow(_sessionListItemPrefab, item => item.Setup(info, selectedSession => GameManager.Instance.JoinSession(selectedSession)));
				}
			}
			else
			{
				Hide();
				_error.text = "Failed to join lobby";
			}
			//_sessionGrid.EndUpdate();
		}
		
		public void OnShowNewSessionUI()
		{
			_newSessionPanel.Show(_playMode);
		}
	}
}