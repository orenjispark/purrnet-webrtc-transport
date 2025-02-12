mergeInto(LibraryManager.library, {
	
	
	// gameId: uuid sting
	// roomId: string or empty string
	// callback : static(err:string, networkId:string, roomId:string, isHost: bool)=>void
    PokiNetlib_Connect: async function(
		gameId, roomId, callback, 
		messageCallback, peerConnectedCallback,
		networkDisconnectedCallback,
		peerDisconnectedCallback
	) {
		gameId = UTF8ToString(gameId)
		roomId = UTF8ToString(roomId);
		
		const isHost = roomId == "" ? 1 : 0;
		
		const pokinetlibJS = window.pokinetlibjs.pokinetlibJS;
		
		function connectionCallbackBridge(errMsg, hostNetworkId, networkId, roomId, isHost){
			// console.log(`bridge connection callback to unity...`);
			// console.log(errMsg, networkId, roomId, isHost);
			
			if(isHost){
				window.hostNetworkId = networkId;
			}
			
			const errBuffer = stringToNewUTF8(errMsg);
			const networkIdBuffer  = stringToNewUTF8(networkId);
			const roomIdBuffer = stringToNewUTF8(roomId);
			const hostNetworkIdBuffer = stringToNewUTF8(hostNetworkId);
			
			{{{ makeDynCall('viiiii', 'callback') }}} (errBuffer, hostNetworkIdBuffer, networkIdBuffer, roomIdBuffer, isHost);
			
			_free(errBuffer);
			_free(hostNetworkIdBuffer);
			_free(networkIdBuffer);
			_free(roomIdBuffer);
		}
		
		function networkDisconnectedCallbackBridge(networkId, isHost){
			const networkIdBuffer  = stringToNewUTF8(networkId);
			
			{{{ makeDynCall('vii', 'networkDisconnectedCallback') }}} ( networkIdBuffer, isHost);
			
			_free(networkIdBuffer);
		}
		
		function messageCallbackBridge(networkId, senderNetworkId, data){
			// console.log('bridge message callback to unity');
			// console.log(networkId, senderNetworkId, data);
			
			//  if (!(data instanceof ArrayBuffer)) {
			// 	console.error("Data is not an ArrayBuffer!");
			// 	return;
			// }

			// Convert ArrayBuffer to Uint8Array
			const message = new Uint8Array(data);
			const size = message.byteLength;  

			// console.log("Converted message:", message);
			// console.log("Message length:", size);

			if (size === 0) {
				console.error("Message is empty!");
				return;
			}

			// Allocate memory in Unity's WebAssembly heap
			const dataPtr = _malloc(size);
			if (!dataPtr) {
				console.error("Memory allocation failed!");
				return;
			}

			const heap = new Uint8Array(HEAPU8.buffer, dataPtr, size);
			heap.set(message);  // Copy existing byte array into allocated memory

			
			const networkIdBuffer = stringToNewUTF8(networkId);;
			const senderNetworkIdBuffer = stringToNewUTF8(senderNetworkId);

			// console.log("sending to Unity:", message);
			{{{ makeDynCall('viiii', 'messageCallback') }}} (networkIdBuffer, senderNetworkIdBuffer, dataPtr, size);
			_free(dataPtr); // Free memory after sending
			_free(networkIdBuffer); // Free memory after sending
			_free(senderNetworkIdBuffer);
		}
		
		function peerConnectedCallbackBridge(networkId, peerNetworkId, peerConnectionId){
			// console.log('bridge peer connected callback to unity');
			// console.log(networkId, peerNetworkId);
			
			const networkIdBuffer  = stringToNewUTF8(networkId);
			const peerNetworkIdBuffer = stringToNewUTF8(peerNetworkId);
			
			const isPeerAHost = peerNetworkId == window.hostNetworkId;
			
			{{{ makeDynCall('viiii', 'peerConnectedCallback') }}} (networkIdBuffer, peerNetworkIdBuffer, peerConnectionId, isPeerAHost);

			_free(networkIdBuffer);
			_free(peerNetworkIdBuffer);
		}
		
		function peerDisconnectedCallbackBridge(networkId, peerNetworkId){
			const networkIdBuffer  = stringToNewUTF8(networkId);
			const peerNetworkIdBuffer = stringToNewUTF8(peerNetworkId);
			
			{{{ makeDynCall('vii', 'peerDisconnectedCallback') }}} (networkIdBuffer, peerNetworkIdBuffer);
			
			_free(networkIdBuffer);
			_free(peerNetworkIdBuffer);
		}
		
		function emptyFunc(){
			
		}
		
        // console.log("call connect on jslib with gameId", gameId);
        // const roomId = window.crypto.randomUUID();
		pokinetlibJS.init(gameId);
		
		if(isHost == 1){
			pokinetlibJS.connectAsHost(
				connectionCallbackBridge, messageCallbackBridge, 
				peerConnectedCallbackBridge, networkDisconnectedCallbackBridge, 
				peerDisconnectedCallbackBridge
			);	
		}
    },
	
	 PokiNetlib_ConnectClient: async function(
		gameId, roomId, callback, 
		messageCallback, networkDisconnectedCallback
	) {
		gameId = UTF8ToString(gameId)
		roomId = UTF8ToString(roomId);
		
		const pokinetlibJS = window.pokinetlibjs.pokinetlibJS;
		
		function connectionCallbackBridge(errMsg, hostNetworkId, networkId, roomId, connectionId){
			const errBuffer = stringToNewUTF8(errMsg);
			let networkIdBuffer  = stringToNewUTF8(networkId);
			
			
			const roomIdBuffer = stringToNewUTF8(roomId);
			const hostNetworkIdBuffer = stringToNewUTF8(hostNetworkId);
			
			{{{ makeDynCall('viiiii', 'callback') }}} (errBuffer, hostNetworkIdBuffer, networkIdBuffer, roomIdBuffer, connectionId);
			
			_free(errBuffer);
			_free(hostNetworkIdBuffer);
			_free(networkIdBuffer);
			_free(roomIdBuffer);
		}
		
		function networkDisconnectedCallbackBridge(networkId, isHost){
			const networkIdBuffer  = stringToNewUTF8(networkId);
			
			{{{ makeDynCall('vii', 'networkDisconnectedCallback') }}} ( networkIdBuffer, isHost);
			
			_free(networkIdBuffer);
		}
		
		function messageCallbackBridge(networkId, senderNetworkId, data){
			// console.log('bridge message callback to unity');
			// console.log(networkId, senderNetworkId, data);
			
			//  if (!(data instanceof ArrayBuffer)) {
			// 	console.error("Data is not an ArrayBuffer!");
			// 	return;
			// }

			// Convert ArrayBuffer to Uint8Array
			const message = new Uint8Array(data);
			const size = message.byteLength;  

			// console.log("Converted message:", message);
			// console.log("Message length:", size);

			if (size === 0) {
				console.error("Message is empty!");
				return;
			}

			// Allocate memory in Unity's WebAssembly heap
			const dataPtr = _malloc(size);
			if (!dataPtr) {
				console.error("Memory allocation failed!");
				return;
			}

			const heap = new Uint8Array(HEAPU8.buffer, dataPtr, size);
			heap.set(message);  // Copy existing byte array into allocated memory

			const networkIdBuffer = stringToNewUTF8(networkId);;
			const senderNetworkIdBuffer = stringToNewUTF8(senderNetworkId);

			// console.log("sending to Unity:", message);
			{{{ makeDynCall('viiii', 'messageCallback') }}} (networkIdBuffer, senderNetworkIdBuffer, dataPtr, size);
			_free(dataPtr); // Free memory after sending
			_free(networkIdBuffer); // Free memory after sending
			_free(senderNetworkIdBuffer);
		}
		
		pokinetlibJS.init(gameId);
		pokinetlibJS.connectAsClient(
			roomId, connectionCallbackBridge, 
			messageCallbackBridge, 
			networkDisconnectedCallbackBridge
		);
	 },
	
	PokiNetlib_SendMessage: function(networkId, targetNetworkId, arrayPtr, offset, length){
		networkId = UTF8ToString(networkId);
		targetNetworkId = UTF8ToString(targetNetworkId);
		
		var byteArray = new Uint8Array(Module.HEAPU8.buffer, arrayPtr + offset, length);
        // console.log("Received data:", byteArray);
        // Example: Convert to string (if data is text)
        // var text = new TextDecoder("utf-8").decode(byteArray);
		// console.log(`${networkId} try to send message to ${targetNetworkId} : `, byteArray , text);
		
		const pokinetlibJS = window.pokinetlibjs.pokinetlibJS;
		
		pokinetlibJS.sendSingle(networkId,targetNetworkId, byteArray);
	},
});
