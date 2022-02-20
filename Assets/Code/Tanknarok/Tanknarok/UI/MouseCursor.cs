using UnityEngine;

namespace FusionExamples.Tanknarok
{
	public class MouseCursor : MonoBehaviour
	{
		RectTransform _rect;
		[SerializeField] private CursorLockMode _cursorLockState = CursorLockMode.None;

		void Start()
		{
			_rect = GetComponent<RectTransform>();

			if (!Application.isEditor)
				Cursor.visible = false;
			Cursor.lockState = _cursorLockState;
		}

		private void LateUpdate()
		{
			_rect.position = Input.mousePosition;
		}
	}
}