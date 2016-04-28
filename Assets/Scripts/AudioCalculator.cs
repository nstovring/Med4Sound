using UnityEngine;
using System.Collections;
using System.Linq;
using Windows.Kinect;
using AForge.Math;
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

    [SyncVar]
    public float currentCorrelation;

    private float angle1;
    private float angle2;
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

    private AudioAnalyzer[] audioAnalyzers = new AudioAnalyzer[2];

    // Update is called once per frame
    void Update ()
	{
        if (Input.GetKeyDown(KeyCode.A))
        {
            kinectSensor = KinectSensor.GetDefault();
            //kinectSensor.AudioSource.PropertyChanged += UpdateAudioTrackingPosition;
        }
        AudioTracking();

        if (Input.GetKeyDown(KeyCode.L))
        {
            Logger.instance.LogData(trackingType, TrackedVector3, 1.ToString() , 0);
        }
    }

    public void AudioTracking()
    {
        GameObject[] skeletonCreators = OffsetCalculator.offsetCalculator.skeletonCreators;
        if (offsetCalculator != null && skeletonCreators.Length > 0)
        {
            if (skeletonCreators.Any(skeletonCreator => skeletonCreator == null))
            {
                return;
            }
            if (audioAnalyzers.Any(audioAnalyzer => audioAnalyzer == null))
            {
                audioAnalyzers[0] = skeletonCreators[0].GetComponent<AudioAnalyzer>();
                audioAnalyzers[1] = skeletonCreators[1].GetComponent<AudioAnalyzer>();
            }

            if (IsSignalCorrelated(audioAnalyzers[0].mySpectrum, audioAnalyzers[1].mySpectrum, correlationThreshold))
            {
                angle1 = Mathf.Rad2Deg * offsetCalculator.skeletonCreators[0].GetComponent<UserSyncPosition>().beamAngle;
                angle2 = Mathf.Rad2Deg * offsetCalculator.skeletonCreators[1].GetComponent<UserSyncPosition>().beamAngle;

                Vector3 interSectionPoint = offsetCalculator.vectorIntersectionPoint(angle1, angle2);
                TrackedVector3 = interSectionPoint * -1;
            }
        }
    }

    [Range(0, 1f)]
    public float correlationThreshold = 0.75f;

    public float GetCrossCorrelationCoefficient(Complex[] spectrumA, Complex[] spectrumB)
    {
        return (float)Correlation.CorrelationCoefficient(spectrumA, spectrumB);
    }

    public bool IsSignalCorrelated(Complex[] spectrumA, Complex[] spectrumB, float correlationThreshold)
    {
        if ((float)Correlation.CorrelationCoefficient(spectrumA, spectrumB) > correlationThreshold)
        {
            return true;
        }
        return false;
    }
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(TrackedVector3,0.5f);
    }

}
