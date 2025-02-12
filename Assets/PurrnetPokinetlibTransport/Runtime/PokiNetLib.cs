using System;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;

public class PokiNetLib : MonoBehaviour
{
	public event Action<ConnectedArgs> EvOnServerConnected;
	public event Action<ClientConnectedArgs> EvOnClientConnected;
	public event Action<DisconectedArgs> EvOnDisconected;


	public static event Action<string, string, ArraySegment<byte>> EvOnMessage;
	public static event Action<PeerConnectedArgs> EvOnPeerConnected;
	public static event Action<PeerDisconnectedArgs> EvOnPeerDisconnected;

	public static PokiNetLib Instance;
	private PokiNetLibLocalProxy localProxy;



	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			localProxy = new PokiNetLibLocalProxy();
			DontDestroyOnLoad(gameObject);
		}
		else
		{
			Destroy(gameObject);
		}
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}

		EvOnPeerConnected = null;
		EvOnServerConnected = null;
	}

	// connect with roomId = "" , will connect as host
	// otherwise will connect as client
	public void Connect(string gameId, string roomId)
	{
		// Debug.Log("network connecting...");
#if UNITY_EDITOR
		_ = localProxy.PokiNetlib_Connect(gameId, roomId, OnNetworkCallback, OnDataReceived, OnPeerConnectedCallback);
#else
		PokiNetlib_Connect(gameId, roomId, OnNetworkCallback, OnDataReceived, OnPeerConnectedCallback, OnDisconected, OnPeerDisconnected);		
#endif
	}

	public void ConnectClient(string gameId, string roomId)
	{
#if UNITY_EDITOR
		_ = localProxy.PokiNetlib_ConnectClient(gameId, roomId, OnClientNetworkCallback, OnDataReceived);
#else
		PokiNetlib_ConnectClient(gameId, roomId, OnClientNetworkCallback, OnDataReceived, OnDisconected);		
#endif
	}

	public void SendMessage(string networkId, string targetNetworkId, IntPtr arrayPtr, int offset, int length)
	{
		// Debug.Log("send message...");
#if UNITY_EDITOR
		localProxy.PokiNetlib_SendMessage(networkId, targetNetworkId, arrayPtr, offset, length);
#else
		PokiNetlib_SendMessage(networkId, targetNetworkId, arrayPtr, offset, length);	
#endif
	}

	[MonoPInvokeCallback(typeof(Action<string, string, string, string, bool>))]
	public static void OnNetworkCallback(
		string errMessage, string hostNetworkId, string networkId,
		string roomId, bool isHost
	)
	{
		if (errMessage != "")
		{
			Debug.Log($"NetworkConnect error with message , {errMessage}");
			return;
		}

		// Debug.Log($"unity receive callback from jslib: "
		// 	+ $"connected with networkId {networkId}"
		// 	+ $"\nroomId : {roomId}"
		// 	+ $"\nisHost: {isHost}");
		if (Instance)
		{
			Instance.EvOnServerConnected?.Invoke(new ConnectedArgs(hostNetworkId, networkId, roomId, isHost));
		}
	}

	[MonoPInvokeCallback(typeof(Action<string, string, string, string, int>))]
	public static void OnClientNetworkCallback(
		string errMessage, string hostNetworkId,
		string networkId, string roomId,
		int connectionId
	)
	{
		if (errMessage != "")
		{
			Debug.Log($"ClientNetworkConnect error with message , {errMessage}");
			return;
		}

		// Debug.Log($"unity receive callback from jslib: "
		// 	+ $"connected with networkId {networkId}"
		// 	+ $"\nroomId : {roomId}"
		// 	+ $"\nisHost: {isHost}");
		if (Instance)
		{
			Instance.EvOnClientConnected?.Invoke(new ClientConnectedArgs(hostNetworkId, networkId, roomId, connectionId));
		}
	}

	[MonoPInvokeCallback(typeof(Action<string, string, int, bool>))]
	public static void OnPeerConnectedCallback(
		string networkId,
		string peerNetworkId,
		int peerConnectionId,
		bool isHost
	)
	{
		// Debug.Log($"unity receive peer connected callback from jslib: "
		// 	+ $"connected with networkId {networkId}"
		// 	+ $"\npeerNetworkId : {peerNetworkId}");

		EvOnPeerConnected?.Invoke(new PeerConnectedArgs(networkId, peerNetworkId, peerConnectionId, isHost));
	}


	[MonoPInvokeCallback(typeof(Action<string, string, IntPtr, int>))] // Helps with garbage collection
	public static void OnDataReceived(string networkId, string senderNetworkId, IntPtr arrayPtr, int length)
	{
		byte[] receivedData = new byte[length];
		Marshal.Copy(arrayPtr, receivedData, 0, length);
		// Convert IntPtr to byte[]
		// Debug.Log("Received Data: " + BitConverter.ToString(receivedData));
		// Debug.Log("Raw " + receivedData);
		// Debug.Log("Length " + receivedData.Length);

		// Debug.Log($"from : {networkId}");
		// Debug.Log("before emit evonmessage");
		EvOnMessage?.Invoke(networkId, senderNetworkId, receivedData);
		// Debug.Log("after emit evonmessage");
	}

	[MonoPInvokeCallback(typeof(Action<string, bool>))]
	public static void OnDisconected(string networkId, bool isHost)
	{
		if (Instance)
		{
			Instance.EvOnDisconected?.Invoke(new DisconectedArgs(networkId, isHost));
		}
	}

	[MonoPInvokeCallback(typeof(Action<string, string>))]
	public static void OnPeerDisconnected(string networkId, string peerNetworkId)
	{
		EvOnPeerDisconnected?.Invoke(new PeerDisconnectedArgs(networkId, peerNetworkId));
	}

	[DllImport("__Internal")]
	private static extern void PokiNetlib_Connect(
		string gameId,
		string roomId,
		Action<string, string, string, string, bool> callback,
		Action<string, string, IntPtr, int> messageCallback,
		Action<string, string, int, bool> peerConnectedCallback,
		Action<string, bool> networkDisconnectedCalback,
		Action<string, string> peerDisconnectedCallback
	);

	[DllImport("__Internal")]
	private static extern void PokiNetlib_ConnectClient(
		string gameId,
		string roomId,
		Action<string, string, string, string, int> callback,
		Action<string, string, IntPtr, int> messageCallback,
		Action<string, bool> networkDisconnectedCalback
	);

	[DllImport("__Internal")]
	private static extern void PokiNetlib_SendMessage(
		string networkId,
		string targetNetworkId,
		IntPtr arrayPtr,
		int offset,
		int length
	);



	public class ConnectedArgs
	{
		public readonly string hostNetworkId;
		public readonly string networkId;
		public readonly string roomId;
		public readonly bool isHost;

		public ConnectedArgs(string hostNetworkId, string networkId, string roomId, bool isHost)
		{
			this.hostNetworkId = hostNetworkId;
			this.networkId = networkId;
			this.roomId = roomId;
			this.isHost = isHost;
		}
	}

	public class ClientConnectedArgs
	{
		public readonly string hostNetworkId;
		public readonly string networkId;
		public readonly string roomId;
		public readonly int connectionId;

		public ClientConnectedArgs(string hostNetworkId, string networkId, string roomid, int connectionId)
		{
			this.hostNetworkId = hostNetworkId;
			this.networkId = networkId;
			this.connectionId = connectionId;
			this.roomId = roomid;
		}
	}

	public class PeerConnectedArgs
	{
		public readonly string networkId;
		public readonly string peerNetworkId;
		public readonly int peerConnectionId;
		public readonly bool isHost;

		public PeerConnectedArgs(string networkId, string peerNetworkId, int peerConnectionId, bool isHost)
		{
			this.networkId = networkId;
			this.peerNetworkId = peerNetworkId;
			this.peerConnectionId = peerConnectionId;
			this.isHost = isHost;
		}
	}

	public class DisconectedArgs
	{
		public readonly string networkId;
		public readonly bool isHost;

		public DisconectedArgs(string networkId, bool isHost)
		{
			this.networkId = networkId;
			this.isHost = isHost;
		}
	}

	public class PeerDisconnectedArgs
	{
		public readonly string networkId;
		public readonly string peerNetworkId;

		public PeerDisconnectedArgs(string networkId, string peerNetworkId)
		{
			this.networkId = networkId;
			this.peerNetworkId = peerNetworkId;
		}
	}

}
