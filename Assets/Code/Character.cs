using Fusion;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Visual representation of a Player - the Character is instantiated by the map once it's loaded.
/// This class handles camera tracking and player movement and is destroyed when the map is unloaded.
/// (I.e. the player gets a new avatar in each map)
/// </summary>

public class Character : NetworkBehaviour
{
	[SerializeField] private Text _name;
	[SerializeField] private MeshRenderer _mesh;

	private Transform _camera;
	private Player _player;

	public override void Spawned()
	{
		_player = GameManager.Instance.GetPlayer(Object.InputAuthority);
		_name.text = _player.Name;
		_mesh.material.color = _player.Color;
	}

	public override void Render()
	{
		if (Object.HasInputAuthority)
		{
			if (_camera == null)
				_camera = Camera.main.transform;
			Transform t = _mesh.transform;
			Vector3 p = t.position;
			_camera.position = p - 10 * t.forward + 5*Vector3.up;
			_camera.LookAt(p+2*Vector3.up);
		}
	}

	public override void FixedUpdateNetwork()
	{
		if (_player && _player.InputEnabled && GetInput(out InputData data))
		{
			if (data.GetButton(ButtonFlag.LEFT))
				transform.Rotate(Vector3.up,-Runner.DeltaTime*180);
			if (data.GetButton(ButtonFlag.RIGHT))
				transform.Rotate(Vector3.up,Runner.DeltaTime*180);
			if (data.GetButton(ButtonFlag.FORWARD))
				transform.position += Runner.DeltaTime * 10 * transform.forward;
			if (data.GetButton(ButtonFlag.BACKWARD))
				transform.position -= Runner.DeltaTime * 10 * transform.forward;
		}
	}
}