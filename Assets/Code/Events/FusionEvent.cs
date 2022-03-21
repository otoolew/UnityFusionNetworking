using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu]
public class FusionEvent : ScriptableObject
{
    public List<UnityAction<PlayerRef, NetworkRunner>> Responses = new List<UnityAction<PlayerRef, NetworkRunner>>();

    public void Raise(PlayerRef player = default, NetworkRunner runner = null)
    {
        for (int i = 0; i < Responses.Count; i++)
        {
            Responses[i]?.Invoke(player, runner);
        }
    }

    public void RegisterResponse(UnityAction<PlayerRef, NetworkRunner> response)
    {
        Responses.Add(response);
    }
        
    public void RemoveResponse(UnityAction<PlayerRef, NetworkRunner> response)
    {
        if (Responses.Contains(response))
            Responses.Remove(response);
    }
}
