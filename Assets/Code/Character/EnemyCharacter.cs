using System;
using Fusion;
using UnityEngine;
using UnityEngine.UI;

[OrderAfter(typeof(HitboxManager))]
[RequireComponent(typeof(NetworkCharacterControllerPrototype))]
public class EnemyCharacter : Actor, IDamageable
{
	[Header("Visuals")] 
	[SerializeField] private Transform visualContainer;
	
	[SerializeField] private NetworkCharacterControllerPrototype networkCharacterController;
	public NetworkCharacterControllerPrototype NetworkCharacterController { get => networkCharacterController; set => networkCharacterController = value; }
	
    [SerializeField] private int pointValue;
    public int PointValue { get => pointValue; set => pointValue = value; }
    [Networked]public int HealthValue { get; set; }
    
    [SerializeField] private int healthMax;
    public int HealthMax { get => healthMax; set => healthMax = value; }
    
    [SerializeField] private Text healthText;
    public Text HealthText { get => healthText; set => healthText = value; }
    
    [SerializeField] private PlayerCharacter currentTarget;
    public PlayerCharacter CurrentTarget { get => currentTarget; set => currentTarget = value; }

    [Networked] public Vector3 MoveDirection { get; set; }
    [Networked] public Vector3 LookDirection { get; set; }
    [Networked] public Vector3 networkedVelocity { get; set; }
    private Vector3 _predictedVelocity;
    public Vector3 Velocity
    {
	    get => Object.IsPredictedSpawn ? _predictedVelocity : networkedVelocity;
	    set { if (Object.IsPredictedSpawn) _predictedVelocity = value; else networkedVelocity = value; }
    }
    [Networked(OnChanged = nameof(OnDestroyedChanged))]
    public NetworkBool networkedDestroyed { get; set; }
    private bool _predictedDestroyed;
    private bool destroyed
    {
	    get => Object.IsPredictedSpawn ? _predictedDestroyed : (bool)networkedDestroyed;
	    set { if (Object.IsPredictedSpawn) _predictedDestroyed = value; else networkedDestroyed = value; }
    }

    public override void InitNetworkState()
    {
	    
    }
    
    public override void Spawned()
    {
        base.Spawned();
        HealthValue = healthMax;
        healthText.text = HealthValue.ToString();
        // TODO: First pass will only find the first player. Implement a random select.
        PlayerCharacter[] targets = FindObjectsOfType<PlayerCharacter>();
        if (targets.Length > 0)
        {
            currentTarget = targets[0];
        }
        
    }
	public override void FixedUpdateNetwork()
	{
		if (currentTarget != null)
		{
			Move();
		}
	}

	private void Move()
	{
		Quaternion lookRotation = Quaternion.LookRotation(currentTarget.transform.position - transform.position);
		if (lookRotation.eulerAngles != Vector3.zero) // It already shouldn't be...
		{
			lookRotation.x = 0f;
			lookRotation.z = 0f;
			LookDirection =  lookRotation.eulerAngles;
			transform.rotation = lookRotation;
		}
		networkCharacterController.Move(transform.forward);
	}

	#region Network Static Calls
	public static void OnDestroyedChanged(Changed<NetworkBehaviour> changed)
	{
		((EnemyCharacter)changed.Behaviour)?.OnDestroyedChanged();
	}

	private void OnDestroyedChanged()
	{
		if (destroyed)
		{
			/*if (_explosionFX != null)
			{
				transform.up = Vector3.up;
				_explosionFX.PlayExplosion();
			}*/
			visualContainer.gameObject.SetActive(false);
		}
	}
	

	#endregion
	
	
    #region IDamagable
    public void TakeDamage(int damageValue)
    {
        HealthValue -= damageValue;
        healthText.text = HealthValue.ToString();
    }

    public void TakeDamage(int damageValue, Vector3 impulse)
    {
        HealthValue -= damageValue;
        healthText.text = HealthValue.ToString();
    }
    
    public void OnHit(PlayerRef player)
    {
        // Check behaviour on the host only.
        if (Object == null) return;
        if (Object.HasStateAuthority == false) return;
        
        if (Runner.TryGetPlayerObject(player, out var playerNetworkObject))
        {
            playerNetworkObject.GetComponent<NetworkPlayer>().AddToScore(pointValue);
        }

        Runner.Despawn(Object);
    }
    
    #endregion

    private void OnValidate()
    {
        if (healthText != null)
        {
            healthText.text = healthMax.ToString();
        }
    }


}
