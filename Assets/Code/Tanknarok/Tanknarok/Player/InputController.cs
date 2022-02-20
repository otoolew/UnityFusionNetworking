using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

namespace FusionExamples.Tanknarok
{
	/// <summary>
	/// Handle player input by responding to Fusion input polling, filling an input struct and then working with
	/// that input struct in the Fusion Simulation loop.
	/// </summary>
	public class InputController : NetworkBehaviour, INetworkRunnerCallbacks
	{
		private Player _player;
		[SerializeField] private LayerMask _mouseRayMask;

		public static bool fetchInput = true;
		private bool _toggleReady = false;

		/// <summary>
		/// Hook up to the Fusion callbacks so we can handle the input polling
		/// </summary>
		public override void Spawned()
		{
			_player = GetComponent<Player>();
			// Technically, it does not really matter which InputController fills the input structure, since the actual data will only be sent to the one that does have authority,
			// but in the name of clarity, let's make sure we give input control to the gameobject that also has Input authority.
			if (Object.HasInputAuthority)
			{
				Runner.AddCallbacks(this);
			}

			Debug.Log("Spawned [" + this + "] IsClient=" + Runner.IsClient + " IsServer=" + Runner.IsServer + " HasInputAuth=" + Object.HasInputAuthority + " HasStateAuth=" + Object.HasStateAuthority);
		}

		/// <summary>
		/// Get Unity input and store them in a struct for Fusion
		/// </summary>
		/// <param name="runner">The current NetworkRunner</param>
		/// <param name="input">The target input handler that we'll pass our data to</param>
		public void OnInput(NetworkRunner runner, NetworkInput input)
		{
			var frameworkInput = new NetworkInputData();

			if (_player!=null && _player.Object!=null && _player.state == Player.State.Active && fetchInput)
			{
				// Instantiate our custom input structure
				// Fill it with input data
				if (Input.GetKey(KeyCode.W))
				{
					frameworkInput.Buttons |= NetworkInputData.BUTTON_FORWARD;
				}

				if (Input.GetKey(KeyCode.S))
				{
					frameworkInput.Buttons |= NetworkInputData.BUTTON_BACKWARD;
				}

				if (Input.GetKey(KeyCode.A))
				{
					frameworkInput.Buttons |= NetworkInputData.BUTTON_LEFT;
				}

				if (Input.GetKey(KeyCode.D))
				{
					frameworkInput.Buttons |= NetworkInputData.BUTTON_RIGHT;
				}

				if (Input.GetMouseButton(0))
				{
					frameworkInput.Buttons |= NetworkInputData.BUTTON_FIRE_PRIMARY;
				}

				if (Input.GetMouseButton(1))
				{
					frameworkInput.Buttons |= NetworkInputData.BUTTON_FIRE_SECONDARY;
				}

				if (_toggleReady)
				{
					_toggleReady = false;
					frameworkInput.Buttons |= NetworkInputData.READY;
				}

				frameworkInput.aimDirection = CalculateAim();
			}

			// Hand over the data to Fusion
			input.Set(frameworkInput);
		}

		private Vector3 CalculateAim()
		{
			Vector3 mousePos = Input.mousePosition;

			RaycastHit hit;
			Ray ray = Camera.main.ScreenPointToRay(mousePos);

			Vector3 mouseCollisionPoint = Vector3.zero;
			// Raycast towards the mouse collider box in the world
			if (Physics.Raycast(ray, out hit, Mathf.Infinity, _mouseRayMask))
			{
				if (hit.collider != null)
				{
					mouseCollisionPoint = hit.point;
				}
			}

			Vector3 aimDirection = mouseCollisionPoint - _player.turretPosition;
			aimDirection.y = 0;
			aimDirection.Normalize();
			return aimDirection;
		}

		public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
		{
		}

		public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
		{
		}

		private void Update()
		{
			_toggleReady = _toggleReady || Input.GetKeyDown(KeyCode.R);
		}

		/// <summary>
		/// FixedUpdateNetwork is the main Fusion simulation callback - this is where
		/// we modify network state.
		/// </summary>
		public override void FixedUpdateNetwork()
		{
			if (GameManagerDeprecated.playState == GameManagerDeprecated.PlayState.TRANSITION)
				return;
			// Get our input struct and act accordingly. This method will only return data if we
			// have Input or State Authority - meaning on the controlling player or the server.
			Vector3 direction = default;
			if (GetInput(out NetworkInputData input))
			{
				if (input.IsDown(NetworkInputData.BUTTON_FORWARD))
				{
					direction += Vector3.forward;
				}
				else if (input.IsDown(NetworkInputData.BUTTON_BACKWARD))
				{
					direction += Vector3.back;
				}

				if (input.IsDown(NetworkInputData.BUTTON_LEFT))
				{
					direction += Vector3.left;
				}
				else if (input.IsDown(NetworkInputData.BUTTON_RIGHT))
				{
					direction += Vector3.right;
				}

				if (input.IsDown(NetworkInputData.BUTTON_FIRE_PRIMARY))
				{
					_player.shooter.FireWeapon(WeaponManager.WeaponInstallationType.PRIMARY);
				}

				if (input.IsDown(NetworkInputData.BUTTON_FIRE_SECONDARY))
				{
					_player.shooter.FireWeapon(WeaponManager.WeaponInstallationType.SECONDARY);
				}

				if (input.IsDown(NetworkInputData.READY))
				{
					_player.ToggleReady();
				}

				// We let the NetworkCharacterController do the actual work
				_player.SetDirections(direction, input.aimDirection);
			}
			_player.Move();
		}

		public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
		public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
		public void OnConnectedToServer(NetworkRunner runner) { }
		public void OnDisconnectedFromServer(NetworkRunner runner) { }
		public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
		public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
		public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
		public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
		public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
		public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }
		public void OnSceneLoadDone(NetworkRunner runner) { }
		public void OnSceneLoadStart(NetworkRunner runner) { }
	}

	/// <summary>
	/// Our custom definition of an INetworkStruct. Keep in mind that
	/// * bool does not work (C# does not define a consistent size on different platforms)
	/// * Must be a top-level struct (cannot be a nested class)
	/// * Stick to primitive types and structs
	/// * Size is not an issue since only modified data is serialized, but things that change often should be compact (e.g. button states)
	/// </summary>
	public struct NetworkInputData : INetworkInput
	{
		public const uint BUTTON_FIRE_PRIMARY = 1 << 0;
		public const uint BUTTON_FIRE_SECONDARY = 1 << 1;
		public const uint BUTTON_FORWARD = 1 << 2;
		public const uint BUTTON_BACKWARD = 1 << 3;
		public const uint BUTTON_LEFT = 1 << 4;
		public const uint BUTTON_RIGHT = 1 << 5;
		public const uint READY = 1 << 6;

		public uint Buttons;
		public Vector3 aimDirection;

		public bool IsUp(uint button)
		{
			return IsDown(button) == false;
		}

		public bool IsDown(uint button)
		{
			return (Buttons & button) == button;
		}
	}
}