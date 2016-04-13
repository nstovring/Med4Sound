using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;
using UnityEngine.UI;

public class skeletonCreator : NetworkBehaviour
{

    OffsetCalculator offsetCalculator;
    public GameObject[] players;
    public GameObject prefab;
    readonly Vector3 initialPosVector3 = new Vector3(50, 50, 50);
    public List<int> trackedJoints;
    public int[] tempJoints;
    string test;
    long playerID;
    int jointAmount;
    KinectManager manager;
    public Button button;
    public Button button2;
    float time;
    float sendRate;
    Vector3[] positions;
    //SyncList<float> SyncList_positionsX;
    //SyncList<float> SyncList_positionsY;
    //SyncList<float> SyncList_positionsZ;
    [SyncVar]
    Vector3 rotation;
    // Use this for initialization
    void Start()
    {
        jointAmount = 20;
        offsetCalculator = OffsetCalculator.offsetCalculator;
        positions = new Vector3[jointAmount];
        players = new GameObject[jointAmount];
        sendRate = 0.1f;
        time = 0;
        //spawnObjects();

    }
    /*void getJointPositionsAndRotations()
    {
        SyncList_positionsX = new SyncList<float>();
        SyncList_positionsY = new SyncList<float>();
        SyncList_positionsZ = new SyncListFloat();
        Quaternion userOrientation = manager.GetJointOrientation(manager.GetUserIdByIndex(0), 0, false);
        rotation = userOrientation.eulerAngles;
        for (int i = 0; i < jointAmount; i++)
        {
            SyncList_positionsX.Add(manager.GetJointPosition(manager.GetUserIdByIndex(0), i).x);
            SyncList_positionsY.Add(manager.GetJointPosition(manager.GetUserIdByIndex(0), i).y);
            SyncList_positionsZ.Add(manager.GetJointPosition(manager.GetUserIdByIndex(0), i).z);
        }
    }*/
    void getJointPositionsAndRotations()
    {
        if (manager != null)
        {
            if (manager.IsUserDetected())
            {
                positions = new Vector3[jointAmount];
                Quaternion userOrientation = manager.GetJointOrientation(manager.GetUserIdByIndex(0), 0, false);
                rotation = userOrientation.eulerAngles;
                for (int i = 0; i < jointAmount; i++)
                {
                    positions[i] = manager.GetUserBodyData(manager.GetUserIdByIndex(0)).joint[i].position;
                }
            }
        }
        else
        {
            manager = KinectManager.Instance;
        }
    }
    /*[Command]
    void Cmd_sendJointPositions(SyncList<float> positionsX, SyncList<float> positionsY, SyncList<float> positionsZ, Vector3 rotation)
    {
        this.SyncList_positionsX = positionsX;
        this.rotation = rotation;
    }*/
    [Command]
    void Cmd_sendJointPositions(Vector3[] positions, Vector3 rotation)
    {
        Debug.Log("sending joints");
        this.positions = positions;
        this.rotation = rotation;
    }
    [ClientRpc]
    void Rpc_sendJointPositions(Vector3[] positions, Vector3 rotation)
    {
        Debug.Log("recieving joints");
        this.positions = positions;
        this.rotation = rotation;
    }
    /*void convertToVector3()
    {
        for(int i = 0; i < jointAmount; i++)
        {
            positions[i] = new Vector3(SyncList_positionsX[i], SyncList_positionsY[i], SyncList_positionsZ[i]);
        }
    }*/
    void applyPosition()
    {
        if (manager != null)
        {
            if (players.Length > 0 && manager.IsUserDetected())
            {
                for (int i = 0; i < players.Length; i++)
                {
                    
                    players[i].transform.position = positions[i];
                    OrientWithUser(players[i]);
                }
            }
        }
        else
        {
            manager = KinectManager.Instance;
        }
    }
    public override void OnStartClient()
    {
        base.OnStartClient();
        button = GameObject.FindGameObjectWithTag("spawn button").GetComponent<Button>();
        button2 = GameObject.FindGameObjectWithTag("send").GetComponent<Button>();
        button.onClick.AddListener(spawnObjects);
        button2.onClick.AddListener(sendJoints);


    }
    public void sendJoints()
    {
        if (hasAuthority)
        {
            tempJoints = toArray(trackedJoints);
            Cmd_sendTrackedJoints(tempJoints);
        }
    }
    public void spawnObjects()
    {
        if (hasAuthority)
        {
            Cmd_SpawnObjects();
        }
    }
    
