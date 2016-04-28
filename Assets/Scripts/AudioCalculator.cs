using UnityEngine;
using System.Collections;
using System.Linq;
using Windows.Kinect;
using UnityEngine.Networking;

public class AudioCalculator : NetworkBehaviour
{
    //[SyncVar] private float beamAngle;
    //[SyncVar] private float beamAngle2;
    [SyncVar] public Vector3 TrackedVector3;
    public SyncList<float> BeamAngleSyncList; 
    public GameObject AudioTrackedGameObject;
    private Vector3 kinectOffset;
    private OffsetCalculator offsetCalculator;

    /// <summary>
    /// Active Kinect sensor
    /// </summary>
    private KinectSensor kinectSensor = null;

    public string trackingType = "AudioTracking";
    // Use this for initialization
    void Start () {
        offsetCalculator = OffsetCalculator.offsetCalculator;

        if (Network.isServer)
        {
            kinectSensor = KinectSensor.GetDefault();
            //kinectSensor.AudioSource.PropertyChanged += UpdateAudioTrackingPosition;
        }
    }

    private GameObject[] players;
    // Update is called once per frame
    void Update ()
	{
        if (Input.GetKeyDown(KeyCode.A))
        {
            kinectSensor = KinectSensor.GetDefault();
            //kinectSensor.AudioSource.PropertyChanged += UpdateAudioTrackingPosition;
        }
        offsetCalculator = OffsetCalculator.offsetCalculator;
        if (offsetCalculator != null && offsetCalculator.skeletonCreators.Length > 0)
        {
            //Debug.Log("Eat shit & die");
            if (offsetCalculator.skeletonCreators.Any(skeletonCreator => skeletonCreator == null))
            {
                return;
            }
            angle1 = Mathf.Rad2Deg * offsetCalculator.skeletonCreators[0].GetComponent<UserSyncPosition>().beamAngle;
            angle2 = Mathf.Rad2Deg * offsetCalculator.skeletonCreators[1].GetComponent<UserSyncPosition>().beamAngle;

            Vector3 interSectionPoint = offsetCalculator.vectorIntersectionPoint(angle1, angle2);
            TrackedVector3 = interSectionPoint * -1;
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            Logger.instance.LogData(trackingType, TrackedVector3, 1.ToString() , 0);
        }
    }

    private float angle1;
    private float angle2;

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(TrackedVector3,0.5f);
    }

}
