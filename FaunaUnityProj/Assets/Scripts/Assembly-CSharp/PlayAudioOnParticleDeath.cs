using UnityEngine;

public class PlayAudioOnParticleDeath : MonoBehaviour
{
	public ParticleSystem _particleSystem;

	public ParticleSystem.Particle[] _particles;

	private int currentParticles;

	public AudioSource particleAudio;

	private void LateUpdate()
	{
		int particleCount = _particleSystem.particleCount;
		if (particleCount < currentParticles)
		{
			particleAudio.pitch = Random.Range(0.93f, 1.05f);
			particleAudio.Play();
			WalkieTalkie.TransmitOneShotAudio(particleAudio, particleAudio.clip);
		}
		currentParticles = particleCount;
	}
}
