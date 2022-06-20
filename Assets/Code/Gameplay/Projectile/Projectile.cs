using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

[OrderAfter(typeof(HitboxManager))]
public class Projectile : Actor
{

	
		/*public interface ITargetVisuals
		{
			void InitializeTargetMarker(Vector3 launchPos, Vector3 bulletVelocity, Bullet.BulletSettings bulletSettings);
			void Destroy();
		}*/
		
		[SerializeField] private LayerMask hitMask;
		public LayerMask HitMask { get => hitMask; set => hitMask = value; }

		[Header("Visuals")] 
		[SerializeField] private Transform visualContainer;
		//[SerializeField] ExplosionFX _explosionFX;

		[Header("Settings")]
		[SerializeField] private ProjectileData projectileData;
		public ProjectileData ProjectileData { get => projectileData; set => projectileData = value; }
		[Serializable]
		public class BulletSettings 
		{
			public LayerMask hitMask;
			public byte damage;
			public float speed = 100;
			public float radius = 0.05f;
			public float gravity = 0f;
			public float timeToLive = 1.5f;
			public float timeToFade = 0.5f;
			public float ownerVelocityMultiplier = 1f;
		}
		
		/// <summary>
		/// Because Bullet.cs uses predictive spawning, we have two different sets of properties:
		/// Networked and Predicted, hidden behind a common front that exposes the relevant value depending on the current state of the object.
		/// This allow us to use the same code in both the predicted and the confirmed state.
		/// </summary>
		[Networked]
		public TickTimer networkedLifeTimer { get; set; }
		private TickTimer _predictedLifeTimer;
		private TickTimer lifeTimer
		{
			get => Object.IsPredictedSpawn ? _predictedLifeTimer : networkedLifeTimer;
			set { if (Object.IsPredictedSpawn) _predictedLifeTimer = value;else networkedLifeTimer = value; }
		}

		[Networked]
		public TickTimer networkedFadeTimer { get; set; }
		private TickTimer _predictedFadeTimer;
		private TickTimer fadeTimer
		{
			get => Object.IsPredictedSpawn ? _predictedFadeTimer : networkedFadeTimer;
			set { if (Object.IsPredictedSpawn) _predictedFadeTimer = value;else networkedFadeTimer = value; }
		}

		[Networked]
		public Vector3 networkedVelocity { get; set; }
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

		private List<LagCompensatedHit> _areaHits = new List<LagCompensatedHit>();
		/*private ITargetVisuals _targetVisuals;

		private void Awake()
		{
			_targetVisuals = GetComponent<ITargetVisuals>();
		}*/

		/// <summary>
		/// PreSpawn is invoked directly when Spawn() is called, before any network state is shared, so this is where we initialize networked properties.
		/// </summary>
		/// <param name="ownervelocity"></param>
		public override void InitNetworkState(Vector3 ownervelocity)
		{
			lifeTimer = TickTimer.CreateFromSeconds(Runner, projectileData.timeToLive + projectileData.timeToFade);
			fadeTimer = TickTimer.CreateFromSeconds(Runner, projectileData.timeToFade);

			destroyed = false;

			Vector3 fwd = transform.forward.normalized;
			Vector3 vel = ownervelocity.normalized;
			vel.y = 0;
			fwd.y = 0;
			float multiplier = Mathf.Abs(Vector3.Dot(vel, fwd));
			
			Velocity = projectileData.speed * transform.forward + ownervelocity * multiplier * projectileData.ownerVelocityMultiplier;
		}

		/// <summary>
		/// Spawned() is invoked on all clients when the networked object is created. 
		/// Note that because Bullets are pooled, we need to reset every local property when spawning.
		/// It's entirely likely that this bullet instance has already been used and no longer has its default values.
		/// </summary>
		public override void Spawned()
		{
			/*if (_explosionFX != null)
				_explosionFX.ResetExplosion();*/
			visualContainer.gameObject.SetActive(true);

			if (Velocity.sqrMagnitude > 0)
				visualContainer.forward = Velocity;

			visualContainer.forward = transform.forward;

			/*if(_targetVisuals!=null)
				_targetVisuals.InitializeTargetMarker(transform.position, Velocity, _bulletSettings);*/

			// We want bullet interpolation to use predicted data on all clients because we're moving them in FixedUpdateNetwork()
			GetComponent<NetworkTransform>().InterpolationDataSource = InterpolationDataSources.Predicted;
		}

