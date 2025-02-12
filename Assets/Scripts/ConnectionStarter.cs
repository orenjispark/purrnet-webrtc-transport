using System;
using System.Linq;
using PurrNet;
using PurrNet.Transports;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConnectionStarter : MonoBehaviour
{
	public NetworkManager networkManager;
	public StatisticsManager statisticsManager;

	public GameObject connectUIPanel;
	public TMP_InputField roomInputField;
	public Button hostBtn;
	public Button clientBtn;

	private void Start()
	{
		if (networkManager == null)
		{
			Debug.Log("no network manager found...");
			return;
		}

		hostBtn.onClick.AddListener(() =>
		{
			clientBtn.interactable = false;
			StartHost();
		});

		clientBtn.onClick.AddListener(() =>
		{
			hostBtn.interactable = false;
			StartClient();
		});

		networkManager.onClientConnectionState += (state) =>
		{
			if (state == ConnectionState.Connected)
			{
				HideUI();
				EnableAllBtns();
			}
		};
	}

	private void EnableAllBtns()
	{
		hostBtn.interactable = clientBtn.interactable = true;
	}

	private void HideUI()
	{
		connectUIPanel.SetActive(false);
	}

	private void ShowUI()
	{
		connectUIPanel.SetActive(true);
	}

	public void StartHost()
	{
		Debug.Log("start as host...");
		networkManager.onServerConnectionState += (state) =>
		{
			if (state == PurrNet.Transports.ConnectionState.Connected)
			{
				_ = DelayCall(1, () =>
				{
					networkManager.StartClient();
				});
			}
		};
		networkManager.StartServer();
	}

	public void StartClient()
	{
		Debug.Log("start as client...");
		FindFirstObjectByType<WebRTCTransport>().roomId = roomInputField.text;
		networkManager.StartClient();
	}

	// private void Update()
	// {
	// 	if (networkManager.clientState == ConnectionState.Connected)
	// 	{
	// 		// Debug.Log($"ping: {statisticsManager.ping}");
	// 	}
	// }

	private async Awaitable DelayCall(int delaySec, Action call)
	{
		await Awaitable.WaitForSecondsAsync(delaySec);
		call();
	}
}
