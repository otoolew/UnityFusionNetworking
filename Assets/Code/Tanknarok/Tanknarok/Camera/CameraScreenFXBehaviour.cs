using UnityEngine;

namespace FusionExamples.Tanknarok
{
	public class CameraScreenFXBehaviour : MonoBehaviour
	{
		Kino.DigitalGlitch glitchEffect;
		[SerializeField] private float durationToTarget = 0.3f;
		float timer = 0;

		bool active = false;
		private float maxGlitch = 1;

		void Start()
		{
			glitchEffect = GetComponent<Kino.DigitalGlitch>();
			glitchEffect.enabled = false;
		}

		public void ToggleGlitch(bool value)
		{
			active = value;
		}

		// Update is called once per frame
		void Update()
		{
			float direction = active ? 1 : -1;
			if ((timer > 0 && direction == -1) || (timer < durationToTarget && direction == 1))
			{
				timer = Mathf.Clamp(timer + Time.deltaTime * direction, 0, durationToTarget);
				float t = timer / durationToTarget;
				glitchEffect.intensity = Mathf.Lerp(0, maxGlitch, t);

				if (timer == 0 && direction == -1 && glitchEffect.enabled)
				{
					glitchEffect.enabled = false;
				}
				else if (direction == 1 && !glitchEffect.enabled)
				{
					glitchEffect.enabled = true;
				}
			}
		}
	}
}