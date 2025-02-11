using System;
using System.Collections.Generic;
using PurrNet.Transports;
using UnityEngine;



public class RelayTransport : GenericTransport, ITransport
{
	public const int TO_SERVER = 300;

	public override bool isSupported => true;
	public override ITransport transport => this;

	public IReadOnlyList<Connection> connections => new List<Connection>();
	public ConnectionState listenerState { get; private set; } = ConnectionState.Disconnected;
	public ConnectionState clientState { get; private set; } = ConnectionState.Disconnected;

	public event OnConnected onConnected;
	public event OnDisconnected onDisconnected;
	public event OnDataReceived onDataReceived;
	public event OnDataSent onDataSent;
	public event OnConnectionState onConnectionState;


	private int connectionCounter;
	public string ip;
	public ushort port;

	private List<WebsocketConnection> websocketConnections = new();
	private WebsocketConnection playerWsConnection;
	private Connection playerConnection;


	public void CloseConnection(Connection conn)
	{
		// throw new NotImplementedException();
		Debug.LogError("Not yet implemented : CloseConnection");
	}

	public void Connect(string ip, ushort port)
	{
		var ws = new WebsocketConnection(ip, port);

		ws.websocket.OnOpen += () =>
		{
			Debug.Log("[client] connected");

			// send to server to inform connection
			ws.websocket.SendText("connection");
		};

		ws.websocket.OnMessage += (bytes) =>
		{
			var strMsg = System.Text.Encoding.UTF8.GetString(bytes);

			Debug.Log($"[client] got message");

			if (strMsg.Contains("client-connected"))
			{
				var parts = strMsg.Split('#');
				string connecionIdStr = parts.Length > 1 ? parts[1] : "0";
				var connectionId = int.Parse(connecionIdStr);
				Debug.Log($"[client] client-connected {connectionId}");

				clientState = ConnectionState.Connected;

				var connection = new Connection(connectionId);
				onConnected?.Invoke(connection, false);

				// mock
				playerConnection = connection;
			}
			else
			{

			}
		};

		ws.StartConnection();
		websocketConnections.Add(ws);
		playerWsConnection = ws;
	}

	public void Disconnect()
	{
		Debug.LogError("Not yet implemented : Disconnect");
	}

	public void Listen(ushort port)
	{
		var ws = new WebsocketConnection(ip, port);

		ws.websocket.OnOpen += () =>
		{
			Debug.Log("[server] connected");

			listenerState = ConnectionState.Connected;
			onConnectionState?.Invoke(ConnectionState.Connected, true);
			var serverConnection = new Connection(0);
			onConnected?.Invoke(default, true);
		};

		ws.websocket.OnMessage += (bytes) =>
		{
			Debug.Log("[server] got message");

			var strMsg = System.Text.Encoding.UTF8.GetString(bytes);
			if (strMsg == "connection")
			{
				Debug.Log("a client just connected");
				var connectionId = GetNextConnectionID();
				var connection = new Connection(connectionId);

				onConnected?.Invoke(connection, true);

				// send ke client
				ws.websocket.SendText("client-connected#" + connectionId);
			}
			else
			{
				int intSize = sizeof(int); // int is 4 bytes in C#

				// Extract the last 4 bytes and convert them back to an int
				int extractedInt = BitConverter.ToInt32(bytes, bytes.Length - intSize);

				if (extractedInt == TO_SERVER)
				{
					// Create a new byte array without the last 4 bytes
					byte[] msgBody = new byte[bytes.Length - intSize];
					Array.Copy(bytes, msgBody, msgBody.Length);
					// ws.websocket.Send(msgBody);

					//RaiseDataReceived(default, )
				}
			}
		};

		ws.StartConnection();
		websocketConnections.Add(ws);
	}

	public void RaiseDataReceived(Connection conn, ByteData data, bool asServer)
	{
		onDataReceived?.Invoke(conn, data, asServer);
	}

	public void RaiseDataSent(Connection conn, ByteData data, bool asServer)
	{
		onDataSent?.Invoke(conn, data, asServer);
	}

	public void SendToClient(Connection target, ByteData data, Channel method = Channel.ReliableOrdered)
	{
		throw new NotImplementedException();
	}

	public void SendToServer(ByteData byteData, Channel method = Channel.ReliableOrdered)
	{
		// throw new NotImplementedException();
		byte[] arr = byteData.data;
		byte[] intBytes = BitConverter.GetBytes(TO_SERVER);

		byte[] msg = new byte[arr.Length + intBytes.Length];
		Array.Copy(arr, 0, msg, 0, arr.Length);
		Array.Copy(intBytes, 0, msg, arr.Length, intBytes.Length);


		playerWsConnection.SendBytesMessage(msg);
		RaiseDataSent(playerConnection, byteData, false);
	}

	public void StopListening()
	{
		// throw new NotImplementedException();
		Debug.Log("Not yet implemented : Stop Listening");
	}

	public void TickUpdate(float delta)
	{
		// throw new NotImplementedException();
		foreach (var ws in websocketConnections)
		{
			ws.Tick();
		}
	}

	protected override void StartClientInternal()
	{
		// throw new NotImplementedException();
		Connect(ip, port);
	}

	protected override void StartServerInternal()
	{
		Listen(port);
	}

	private int GetNextConnectionID()
	{
		connectionCounter++;
		return connectionCounter;
	}
}