    void FixedUpdate()
    {
        if (hasAuthority && manager != null && isClient)
        {
            playerID = manager.GetUserIdByIndex(0);
            trackedJoints = new List<int>();
            getTrackedJoints();
            //if(time >= sendRate)
            if (true)
            {
                sendJoints();
                time = 0;
            }
            time += Time.deltaTime;
            if (manager.IsUserDetected())
            {
                getJointPositionsAndRotations();
                if (isClient)
                {
                    //Cmd_sendJointPositions(positionsX, positionsY, positionsZ, rotation);
                    Cmd_sendJointPositions(positions, rotation);
                }
            }
        }
        if (manager != null)
        {
            //convertToVector3();
            //applyPosition();
            if (isServer)
            {
                Rpc_sendJointPositions(positions, rotation);
            }
        }
        applyPosition();
    }
    void getTrackedJoints()
    {
        for (int i = 0; i < 20; i++)
        {
            if (manager == null)
            {
                manager = KinectManager.Instance;
            }
            else if (manager.IsJointTracked(playerID, i))
            {
                trackedJoints.Add(i);
            }
        }

    }
    [Command]
    void Cmd_sendTrackedJoints(int[] joints)
    {
        trackedJoints = toList(joints);
    }
    [Command]
    // Cmd_SpawnObjects Instantiates the gamesobject which represent the tracked users
    void Cmd_SpawnObjects()
    {
        for (int i = 0; i < players.Length; i++)
        {
            //To instantiate on a network the gameobject prefab must be registered as a spawnable prefab
            ClientScene.RegisterPrefab(prefab);
            //The  prefab is instantiated and asssigned to the users array
            players[i] = Instantiate(prefab, initialPosVector3, Quaternion.identity) as GameObject;
            // Get the class UserSyncPosition is aquired from the prefab
            PlayerInitializeScript playerInitialize = players[i].transform.GetComponent<PlayerInitializeScript>();
            Color rndColor = RandomColor();
            //Call the initialize method on the userSyncPosition class on the current user
            playerInitialize.Initialize((GetComponent<NetworkIdentity>().netId.Value - 1) + " " + i, rndColor);
            //Spawn the prefab on the server after initialization, enabliing us to call network methods from classes on it
            NetworkServer.SpawnWithClientAuthority(players[i], connectionToClient);
            //Call the Cmd_changeIdentity method, which recieves The networkidentity netids' value as well as a number from the loop
            playerInitialize.Cmd_ChangeIdentity(rndColor, ("SubUser " + (GetComponent<NetworkIdentity>().netId.Value - 1) + " " + i));
        }
        //This method recieves the array of users previously filled with prefabs and is called on the clients
        Rpc_SpawnObjects(players);
    }

    //The ClientRpc Attribute means that this method is only called from the server, yet runs on all clients
    [ClientRpc]
    void Rpc_SpawnObjects(GameObject[] userGameObjects)
    {
        //If the client is the localPlayer that is to say refering to its' own instance only
        if (isLocalPlayer)
        {
            //The array recieved is assigned to this class
            players = userGameObjects;
            //Every gameobject in the array is set to the the child of the gameobject this script is attatched to
            foreach (var i in userGameObjects)
            {
                i.transform.parent = transform;
            }
        }
        else
        {
            //The other clients connected to the server also assign their user array to their own gameobject
            players = userGameObjects;
            foreach (var i in userGameObjects)
            {
                i.transform.parent = transform;
            }
        }
    }
    Color RandomColor()
    {
        return new Color(Random.value, Random.value, Random.value);
    }
    int[] toArray(List<int> list)
    {
        int[] temp = new int[list.Count];
        for (int i = 0; i < list.Count; i++)
        {
            temp[i] = list[i];
        }
        return temp;
    }
    Vector3[] toArray(List<Vector3> list)
    {
        Vector3[] temp = new Vector3[list.Count];
        for (int i = 0; i < list.Count; i++)
        {
            temp[i] = list[i];
        }
        return temp;
    }
    List<int> toList(int[] list)
    {
        List<int> temp = new List<int>();
        for (int i = 0; i < list.Length; i++)
        {
            temp.Add(list[i]);
        }
        return temp;
    }
    [Client]
    //This method is responsible for orienting the cube so it rotation and tilt coressponds to the tracked person orientation
    private void OrientWithUser(GameObject target)
    {
        //If a skeleteon is tracked
        if (manager != null)
        {
            if (manager.IsUserDetected())
            {
                Quaternion userOrientation = Quaternion.Euler(rotation);
                //Quaternion userOrientation = manager.GetUserOrientation(0, false);

                if (offsetCalculator.rotationalOffset.magnitude > Vector3.zero.magnitude)
                {
                    //Offset the rotation by the offsetcalculators offsetVector and apply to this gameObject
                    userOrientation.eulerAngles -= new Vector3(offsetCalculator.rotationalOffset.x, offsetCalculator.rotationalOffset.y, 0);
                    target.transform.rotation = userOrientation;
                }
                else
                {
                    //Simple Apply this rotation to this gameObject
                    target.transform.rotation = userOrientation;
                }
            }
        }
    }
}