using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Windows.Kinect;
using AForge.Math;
using UnityEngine.Networking;

public class AudioCalculator : NetworkBehaviour
{

    /// <summary>
    /// Number of bytes in each Kinect audio stream sample (32-bit IEEE float).
    /// </summary>
    private const int BytesPerSample = sizeof(float);


    [SyncVar] public Vector3 TrackedVector3;
    public SyncList<float> BeamAngleSyncList; 
    public GameObject AudioTrackedGameObject;

    private OffsetCalculator offsetCalculator;

    public UnityEngine.AudioSource MyAudioSource;

    [SyncVar]
    public float currentCorrelation;

    private float beamAngle1;
    private float beamAngle2;
    /// <summary>
    /// Active Kinect sensor
    /// </summary>
    private KinectSensor kinectSensor = null;

    public static AudioCalculator Instance;
    public string trackingType = "AudioTracking";
    // Use this for initialization
    void Start ()
    {
        Instance = this;
        offsetCalculator = OffsetCalculator.offsetCalculator;
        MyAudioSource = GetComponent<UnityEngine.AudioSource>();
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
            Logger.instance.LogData(trackingType, TrackedVector3, beamAngle1 + " , " + beamAngle2 , 0);
        }
    }

    public GameObject[] skeletonCreators;
    [Server]
    public void AudioTracking()
    {
        offsetCalculator = OffsetCalculator.offsetCalculator;
        skeletonCreators = GameObject.FindGameObjectsWithTag("SkeletonCreator");
        if (offsetCalculator == null || skeletonCreators.Length <= 1)
        {
            return;
        }

        if (audioAnalyzers.All(analyzer => analyzer == null))
        {
            audioAnalyzers[0] = skeletonCreators[0].GetComponent<AudioAnalyzer>();
            audioAnalyzers[1] = skeletonCreators[1].GetComponent<AudioAnalyzer>();
        }
        if (audioAnalyzers.All(analyzer => analyzer.beamAngleConfidence < 1))
        {
            return;
        }
        int signalLength = 0;

        foreach (AudioAnalyzer analyzer in audioAnalyzers)
        {
            List<float> newSignal = new List<float>();
            for (int i = 0; i < analyzer.audioBuffer.Length; i += BytesPerSample)
            {
                // Extract the 32-bit IEEE float sample from the byte array
                float audioSample = BitConverter.ToSingle(analyzer.audioBuffer, i);
                // add audiosample to array for analysis
                if (newSignal.Count > analyzer.audioBuffer.Length)
                    break;
                newSignal.Add(audioSample);
                signalLength ++;
            }
            analyzer.newSignal = newSignal.ToArray();
        }

        Complex[] complexSignalA = new Complex[signalLength/2];
        Complex[] complexSignalB = new Complex[signalLength/2];

        for (int i = 0; i < complexSignalA.Length; i++)
        {
            //First parameter is the real value second is the imaginary
            complexSignalA[i] = new Complex(audioAnalyzers[0].newSignal[i], 0);
            complexSignalB[i] = new Complex(audioAnalyzers[1].newSignal[i], 0);
        }
        //Apply Fast fourier transform on the signal
        FourierTransform.FFT(complexSignalA,
            FourierTransform.Direction.Forward);
        FourierTransform.FFT(complexSignalB,
            FourierTransform.Direction.Forward);

        if (IsSignalCorrelated(complexSignalA, complexSignalB, correlationThreshold))
        {
            beamAngle1 = Mathf.Rad2Deg * audioAnalyzers[0].beamAngle;
            beamAngle2 = Mathf.Rad2Deg * audioAnalyzers[1].beamAngle;
            Vector3 interSectionPoint = offsetCalculator.vectorIntersectionPoint(beamAngle1, beamAngle2);
            TrackedVector3 = interSectionPoint * -1;
        }
    }

    [Range(0, 1f)]
    public float correlationThreshold = 0.75f;

    public bool IsSignalCorrelated(Complex[] spectrumA, Complex[] spectrumB, float correlationThreshold)
    {
        currentCorrelation = (float)Correlation.CorrelationCoefficient(spectrumA, spectrumB);
        if (currentCorrelation > correlationThreshold)
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
