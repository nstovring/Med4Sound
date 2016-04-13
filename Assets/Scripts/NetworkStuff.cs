using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class NetworkStuff : MonoBehaviour{

	// Use this for initialization
	void Start () {
        NetworkManager manager = GetComponent<NetworkManager>();
        manager.connectionConfig.IsAcksLong = true;
        manager.connectionConfig.MaxSentMessageQueueSize = 512;
    }
	
	// Update is called once per frame
	void Update () {
	
	}
}
