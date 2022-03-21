
using UnityEngine;

namespace Utility
{
	public class LookAtCamera : MonoBehaviour
	{
		private void LateUpdate()
		{
			transform.LookAt(Camera.main.transform);
		}
	}
}