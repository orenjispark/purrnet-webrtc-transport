using UnityEngine;

public class SimpleCameraFollowSmooth : MonoBehaviour
{
	public Transform target;
	public float smoothSpeed = 5f;
	public Vector3 offset = new Vector3(0f, 0f, -10f);

	private void LateUpdate()
	{
		if (target == null) return;

		Vector3 desiredPosition = target.position + offset;
		transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
	}
}
