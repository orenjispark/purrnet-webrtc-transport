using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using PurrNet.Packing;
using PurrNet.Transports;
using UnityEngine;

public class Peer
{
	public readonly int connectionId;
	public readonly string networkId;

	public Peer(int connectionId, string networkId)
	{
		this.connectionId = connectionId;
		this.networkId = networkId;
	}
}

public class WebRTCTransport : GenericTransport, ITransport
{
	public override bool isSupported => true;
	public override ITransport transport => this;

	public IReadOnlyList<Connection> connections => new List<Connection>() { };

	public ConnectionState listenerState { get; private set; } = ConnectionState.Disconnected;
	public ConnectionState clientState { get; private set; } = ConnectionState.Disconnected;

	public event OnConnected onConnected;
	public event OnDisconnected onDisconnected;
	public event OnDataReceived onDataReceived;
	public event OnDataSent onDataSent;
	public event OnConnectionState onConnectionState;

	public string gameId = "41a1e304-808e-49cc-833c-2c1e3cf54cd4";
	public string roomId = "";

	private int connectionIdCounter = 0;


	private string playerNetworkId = "";
	private string hostNetworkId = "";

	private bool startAsHost = false;
	private Peer server;
	private Dictionary<int, Peer> peers = new();


	public readonly Queue<BitPacker> serverQueue = new Queue<BitPacker>();
	public readonly Queue<BitPacker> clientQueue = new Queue<BitPacker>();

	static readonly BitPacker _packer = new BitPacker();


	/*
	- wehn start host, 2 network will be created (listen, connect) and that 1 unity-instance will have 2 network, server and client
	- when start as client (connect) a network will be created and act as client
	
	server
	- if in server mode. onConnected will be called after the network is created and successfully create a room
	
	client
	- if in client mode. onConnected will be called when the network is created and successfully join a room
	*/

	private void Start()
	{
		// this get call on client and server
		PokiNetLib.EvOnMessage += (networkId, senderNetworkId, odata) =>
		{
			var byteData = new ByteData(odata.Array, odata.Offset, odata.Count);

			if (networkId == server.networkId && startAsHost)
			{
				Debug.Log("add msg que to server");
				var senderPeer = peers.FirstOrDefault(e => e.Value.networkId == senderNetworkId).Value;

				if (senderPeer == null)
				{
					Debug.LogError("cannot add message to server que. no client peer found with networkId : " + senderNetworkId);
					return;
				}

				// append senderClientConnectionId in front of msg
				_packer.ResetPositionAndMode(false);
				Packer<int>.Write(_packer, senderPeer.connectionId);
				_packer.WriteBytes(odata);

				var data = _packer.ToByteData();
				QueuePacket(data, true);
			}
			else if (networkId == playerNetworkId)
			{
				Debug.Log("add msg que to client");
				QueuePacket(byteData, false);
			}
		};

		// this get call on server only
		PokiNetLib.EvOnPeerConnected += (data) =>
		{
			Debug.Log($"new peer connected : {data.networkId} {data.peerNetworkId}");
			var connection = new Connection(GetNextConnectionID());
			var peer = new Peer(connection.connectionId, data.peerNetworkId);

			peers.Add(connection.connectionId, peer);

			if (startAsHost)
			{
				onConnected?.Invoke(connection, true);
			}
		};
	}

	private int GetNextConnectionID()
	{
		connectionIdCounter++;
		return connectionIdCounter;
	}

	public void CloseConnection(Connection conn)
	{
		Debug.Log("should disconnect " + conn.connectionId);
		Debug.LogWarning("not implemented yet");
	}

	public void Connect(string ip, ushort port)
	{
		Debug.Log($"connect as client with gameId : {gameId} roomId : {roomId}");

		if (roomId == "")
		{
			Debug.Log("cancel connect as client. no roomId assigned");
			return;
		}

		var pokiNetlib = PokiNetLib.Instance;
		pokiNetlib.Connect(gameId, roomId);

		clientState = ConnectionState.Connecting;
		void OnConnected(PokiNetLib.ConnectedArgs data)
		{
			pokiNetlib.EvOnConnected -= OnConnected;
			playerNetworkId = data.networkId;
			hostNetworkId = data.hostNetworkId;
			Debug.Log($"player networkID {playerNetworkId}");

			if (data.isHost == false)
			{
				clientState = ConnectionState.Connected;
				var connection = new Connection(0);


				_ = DelayCall(4, () =>
				{
					onConnectionState?.Invoke(ConnectionState.Connected, false);
					onConnected?.Invoke(connection, false);
				});

			}
		}

		pokiNetlib.EvOnConnected += OnConnected;
	}

	public void Disconnect()
	{
		Debug.Log("wt: disconnect...");
		Debug.LogWarning("not implemented yet");
	}

