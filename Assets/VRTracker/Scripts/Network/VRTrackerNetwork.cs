using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class VRTrackerNetwork : NetworkManager
{
    /* VR Tracker
     * VR Tracker Network is overloading the network manager to have custom behavior on specific network event or action
     * It holds the different player game objects
     */
    public static VRTrackerNetwork instance;

    private bool isWaitingForIP = false;
    public bool playerSpawned = false;
    public List<GameObject> players;

    void Start()
    {
        if (instance != null)
        {
            Debug.LogError("More than one VRTracker Network in the scene");
        }
        else
        {
            instance = this;
        }
    }

    public override void OnServerConnect(NetworkConnection conn)
    {
        base.OnServerConnect(conn);
              
    }
    public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId)
    {

        base.OnServerAddPlayer(conn, playerControllerId);

        var newPlayer = conn.playerControllers[0].gameObject;

		if (newPlayer.GetComponent<NetworkIdentity> ().isLocalPlayer) {
			VRTracker.instance.SetLocalPlayer(newPlayer);
		} 
        
        players.Add(newPlayer);
    }

    public override void OnServerRemovePlayer(NetworkConnection conn, PlayerController player)
    {
        players.Remove(player.gameObject);
        base.OnServerRemovePlayer(conn, player);
    }

    public override void OnServerDisconnect(NetworkConnection conn)
    {
        foreach (var p in conn.playerControllers)
        {
            if (p != null && p.gameObject != null)
            {
                players.Remove(p.gameObject);
            }
        }
        base.OnServerDisconnect(conn);
    }

    public override void OnClientConnect(NetworkConnection conn)
    {
        base.OnClientConnect(conn);
        
    }

    public override void OnStartClient(NetworkClient client)
    {
        base.OnStartClient(client);
    }

}
