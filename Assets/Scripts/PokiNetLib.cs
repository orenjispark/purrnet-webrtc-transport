using System;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;

public class PokiNetLib : MonoBehaviour
{
	public class ConnectedArgs
	{
		public string hostNetworkId;
		public string networkId;
		public string roomId;
		public bool isHost;

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
		public string hostNetworkId;
		public string networkId;
		public string roomId;
		public int connectionId;

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
		public string networkId;
		public string peerNetworkId;
		public int peerConnectionId;
		public bool isHost;

		public PeerConnectedArgs(string networkId, string peerNetworkId, int peerConnectionId, bool isHost)
		{
			this.networkId = networkId;
			this.peerNetworkId = peerNetworkId;
			this.peerConnectionId = peerConnectionId;
			this.isHost = isHost;
		}
	}

	public event Action<ConnectedArgs> EvOnServerConnected;
	public event Action<ClientConnectedArgs> EvOnClientConnected;
	public static event Action<string, string, ArraySegment<byte>> EvOnMessage;
	public static event Action<PeerConnectedArgs> EvOnPeerConnected;

	public static PokiNetLib Instance;
	private PokiNetLibLocalProxy localProxy;

	public string currentRoomId = "";


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
		PokiNetlib_Connect(gameId, roomId, OnNetworkCallback, OnDataReceived, OnPeerConnectedCallback);		
#endif
	}

	public void ConnectClient(string gameId, string roomId)
	{
#if UNITY_EDITOR
		_ = localProxy.PokiNetlib_ConnectClient(gameId, roomId, OnClientNetworkCallback, OnDataReceived);
#else
		PokiNetlib_ConnectClient(gameId, roomId, OnClientNetworkCallback, OnDataReceived);		
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
			Instance.currentRoomId = roomId;
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
			Instance.currentRoomId = roomId;
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

	[DllImport("__Internal")]
	private static extern void PokiNetlib_Connect(
		string gameId,
		string roomId,
		Action<string, string, string, string, bool> callback,
		Action<string, string, IntPtr, int> messageCallback,
		Action<string, string, int, bool> peerConnectedCallback
	);

	[DllImport("__Internal")]
	private static extern void PokiNetlib_ConnectClient(
		string gameId,
		string roomId,
		Action<string, string, string, string, int> callback,
		Action<string, string, IntPtr, int> messageCallback
	);

	[DllImport("__Internal")]
	private static extern void PokiNetlib_SendMessage(
		string networkId,
		string targetNetworkId,
		IntPtr arrayPtr,
		int offset,
		int length
	);
}
