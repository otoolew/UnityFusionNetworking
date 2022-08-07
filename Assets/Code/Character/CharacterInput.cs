using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace UnityFusionNetworking
{
    [System.Flags]
    public enum EInputButton
    {
        USE = 0,
        FIRE = 1,
        FIRE_ALT = 2,
        RELOAD = 3,
        JUMP = 4,
        CROUCH = 5,
        SPRINT = 6,
        ACTION_01 = 7,
        ACTION_02 = 8,
        ACTION_03 = 9,
        ACTION_04 = 10
    }
    /// <summary>
    /// Custom definition of an INetworkStruct. Keep in mind that
    /// * bool does not work (C# does not define a consistent size on different platforms)
    /// * Must be a top-level struct (cannot be a nested class)
    /// * Stick to primitive types and structs
    /// * Size is not an issue since only modified data is serialized, but things that change often should be compact (e.g. button states)
    /// </summary>
    public struct CharacterInput : INetworkInput
    {
        public Vector2 MoveDirection;
        public Vector2 LookRotationDelta;
        public NetworkButtons Buttons;    
        public byte WeaponButton;
        public int WeaponSlot => WeaponButton - 1;
    }
}