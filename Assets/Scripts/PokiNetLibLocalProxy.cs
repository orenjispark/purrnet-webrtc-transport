using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PokiNetLibLocalProxy
{
	public class ProxyPeer
	{
		public string networkId;
		public int connectionId;
		public bool isHost;

		public void OnMesage(string senderNetworkId, IntPtr arrayPtr, int length)
		{
			// Debug.Log($"{networkId} got message ...");
			// await Awaitable.WaitForSecondsAsync(0.1f);
			PokiNetLib.OnDataReceived(networkId, senderNetworkId, arrayPtr, length);
		}
	}

	public Dictionary<string, ProxyPeer> peers = new();
	private Action<string, string, int, bool> EvOnPeerConnected;


	public async Awaitable PokiNetlib_Connect(
		string gameId,
		string roomId,
		Action<string, string, string, string, bool> callback,
		Action<string, string, IntPtr, int> messageCallback,
		Action<string, string, int, bool> peerConnectedCallback
	)
	{
		EvOnPeerConnected = peerConnectedCallback;

		var isHost = roomId == "";
		if (isHost)
		{
			roomId = Guid.NewGuid().ToString().Substring(0, 4).ToUpper();
		}

		Debug.Log($"call connect to localproxy as {(isHost ? "host" : "client")}");

		//errString, networkId, roomId, isHost
		var networkId = Guid.NewGuid().ToString();
		var peer = new ProxyPeer()
		{
			networkId = networkId,
			isHost = isHost,
			connectionId = GetNextConnectionID(),
		};

		peers.Add(peer.networkId, peer);
		Debug.Log("peer added to list");

		await Awaitable.WaitForSecondsAsync(0.15f);


		foreach (var e in peers)
		{
			foreach (var p in peers)
			{
				if (e.Value == p.Value) { continue; }

				// Debug.Log($"invoke peer connected {peer.networkId} , {e.Value.networkId}");

				EvOnPeerConnected?.Invoke(e.Value.networkId, p.Value.networkId, p.Value.connectionId, p.Value.isHost);
			}
		}

		var host = peers.FirstOrDefault(e => e.Value.isHost == true).Value;
		callback?.Invoke("", host.networkId, networkId, roomId, isHost);
	}

	public async Awaitable PokiNetlib_ConnectClient(
		string gameId,
		string roomId,
		Action<string, string, string, string, int> callback,
		Action<string, string, IntPtr, int> messageCallback
	)
	{

		Debug.Log($"call connect to localproxy as client");

		//errString, networkId, roomId, isHost
		var networkId = Guid.NewGuid().ToString();
		var peer = new ProxyPeer()
		{
			networkId = networkId,
			isHost = false,
			connectionId = GetNextConnectionID(),
		};

		peers.Add(peer.networkId, peer);
		Debug.Log("peer added to list");



		foreach (var e in peers)
		{
			foreach (var p in peers)
			{
				if (e.Value == p.Value) { continue; }

				Debug.Log($"invoke peer connected {peer.networkId} , {e.Value.networkId}");
				EvOnPeerConnected?.Invoke(e.Value.networkId, p.Value.networkId, p.Value.connectionId, p.Value.isHost);
			}
		}

		await Awaitable.WaitForSecondsAsync(0.15f);

		var host = peers.FirstOrDefault(e => e.Value.isHost == true).Value;
		callback?.Invoke("", host.networkId, networkId, roomId, peer.connectionId);
	}


	public void PokiNetlib_SendMessage(
		string networkId,
		string targetNetworkId,
		IntPtr arrayPtr,
		int offset,
		int length
	)
	{
		// await Awaitable.NextFrameAsync();

		var peer = peers.FirstOrDefault(p => p.Value.networkId == networkId).Value;
		if (peer == null)
		{
			Debug.Log($"cannot send message. no peer found with networkId {networkId}");
			return;
		}

		var targetPeer = peers.FirstOrDefault(p => p.Value.networkId == targetNetworkId).Value;
		if (targetPeer == null)
		{
			Debug.Log($"cannot send message. no target peer found with networkId {targetNetworkId}");
			return;
		}


		// Debug.Log("sender and target found. sending message...");
		// await Awaitable.WaitForSecondsAsync(0.1f);
		// Debug.Log($"{networkId} send message to {targetNetworkId}");
		targetPeer.OnMesage(networkId, arrayPtr, length);
	}

	private int connectionId = 0;
	private int GetNextConnectionID()
	{
		connectionId++;
		return connectionId;
	}
}
