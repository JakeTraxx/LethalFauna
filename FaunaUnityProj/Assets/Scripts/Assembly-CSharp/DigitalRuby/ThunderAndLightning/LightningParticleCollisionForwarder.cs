using System.Collections.Generic;
using UnityEngine;

namespace DigitalRuby.ThunderAndLightning
{
	[RequireComponent(typeof(ParticleSystem))]
	public class LightningParticleCollisionForwarder : MonoBehaviour
	{
		[Tooltip("The script to forward the collision to. Must implement ICollisionHandler.")]
		public MonoBehaviour CollisionHandler;

		private ParticleSystem _particleSystem;

		private readonly List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();

		private void Start()
		{
			_particleSystem = GetComponent<ParticleSystem>();
		}

		private void OnParticleCollision(GameObject other)
		{
			if (CollisionHandler is ICollisionHandler collisionHandler)
			{
				int num = _particleSystem.GetCollisionEvents(other, collisionEvents);
				if (num != 0)
				{
					collisionHandler.HandleCollision(other, collisionEvents, num);
				}
			}
		}
	}
}
