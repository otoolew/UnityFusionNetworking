using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using LogType = UnityEngine.LogType;

public class KillLocalPlayer : NetworkBehaviour
{
    #region Behaviour
    
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        base.Despawned(runner, hasState);
        DebugLogMessage.Log(Color.green,$"NetworkBehaviour -> Despawned({runner.LocalPlayer.PlayerId}, {hasState})");
    }
    
    public void Kill() 
    {
        
        if (GameManager.Instance.GetPlayerCharacter(out Character character, Object.Runner.LocalPlayer))
        {
            DebugLogMessage.Log(Color.red,$"KillLocalPlayer -> Kill({Object.Runner.LocalPlayer.PlayerId})");
            //DebugLogMessage.Log(Color.red,$"KillLocalPlayer -> Kill({character.CurrentState.ToString()})");
        }
    }
    
    #endregion
    
}
