using PurrNet;
using UnityEngine;

public class PlayerColorChanger : NetworkBehaviour
{
	public SpriteRenderer spriteRenderer;


	protected override void OnSpawned()
	{
		if (isOwner)
		{
			Server_RandomColor();
		}
	}

	[ServerRpc]
	private void Server_RandomColor()
	{
		var colors = new Color[] { Color.red, Color.green, Color.blue, Color.yellow, Color.cyan, Color.magenta };
		var randomColor = colors[Random.Range(0, colors.Length)];

		Observers_ChangeColor(randomColor);
	}

	[ObserversRpc(bufferLast: true)]
	public void Observers_ChangeColor(Color color)
	{
		spriteRenderer.color = color;
	}
}
