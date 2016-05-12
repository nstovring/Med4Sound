﻿using UnityEngine;
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

    public GameObject[] skeletonCreators;
    public void AudioTracking()
    {
        //GameObject[] skeletonCreators = OffsetCalculator.offsetCalculator.skeletonCreators;
        offsetCalculator = OffsetCalculator.offsetCalculator;
        skeletonCreators = GameObject.FindGameObjectsWithTag("SkeletonCreator");
        if (offsetCalculator != null && skeletonCreators.Length > 1)
        {
            if (skeletonCreators.Any(skeletonCreator => skeletonCreator == null))
            {
                return;
            }

            audioAnalyzers[0] = skeletonCreators[0].GetComponent<AudioAnalyzer>();
            audioAnalyzers[1] = skeletonCreators[1].GetComponent<AudioAnalyzer>();

            Debug.Log(GetCrossCorrelationCoefficient(audioAnalyzers[0].mySpectrum, audioAnalyzers[1].mySpectrum));

            float[] newSignalA = audioAnalyzers[0].newSignal;
            float[] newSignalB = audioAnalyzers[1].newSignal;

            Complex[] complexSignalA = new Complex[newSignalA.Length];
            Complex[] complexSignalB = new Complex[newSignalB.Length];

            for (int i = 0; i < complexSignalA.Length; i++)
            {
                //First parameter is the real value second is the imaginary
                complexSignalA[i] = new Complex(newSignalA[i], 0);
                complexSignalB[i] = new Complex(newSignalB[i], 0);

            }
            //Apply Fast fourier transform on the signal
            FourierTransform.FFT(complexSignalA,
                FourierTransform.Direction.Forward);
            FourierTransform.FFT(complexSignalB,
               FourierTransform.Direction.Forward);


            if (IsSignalCorrelated(complexSignalA, complexSignalB, correlationThreshold))
            {
                angle1 = Mathf.Rad2Deg * skeletonCreators[0].GetComponent<UserSyncPosition>().beamAngle;
                angle2 = Mathf.Rad2Deg * skeletonCreators[1].GetComponent<UserSyncPosition>().beamAngle;

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
