﻿using Fusion;
using Fusion.Plugin;
using TMPro;
using UnityEngine;

namespace UnityFusionNetworking.UI
{
	public class UIGameInfo : UIWidget
	{
		// PRIVATE MEMBERS

		// [SerializeField]
		// private TextMeshProUGUI _gameNameText;
		[SerializeField]
		private TextMeshProUGUI _sessionNameText;
		[SerializeField]
		private TextMeshProUGUI _regionText;
		[SerializeField]
		private TextMeshProUGUI _fpsText;
		[SerializeField]
		private TextMeshProUGUI _rttText;
		[SerializeField]
		private TextMeshProUGUI _connectionTypeText;

		private int             _lastFPS;
		private int             _lastRTT = -1;
		private int             _frameCount;
		private float           _deltaTime;
		private double          _rtt;
		private ConnectionType  _connectionType;

		// UIWidget INTERFACE

		protected override void OnVisible()
		{
			base.OnVisible();

			UpdateInfo(Context.Runner, true);
		}

		protected override void OnTick()
		{
			UpdateInfo(Context.Runner);
		}

		// PRIVATE MEMBERS

		private void UpdateInfo(NetworkRunner runner, bool full = false)
		{
			if (runner == null)
				return;

			_frameCount++;

			_deltaTime += Time.deltaTime;
			_rtt += runner.Simulation.GetRTT();

			if (_deltaTime > 0.25f)
			{
				int fps = Mathf.RoundToInt(_frameCount / _deltaTime);
				if (fps != _lastFPS)
				{
					_lastFPS = fps;
					_fpsText.text = fps.ToString();
				}

				int rtt = (int)(_rtt * 1000.0 / _frameCount);
				if (rtt != _lastRTT)
				{
					_lastRTT = rtt;
					_rttText.text = rtt > 0 ? rtt.ToString() : "---";
				}

				_frameCount = 0;
				_deltaTime = 0f;
				_rtt = 0.0;
			}

			if (_connectionType != runner.CurrentConnectionType)
			{
				_connectionType = runner.CurrentConnectionType;
				_connectionTypeText.text = _connectionType.ToString();
			}

			if (full == true)
			{
				//_gameNameText.text = runner.SessionInfo.GetDisplayName();
				_sessionNameText.text = runner.SessionInfo.Name;
				_regionText.text = runner.SessionInfo.Region;
			}
		}
	}
}