		private void OnDestroy()
		{
			// Explicitly destroy the target marker because it may not currently be a child of the bullet
			//_targetVisuals?.Destroy();
		}

		/// <summary>
		/// Simulate bullet movement and check for collision.
		/// This executes on all clients using the Velocity and last validated state to predict the correct state of the object
		/// </summary>
		public override void FixedUpdateNetwork()
		{
			if (!lifeTimer.Expired(Runner))
			{
				Move();
			}
			else
			{
				Runner.Despawn(Object);
			}
		}

		private void Move()
		{
			Transform xfrm = transform;
			float dt = Runner.DeltaTime;
			Vector3 vel = Velocity;
			float speed = vel.magnitude;
			Vector3 pos = xfrm.position;

			if (!destroyed)
			{
				if (fadeTimer.Expired(Runner))
				{
					Detonate(transform.position);
				}
				else
				{
					vel.y += dt * projectileData.gravity;

					// We move the origin back from the actual position to make sure we can't shoot through things even if we start inside them
					Vector3 dir = vel.normalized;
					if (Runner.LagCompensation.Raycast(pos -0.5f*dir, dir, Mathf.Max(projectileData.radius, speed * dt), Object.InputAuthority, out var hitinfo, hitMask.value, HitOptions.IncludePhysX))
					{
						vel = HandleImpact(hitinfo);
						pos = hitinfo.Point;
					}
				}
			}

			// If the bullet is destroyed, we stop the movement so we don't get a flying explosion
			if (destroyed)
			{
				vel = Vector3.zero;
				dt = 0;
			}

			Velocity = vel;
			pos += dt * Velocity;

			xfrm.position = pos;
			if(vel.sqrMagnitude>0)
				visualContainer.forward = vel.normalized;
		}

		/// <summary>
		/// Bullets will detonate when they expire or on impact.
		/// After detonating, the mesh will disappear and it will no longer collide.
		/// If specified, an impact fx may play and area damage may be applied.
		/// </summary>
		private void Detonate(Vector3 hitPoint)
		{
			if (destroyed)
				return;
			// Mark the bullet as destroyed.
			// This will trigger the OnDestroyedChanged callback which makes sure the explosion triggers correctly on all clients.
			// Using an OnChange callback instead of an RPC further ensures that we don't trigger the explosion in a different frame from
			// when the bullet stops moving (That would lead to moving explosions, or frozen bullets)
			destroyed = true;

			/*if (_bulletSettings.areaRadius > 0)
			{
				ApplyAreaDamage(hitPoint);
			}*/
		}

		public static void OnDestroyedChanged(Changed<NetworkBehaviour> changed)
		{
			((Projectile)changed.Behaviour)?.OnDestroyedChanged();
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
		

		private Vector3 HandleImpact(LagCompensatedHit hit)
		{
			
			Transform xfrm = transform;
			float dt = Runner.DeltaTime;
			Vector3 vel = Velocity;
			float speed = vel.magnitude;
			Vector3 pos = xfrm.position;
			
			if (hit.Hitbox != null)
			{
				NetworkObject netobj = hit.Hitbox.Root.Object;
				if (netobj != null && Object!=null && netobj.InputAuthority == Object.InputAuthority)
					return Velocity; // Don't let us hit ourselves - this is esp. important with lag compensation since, if we move backwards, we're very likely to hit our own ghost from a previous frame.
			}

			IDamageable damageable = hit.GameObject.GetComponent<IDamageable>();
			if (damageable != null)
			{
				damageable.TakeDamage(projectileData.damage);
			}
			Detonate(hit.Point);

			return Vector3.zero;
		}

#if UNITY_EDITOR
		private void OnDrawGizmos()
		{
			Gizmos.color = Color.green;
			Gizmos.DrawWireSphere(transform.position, projectileData.radius);
		}
#endif
}
