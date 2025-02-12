using PurrNet;
using TMPro;

public class PlayerNameText : NetworkBehaviour
{
	public TMP_Text playerNameText;


	protected override void OnSpawned()
	{
		base.OnSpawned();
		Observers_SetPlayerName();
	}

	[ObserversRpc]
	private void Observers_SetPlayerName()
	{
		var ownerId = "";
		if (owner.HasValue)
		{
			ownerId = owner.ToString();
		}

		playerNameText.text = $"Player_{ownerId}";
	}
}
