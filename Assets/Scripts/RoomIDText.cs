using PurrNet;
using PurrNet.Transports;
using TMPro;
using UnityEngine;


public class RoomIDText : NetworkBehaviour
{
	public TMP_Text roomText;

	protected override void OnSpawned()
	{
		base.OnSpawned();

		if (networkManager.transport.GetType() == typeof(WebRTCTransport))
		{
			var roomId = (networkManager.transport as WebRTCTransport).roomId;
			roomText.text = $"WebRTC roomID : {roomId}";
		}
		else if (networkManager.transport.GetType() == typeof(PurrTransport))
		{
			var roomName = (networkManager.transport as PurrTransport).roomName;
			roomText.text = $"PurrTransport roomName : {roomName}";
		}

	}
}
