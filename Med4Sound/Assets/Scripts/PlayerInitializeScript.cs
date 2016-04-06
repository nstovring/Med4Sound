using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class PlayerInitializeScript : NetworkBehaviour {
    private string objectName;
    private Color userColor;

    // Use this for initialization
    void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
    public void Initialize(string id, Color userColor)
    {
        //If this is not an object´created by the current client
        if (!isLocalPlayer)
        {
            //Assign all the SyncVar variables from the network to this gameObject
            this.objectName = "SubUser " + id;
            this.userColor = userColor;
            transform.GetComponent<MeshRenderer>().material.color = userColor;
            transform.name = objectName;
        }
    }
    [Command]
    //Cmd_ChangeIdentity changes the colour of this object on the server which in turn will update it on all other clients
    public void Cmd_ChangeIdentity(Color col, string objectName)
    {
        this.objectName = objectName;
        userColor = col;
        //Spawn this object on the network for good measure, to ensure again that commands are able to be called from other classes
        NetworkServer.Spawn(gameObject);
    }
}
