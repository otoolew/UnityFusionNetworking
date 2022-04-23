using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
/// <summary>
/// Our custom definition of an INetworkStruct. Keep in mind that
/// * bool does not work (C# does not define a consistent size on different platforms)
/// * Must be a top-level struct (cannot be a nested class)
/// * Stick to primitive types and structs
/// * Size is not an issue since only modified data is serialized, but things that change often should be compact (e.g. button states)
/// </summary>
public struct CharacterInputData : INetworkInput
{
    public const uint USE = 1 << 0;
    public const uint FIRE = 1 << 1;
    public const uint FIRE_ALT = 1 << 2;

    public const uint FORWARD = 1 << 3;
    public const uint BACKWARD = 1 << 4;
    public const uint LEFT = 1 << 5;
    public const uint RIGHT = 1 << 6;

    public const uint JUMP = 1 << 7;
    public const uint CROUCH = 1 << 8;
    public const uint SPRINT = 1 << 9;

    public const uint ACTION_01 = 1 << 10;
    public const uint ACTION_02 = 1 << 11;
    public const uint ACTION_03 = 1 << 12;
    public const uint ACTION_04 = 1 << 13;

    public const uint RELOAD = 1 << 14;
    
    public Vector3 AimDirection;

    public uint Buttons;

    public bool IsUp(uint button)
    {
        return IsDown(button) == false;
    }

    public bool IsDown(uint button)
    {
        return (Buttons & button) == button;
    }
}
