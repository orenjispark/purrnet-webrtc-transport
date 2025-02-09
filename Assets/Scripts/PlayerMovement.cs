using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
	public float movementSpeed = 5f;
	private Rigidbody2D rb;
	private Vector2 movement;

	private void Start()
	{
		rb = GetComponent<Rigidbody2D>();
	}

	private void Update()
	{
		// Get input from arrow keys
		movement.x = Input.GetAxisRaw("Horizontal"); // Left (-1) | Right (1)
		movement.y = Input.GetAxisRaw("Vertical");   // Down (-1) | Up (1)
		movement.Normalize();
	}

	private void FixedUpdate()
	{
		// Apply movement to Rigidbody2D
		rb.linearVelocity = movement * movementSpeed;
	}
}
