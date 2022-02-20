using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

/// <summary>
/// Handle player input by responding to Fusion input polling, filling an input struct and then working with
/// that input struct in the Fusion Simulation loop.
/// </summary>
public class CharacterInput : NetworkBehaviour, INetworkRunnerCallbacks
{
	public static bool FetchInput { get; set; } = true;
	
	[SerializeField]private Character _character;
	[SerializeField] private LayerMask _mouseRayMask;

	#region NetworkBehaviour
    /// <summary>
    /// Hook up to the Fusion callbacks so we can handle the input polling
    /// </summary>
    public override void Spawned()
    {
	    _character = GetComponent<Character>();
	    // Technically, it does not really matter which InputController fills the input structure, since the actual data will only be sent to the one that does have authority,
	    // but in the name of clarity, let's make sure we give input control to the gameobject that also has Input authority.
	    if (Object.HasInputAuthority)
	    {
		    Runner.AddCallbacks(this);
	    }

	    Debug.Log("Spawned [" + this + "] IsClient=" + Runner.IsClient + " IsServer=" + Runner.IsServer + " HasInputAuth=" + Object.HasInputAuthority + " HasStateAuth=" + Object.HasStateAuthority);
    }
    
    /// <summary>
    /// FixedUpdateNetwork is the main Fusion simulation callback - this is where
    /// we modify network state.
    /// </summary>
    public override void FixedUpdateNetwork()
    {
	    /*if (GameManager.playState == GameManager.PlayState.TRANSITION)
		    return;*/
	    
	    // Get our input struct and act accordingly. This method will only return data if we
	    // have Input or State Authority - meaning on the controlling player or the server.
	    Vector3 direction = default;
	    if (GetInput(out CharacterInputData input))
	    {
		    if (input.IsDown(CharacterButton.FORWARD))
		    {
			    direction += Vector3.forward;
		    }
		    else if (input.IsDown(CharacterButton.BACKWARD))
		    {
			    direction += Vector3.back;
		    }

		    if (input.IsDown(CharacterButton.LEFT))
		    {
			    direction += Vector3.left;
		    }
		    else if (input.IsDown(CharacterButton.RIGHT))
		    {
			    direction += Vector3.right;
		    }

		    if (input.IsDown(CharacterButton.FIRE_PRIMARY))
		    {
			    Debug.Log($"[{Runner.LocalPlayer.PlayerId}] Player FIRE_PRIMARY");
		    }

		    if (input.IsDown(CharacterButton.FIRE_SECONDARY))
		    {
			    Debug.Log($"[{Runner.LocalPlayer.PlayerId}] Player FIRE_PRIMARY");
		    }
		    
		    // We let the NetworkCharacterController do the actual work
		    _character.SetDirections(direction, input.AimDirection);
	    }
	    _character.Move();
    }

    #endregion
    
    /// <summary>
    /// Get Unity input and store them in a struct for Fusion
    /// </summary>
    /// <param name="runner">The current NetworkRunner</param>
    /// <param name="input">The target input handler that we'll pass our data to</param>
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
    	var frameworkInput = new CharacterInputData();

    	if (_character!=null && _character.Object!=null && _character.CurrentState == Character.State.ACTIVE && FetchInput)
    	{
    		// Instantiate our custom input structure
    		// Fill it with input data
    		if (Input.GetKey(KeyCode.W))
    		{
    			frameworkInput.Buttons |= CharacterButton.FORWARD;
    		}

    		if (Input.GetKey(KeyCode.S))
    		{
    			frameworkInput.Buttons |= CharacterButton.BACKWARD;
    		}

    		if (Input.GetKey(KeyCode.A))
    		{
    			frameworkInput.Buttons |= CharacterButton.LEFT;
    		}

    		if (Input.GetKey(KeyCode.D))
    		{
    			frameworkInput.Buttons |= CharacterButton.RIGHT;
    		}

    		if (Input.GetMouseButton(0))
    		{
    			frameworkInput.Buttons |= CharacterButton.FIRE_PRIMARY;
    		}

    		if (Input.GetMouseButton(1))
    		{
    			frameworkInput.Buttons |= CharacterButton.FIRE_SECONDARY;
    		}
            
    		frameworkInput.AimDirection = CalculateAim();
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

	    Vector3 aimDirection = mouseCollisionPoint - transform.position;
	    aimDirection.y = 0;
	    aimDirection.Normalize();
	    return aimDirection;
    }

    #region INetworkRunnerCallbacks
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
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    #endregion
}

[System.Flags]
public enum CharacterButton
{
    FIRE_PRIMARY = 1 << 0,
    FIRE_SECONDARY = 1 << 1,
    FORWARD = 1 << 2,
    BACKWARD = 1 << 3,
    LEFT = 1 << 4,
    RIGHT = 1 << 5
}

/// <summary>
/// Our custom definition of an INetworkStruct. Keep in mind that
/// * bool does not work (C# does not define a consistent size on different platforms)
/// * Must be a top-level struct (cannot be a nested class)
/// * Stick to primitive types and structs
/// * Size is not an issue since only modified data is serialized, but things that change often should be compact (e.g. button states)
/// </summary>
public struct CharacterInputData : INetworkInput
{
    public CharacterButton Buttons;
    public Vector3 AimDirection { get; set;}

    public bool IsUp(CharacterButton button)
    {
        return IsDown(button) == false;
    }

    public bool IsDown(CharacterButton button)
    {
        return (Buttons & button) == button;
    }
    
    public bool IsPressed(CharacterButton button)
    {
        return IsDown(button) == true;
    }
}