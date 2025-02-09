using System;
using System.Linq;
using PurrNet;
using PurrNet.Transports;
using TMPro;
using UnityEngine;

public class ConnectionStarter : MonoBehaviour
{
	public NetworkManager networkManager;
	public StatisticsManager statisticsManager;

	public TMP_InputField roomInputField;

	private void Start()
	{
		if (networkManager == null)
		{
			Debug.Log("no network manager found...");
			return;
		}
		// var mppmTag = CurrentPlayer.ReadOnlyTags().ToList();

		// if (mppmTag.Contains("Host"))
		// {
		// 	purrTransport.roomName = Guid.NewGuid().ToString(); ;
		// 	Debug.Log($"room id: {purrTransport.roomName}");
		// 	Debug.Log("start as host...");
		// 	networkManager.StartServer();
		// 	networkManager.onServerConnectionState += (state) =>
		// 	{
		// 		if (state == PurrNet.Transports.ConnectionState.Connected)
		// 		{
		// 			networkManager.StartClient();
		// 		}
		// 	};
		// }
		// else if (mppmTag.Contains("Client"))
		// {
		// 	Debug.Log($"room id: {purrTransport.roomName}");
		// 	Debug.Log("start as client...");
		// 	_ = DelayConnectAsClient();
		// }
	}

	public void StartHost()
	{
		//Debug.Log($"room id: {purrTransport.roomName}");
		Debug.Log("start as host...");

		networkManager.onServerConnectionState += (state) =>
		{
			if (state == PurrNet.Transports.ConnectionState.Connected)
			{
				// FindFirstObjectByType<WebRTCTransport>().roomId = roomInputField.text;
				_ = DelayCall(2, () =>
				{
					networkManager.StartClient();
				});
			}
		};
		networkManager.StartServer();
	}

	private async Awaitable DelayCall(int delaySec, Action call)
	{
		await Awaitable.WaitForSecondsAsync(delaySec);
		call();
	}

	private void Update()
	{
		if (networkManager.clientState == ConnectionState.Connected)
		{
			// Debug.Log($"ping: {statisticsManager.ping}");
		}
	}

	public void StartClient()
	{
		//purrTransport.roomName = "dd84e7a7-0577-4600-9698-f363975ed25d";
		//Debug.Log($"room id: {purrTransport.roomName}");
		Debug.Log("start as client...");
		FindFirstObjectByType<WebRTCTransport>().roomId = roomInputField.text;
		networkManager.StartClient();
	}

	private async Awaitable DelayConnectAsClient()
	{
		await Awaitable.WaitForSecondsAsync(1);
		networkManager.StartClient();
	}
}