	public void Listen(ushort port)
	{
		var pokiNetlib = PokiNetLib.Instance;
		startAsHost = true;
		pokiNetlib.Connect(gameId, "");
		listenerState = ConnectionState.Connecting;

		void OnConnected(PokiNetLib.ConnectedArgs data)
		{
			Debug.Log($"server connected : {data.networkId} , roomId : {data.roomId} ,  {data.isHost}");
			if (data.isHost == true)
			{
				pokiNetlib.EvOnConnected -= OnConnected;

				var connection = new Connection(0);
				var serverPeer = new Peer(connection.connectionId, data.networkId);
				this.server = serverPeer;

				hostNetworkId = data.hostNetworkId;
				roomId = data.roomId;

				listenerState = ConnectionState.Connected;
				onConnectionState?.Invoke(ConnectionState.Connected, true);
			}
		}

		pokiNetlib.EvOnConnected += OnConnected;
	}

	private async Awaitable DelayCall(int delaySec, Action call)
	{
		await Awaitable.WaitForSecondsAsync(delaySec);
		call();
	}

	public void StopListening()
	{
		// throw new NotImplementedException();
		Debug.Log("wt: stop listening...");
		Debug.LogWarning("not implemented yet");
	}

	public void RaiseDataReceived(Connection conn, ByteData data, bool asServer)
	{
		onDataReceived?.Invoke(conn, data, asServer);
	}

	public void RaiseDataSent(Connection conn, ByteData data, bool asServer)
	{
		onDataSent?.Invoke(conn, data, asServer);
	}

	private void QueuePacket(ByteData data, bool asServer)
	{
		var datac = BitPackerPool.Get();
		datac.WriteBytes(data);

		if (asServer)
		{
			serverQueue.Enqueue(datac);
		}
		else
		{
			clientQueue.Enqueue(datac);
		}
	}


	public void SendToClient(Connection target, ByteData data, Channel method = Channel.ReliableOrdered)
	{
		var pokiNetlib = PokiNetLib.Instance;
		if (!pokiNetlib)
		{
			Debug.Log("no pokinetlib found");
		}

		Debug.Log($"send to client {target.connectionId}");
		var targetPeer = peers.GetValueOrDefault(target.connectionId);
		if (targetPeer == null)
		{
			Debug.Log($"server cant send to connectionId {target.connectionId}. peer not found on clients list");
			string keysAsString = string.Join(", ", peers.Keys.Select(k => k.ToString()));
			Debug.Log($"available client list : {keysAsString}");
			return;
		}

		var targetConnectionId = targetPeer.connectionId;
		var newData = data;

		var arr = new ArraySegment<byte>(newData.data, newData.offset, newData.length);

		GCHandle handle = GCHandle.Alloc(newData.data, GCHandleType.Pinned);
		try
		{
			IntPtr ptr = handle.AddrOfPinnedObject();
			Debug.Log($"server send to client {hostNetworkId} {targetPeer.networkId}");
			pokiNetlib.SendMessage(hostNetworkId, targetPeer.networkId, ptr, arr.Offset, arr.Count);
		}
		finally
		{
			RaiseDataSent(target, newData, true);
			handle.Free();
		}
	}

	public void SendToServer(ByteData data, Channel method = Channel.ReliableOrdered)
	{
		Debug.Log("client send to server");
		var pokiNetlib = PokiNetLib.Instance;
		if (!pokiNetlib)
		{
			Debug.Log("no pokinetlib found");
		}

		var arr = new ArraySegment<byte>(data.data, data.offset, data.length);

		GCHandle handle = GCHandle.Alloc(data.data, GCHandleType.Pinned);
		try
		{
			IntPtr ptr = handle.AddrOfPinnedObject();
			pokiNetlib.SendMessage(playerNetworkId, hostNetworkId, ptr, arr.Offset, arr.Count);
		}
		finally
		{
			RaiseDataSent(default, data, false);
			handle.Free();
		}
	}

	public void TickUpdate(float delta)
	{
		// throw new NotImplementedException();
		while (serverQueue.Count > 0)
		{
			using var data = serverQueue.Dequeue();
			var byteData = data.ToByteData();

			byte[] arr = byteData.data;

			int senderConnectionid = BitConverter.ToInt32(arr, 0);
			var arrSegment = new ArraySegment<byte>(byteData.data, byteData.offset + 4, data.length - 4);
			var bodyByteData = new ByteData(arrSegment.Array, arrSegment.Offset, arrSegment.Count);

			// mock
			var clientPeer = peers.FirstOrDefault(e => e.Value.connectionId == senderConnectionid).Value;
			if (clientPeer == null)
			{
				Debug.Log($"cant process serverMsgQue. client peer not found with connectionId : {senderConnectionid}");
				return;
			}

			onDataReceived?.Invoke(new Connection(clientPeer.connectionId), bodyByteData, true);
		}

		while (clientQueue.Count > 0)
		{
			using var data = clientQueue.Dequeue();
			var byteData = data.ToByteData();
			onDataReceived?.Invoke(default, byteData, false);
		}
	}

	protected override void StartClientInternal()
	{
		// throw new NotImplementedException();
		Debug.Log("wt: start client interval...");
		Connect("", 0);
	}

	protected override void StartServerInternal()
	{
		// throw new NotImplementedException();
		Debug.Log("wt: start server interval...");
		Listen(0);
	}
}
