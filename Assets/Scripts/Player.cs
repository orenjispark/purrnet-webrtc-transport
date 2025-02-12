using PurrNet;
using UnityEngine;

public class Player : NetworkBehaviour
{
	public Transform visual;

	protected override void OnSpawned()
	{
		base.OnSpawned();

		if (isOwner && localPlayer.HasValue)
		{
			var cameraFollow = FindFirstObjectByType<SimpleCameraFollowSmooth>();
			if (cameraFollow == null) { return; }

			cameraFollow.target = visual;
		}
	}
}
