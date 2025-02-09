using System.Collections.Generic;
using PurrNet.Packing;
using PurrNet.Transports;
using UnityEngine;

public class MyNetwork
{
	public int ConnectionIdCounter = 0;
	public List<Connection> connections = new();

	private WebRTCTransport webRTCTransport;

	public readonly Queue<BitPacker> serverQueue = new Queue<BitPacker>();
	public readonly Queue<BitPacker> clientQueue = new Queue<BitPacker>();


	public MyNetwork(WebRTCTransport webRTCTransport)
	{
		this.webRTCTransport = webRTCTransport;
	}

	public int GetNextConnectionID()
	{
		ConnectionIdCounter++;
		return ConnectionIdCounter;
	}

	public void AddPlayerConnection(Connection connection, bool asServer)
	{
		connections.Add(connection);
		Debug.Log("add player connection");
	}

	public void RemovePlayerConnection(Connection connection)
	{
		connections.Remove(connection);
	}
}