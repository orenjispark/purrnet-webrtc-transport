using System;
using System.Runtime.InteropServices;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PokiNetLibTestUI : MonoBehaviour
{
	[SerializeField] private Button _startHostBtn;
	[SerializeField] private Button _startClientBtn;

	[Space]
	[SerializeField] private TMP_InputField _clientRoomIdInputField;
	[SerializeField] private TMP_Text _hostRoomIdText;

	[Space]
	[SerializeField] private TMP_InputField _msgField;
	[SerializeField] private Button _sendAsServerBtn;
	[SerializeField] private Button _sendAsClientBtn;

	private string hostNetworkId = "";
	private string clientNetworkId = "";

	private string gameId = "d28132e0-76ad-4317-9436-b216b09021b9";

	private void Start()
	{
		var pokiNetLib = PokiNetLib.Instance;
		//var gameId = Guid.NewGuid().ToString();

		_startHostBtn.onClick.AddListener(() =>
		{
			pokiNetLib.Connect(gameId, "");
		});

		_startClientBtn.onClick.AddListener(() =>
		{
			var roomId = _clientRoomIdInputField.text;
			if (roomId == "" || roomId == null) { return; }
			pokiNetLib.Connect(gameId, roomId);
		});

		_sendAsClientBtn.onClick.AddListener(() =>
		{
			var msg = _msgField.text;
			var arr = ConvertStringToArraySegment(msg);

			// Pin the array in memory
			GCHandle handle = GCHandle.Alloc(arr.Array, GCHandleType.Pinned);
			try
			{
				// Get a pointer to the array
				IntPtr ptr = IntPtr.Add(handle.AddrOfPinnedObject(), arr.Offset);

				// Send the message to JS
				pokiNetLib.SendMessage(clientNetworkId, hostNetworkId, ptr, arr.Offset, arr.Count); // Pass pointer, offset, and length to JS
			}
			finally
			{
				handle.Free(); // Always free GCHandle after usage
			}
		});

		_sendAsServerBtn.onClick.AddListener(() =>
		{
			var msg = _msgField.text;
			var arr = ConvertStringToArraySegment(msg);

			// Pin the array in memory
			GCHandle handle = GCHandle.Alloc(arr.Array, GCHandleType.Pinned);
			try
			{
				// Get a pointer to the array
				IntPtr ptr = IntPtr.Add(handle.AddrOfPinnedObject(), arr.Offset);

				// Send the message to JS
				pokiNetLib.SendMessage(hostNetworkId, clientNetworkId, ptr, arr.Offset, arr.Count); // Pass pointer, offset, and length to JS
			}
			finally
			{
				handle.Free(); // Always free GCHandle after usage
			}
		});

		pokiNetLib.EvOnServerConnected += (data) =>
		{
			if (data.isHost)
			{
				_hostRoomIdText.text = data.roomId;
				hostNetworkId = data.networkId;
			}
			else
			{
				clientNetworkId = data.networkId;
			}
		};
	}

	public ArraySegment<byte> ConvertStringToArraySegment(string str)
	{
		// Convert the string to a byte array (using UTF8 encoding)
		byte[] byteArray = Encoding.UTF8.GetBytes(str);

		// Create an ArraySegment<byte> from the byte array
		ArraySegment<byte> segment = new ArraySegment<byte>(byteArray);

		return segment;
	}
}
