using Fusion;
using UnityEngine;

namespace UnityFusionNetworking
{
    public class AbilityController : NetworkBehaviour
    {
        [SerializeField] private Ability currentWeapon;
        //[SerializeField] private PlayerCharacter playerCharacter;
        [Networked] public TickTimer primaryFireDelay { get; set; }

        /// <summary>
        /// Fire the current weapon. This is called from the Input Auth Client and on the Server in
        /// response to player input. Input Auth Client spawns a dummy shot that gets replaced by the networked shot
        /// whenever it arrives
        /// </summary>
        public void FireWeapon()
        {
            TickTimer tickTimer = primaryFireDelay;
            if (tickTimer.ExpiredOrNotRunning(Runner))
            {
                //currentWeapon.Fire(Runner, Object.InputAuthority, playerCharacter.Velocity);
                primaryFireDelay = TickTimer.CreateFromSeconds(Runner, currentWeapon.delay);
            }
        }
    }
}