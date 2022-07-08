using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
	public class ErrorBox : MonoBehaviour
	{
		[SerializeField] private TMP_Text status;
		[SerializeField] private TMP_Text message;

		private void Awake()
		{
			gameObject.SetActive(false);
		}

		public void Show(ConnectionStatus stat, string message)
		{
			status.text = stat.ToString();
			this.message.text = message;
			gameObject.SetActive(true);
		}

		public void OnClose()
		{
			gameObject.SetActive(false);
		}
	}
}