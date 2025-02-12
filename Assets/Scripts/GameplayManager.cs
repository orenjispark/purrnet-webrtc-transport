using PurrNet;
using UnityEngine;

public class GameplayManager : NetworkBehaviour
{
	public SyncVar<GameplayState> gameplayState;

	private int requirePlayer = 1;
	private int playerJoinedCount = 0;


	protected override void OnInitializeModules()
	{
		base.OnInitializeModules();

		gameplayState.value = GameplayState.Init;
	}

	protected override void OnSpawned()
	{
		base.OnSpawned();

		if (isServer)
		{
			networkManager.onPlayerJoined += OnPlayerJoined;
		}
	}

	protected override void OnDespawned()
	{
		base.OnDespawned();

		if (isServer)
		{
			networkManager.onPlayerJoined -= OnPlayerJoined;
		}
	}

	private void OnPlayerJoined(PlayerID playerId, bool isReconnect, bool asServer)
	{
		if (asServer) { return; }

		playerJoinedCount++;
		Debug.Log("player joined count " + playerJoinedCount);

		if (playerJoinedCount >= requirePlayer)
		{
			Server_StartGameplay();
		}
	}


	private void Server_StartGameplay()
	{
		gameplayState.value = GameplayState.Started;
		Debug.Log("[server] : gameplay started");
	}

	void Update()
	{
		if (!isSpawned || !isServer) { return; }
		if (!localPlayer.HasValue) { return; }
		if (!observers.Contains(localPlayer.Value)) { return; }
		if (gameplayState.value != GameplayState.Started) { return; }

		Server_SpawnEnemy();
	}

	[ServerRpc]
	private void Server_SpawnEnemy(RPCInfo info = default)
	{
		// Debug.Log("spawn enemy");
	}


	public enum GameplayState
	{
		Init,
		Started,
		Finished,
	}
}
