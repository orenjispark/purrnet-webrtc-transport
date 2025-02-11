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

	public IReadOnlyList<Connection> connections => _connections;
	private List<Connection> _connections = new();

	public ConnectionState listenerState { get; private set; } = ConnectionState.Disconnected;
	public ConnectionState clientState { get; private set; } = ConnectionState.Disconnected;

	public event OnConnected onConnected;
	public event OnDisconnected onDisconnected;
	public event OnDataReceived onDataReceived;
	public event OnDataSent onDataSent;
	public event OnConnectionState onConnectionState;

	[Tooltip("should be valid UUID string")]
	public string gameId = "41a1e304-808e-49cc-833c-2c1e3cf54cd4";
	public string roomId = "";

	private string playerNetworkId = "";
	private string hostNetworkId = "";

	private bool startAsHost = false;
	private Dictionary<int, Peer> peers = new();

	public readonly Queue<BitPacker> serverQueue = new Queue<BitPacker>();
	public readonly Queue<BitPacker> clientQueue = new Queue<BitPacker>();
	static readonly BitPacker _packer = new BitPacker();


	private void Start()
	{
		// this get call on client and server
		PokiNetLib.EvOnMessage += OnMessage;

		// these get call on server only
		PokiNetLib.EvOnPeerConnected += Server_OnPeerConnected;
		PokiNetLib.EvOnPeerDisconnected += Server_OnPeerDisconnected;
	}

	private void OnMessage(string networkId, string senderNetworkId, ArraySegment<byte> odata)
	{
		var byteData = new ByteData(odata.Array, odata.Offset, odata.Count);

		if (networkId == hostNetworkId && startAsHost)
		{
			var senderPeer = peers.FirstOrDefault(e => e.Value.networkId == senderNetworkId).Value;

			// append senderClientConnectionId in front of msg
			_packer.ResetPositionAndMode(false);
			Packer<int>.Write(_packer, senderPeer.connectionId);
			_packer.WriteBytes(odata);

			var data = _packer.ToByteData();
			// Debug.Log("add msg que to server");
			QueuePacket(data, true);
		}
		else if (networkId == playerNetworkId)
		{
			// Debug.Log("add msg que to client");
			QueuePacket(byteData, false);
		}
	}

	private void Server_OnPeerConnected(PokiNetLib.PeerConnectedArgs data)
	{
		Debug.Log($"new peer connected : {data.networkId} {data.peerNetworkId}");
		var connection = new Connection(data.peerConnectionId);
		var peer = new Peer(connection.connectionId, data.peerNetworkId);

		peers.Add(connection.connectionId, peer);

		if (startAsHost)
		{
			_connections.Add(connection);
			onConnected?.Invoke(connection, true);
		}
	}

	private void Server_OnPeerDisconnected(PokiNetLib.PeerDisconnectedArgs data)
	{
		Debug.Log($"{data.networkId} detect peer disconnected : {data.peerNetworkId}");

		var peer = peers.FirstOrDefault(e => e.Value.networkId == data.peerNetworkId).Value;
		if (peer == null) { return; }

		var conn = new Connection(peer.connectionId);

		if (data.networkId == hostNetworkId && startAsHost)
		{
			for (int i = 0; i < _connections.Count; i++)
			{
				if (_connections[i] == conn)
				{
					_connections.RemoveAt(i);
					break;
				}
			}

			peers.Remove(peer.connectionId);
			onDisconnected?.Invoke(conn, DisconnectReason.ClientRequest, true);
		}
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
		pokiNetlib.ConnectClient(gameId, roomId);

		clientState = ConnectionState.Connecting;
		void OnConnected(PokiNetLib.ClientConnectedArgs data)
		{
			pokiNetlib.EvOnClientConnected -= OnConnected;
			playerNetworkId = data.networkId;
			hostNetworkId = data.hostNetworkId;
			Debug.Log($"player networkID {playerNetworkId}");

			clientState = ConnectionState.Connected;
			var connection = new Connection(data.connectionId);

			// _connections.Add(connection);

			onConnected?.Invoke(connection, false);
			onConnectionState?.Invoke(ConnectionState.Connected, false);
		}

		void OnDisconected(PokiNetLib.DisconectedArgs data)
		{
			if (data.networkId == playerNetworkId && !data.isHost)
			{
				Debug.Log("[client] : disconnected");

				pokiNetlib.EvOnDisconected -= OnDisconected;

				if (clientState == ConnectionState.Disconnected) { return; }
				clientState = ConnectionState.Disconnected;
				onDisconnected?.Invoke(default, DisconnectReason.ClientRequest, false);
			}
		}

		pokiNetlib.EvOnClientConnected += OnConnected;
		pokiNetlib.EvOnDisconected += OnDisconected;
	}

	public void Disconnect()
	{
		if (clientState != ConnectionState.Disconnected)
			onDisconnected?.Invoke(default, DisconnectReason.ClientRequest, false);

		playerNetworkId = "";
		roomId = "";
		clientQueue.Clear();

		if (clientState is ConnectionState.Connecting or ConnectionState.Connected)
			clientState = ConnectionState.Disconnecting;
		clientState = ConnectionState.Disconnected;
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
				pokiNetlib.EvOnServerConnected -= OnConnected;

				hostNetworkId = data.hostNetworkId;
				roomId = data.roomId;

				listenerState = ConnectionState.Connected;
				onConnectionState?.Invoke(ConnectionState.Connected, true);
			}
		}

		void OnNetworkDisconnected(PokiNetLib.DisconectedArgs data)
		{
			if (data.networkId == hostNetworkId && startAsHost)
			{
				Debug.Log("[host] : disconnected");

				// apakah perlu trigger utk client-host ini onDisconnected???
				// client-host = client yg 1 pc dengan server ini

				pokiNetlib.EvOnDisconected -= OnNetworkDisconnected;
				StopListening();
			}
		}

		pokiNetlib.EvOnServerConnected += OnConnected;
		pokiNetlib.EvOnDisconected += OnNetworkDisconnected;
	}

	public void StopListening()
	{
		serverQueue.Clear();
		_connections.Clear();
		peers.Clear();

		if (listenerState is ConnectionState.Connecting or ConnectionState.Connected)
			listenerState = ConnectionState.Disconnecting;
		listenerState = ConnectionState.Disconnected;
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

		var targetPeer = peers.GetValueOrDefault(target.connectionId);
		// Debug.Log($"Server send to client {target.connectionId} - {targetPeer.connectionId}");
		// Debug.Log(BitConverter.ToString(data.data));

		var newData = data;

		var arr = new ArraySegment<byte>(newData.data, newData.offset, newData.length);

		GCHandle handle = GCHandle.Alloc(newData.data, GCHandleType.Pinned);
		try
		{
			IntPtr ptr = handle.AddrOfPinnedObject();
			// Debug.Log($"server send to client {hostNetworkId} {targetPeer.networkId}");
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
		// Debug.Log("client send to server");
		// Debug.Log(BitConverter.ToString(data.data));

		var pokiNetlib = PokiNetLib.Instance;

		var arr = new ArraySegment<byte>(data.data, data.offset, data.length);
		GCHandle handle = GCHandle.Alloc(data.data, GCHandleType.Pinned);
		try
		{
			IntPtr ptr = handle.AddrOfPinnedObject();
			pokiNetlib.SendMessage(playerNetworkId, hostNetworkId, ptr, arr.Offset, arr.Count);
		}
		finally
		{
			RaiseDataSent(default, data, startAsHost);
			handle.Free();
		}
	}

	public void TickUpdate(float delta)
	{
		// throw new NotImplementedException();
		while (serverQueue.Count > 0 && startAsHost)
		{
			using var data = serverQueue.Dequeue();
			var byteData = data.ToByteData();

			byte[] arr = byteData.data;

			int senderConnectionid = BitConverter.ToInt32(arr, 0);
			var arrSegment = new ArraySegment<byte>(byteData.data, byteData.offset + 4, byteData.length - 4);
			byte[] bodyArray = arrSegment.ToArray(); // Creates a NEW array from the segment
			var bodyByteData = new ByteData(bodyArray, 0, bodyArray.Length);

			// Debug.Log($"[server] processing msg from {senderConnectionid} - {BitConverter.ToString(bodyByteData.data)}");

			RaiseDataReceived(new Connection(senderConnectionid), bodyByteData, true);
		}

		while (clientQueue.Count > 0)
		{
			using var data = clientQueue.Dequeue();
			var byteData = data.ToByteData();

			// onDataReceived?.Invoke(new Connection(0), byteData, false);
			// Debug.Log($"[client] processing msg from server - {BitConverter.ToString(byteData.data)}");
			RaiseDataReceived(default, byteData, false);
		}
	}

	protected override void StartClientInternal()
	{
		// throw new NotImplementedException();
		// Debug.Log("wt: start client interval...");
		Connect("", 0);
	}

	protected override void StartServerInternal()
	{
		// throw new NotImplementedException();
		// Debug.Log("wt: start server interval...");
		Listen(0);
	}
}
