using System;

namespace UnityFusionNetworking
{
    [Serializable]
    public enum ConnectionStatus
    {

        Disconnected,
        Connecting,
        Connected,
        Failed,
        EnteringLobby,
        InLobby,
        Starting,
        Started
    }
}
