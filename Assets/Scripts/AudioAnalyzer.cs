using System;
using UnityEngine;
using System.Collections;
using Windows.Kinect;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Windows.Data;
using AForge.Math;
using UnityEngine.Networking;

public class AudioAnalyzer : NetworkBehaviour
{

    Windows.Kinect.AudioSource aSource1;
    Windows.Kinect.AudioSource aSource2;

    public UnityEngine.AudioSource unityAudioSource;
    public GameObject AudioGameObject;

    /// <summary>
    /// Number of samples captured from Kinect audio stream each millisecond.
    /// </summary>
    private const int SamplesPerMillisecond = 16;

    /// <summary>
    /// Number of bytes in each Kinect audio stream sample (32-bit IEEE float).
    /// </summary>
    private const int BytesPerSample = sizeof (float);

    /// <summary>
    /// Number of audio samples represented by each column of pixels in wave bitmap.
    /// </summary>
    private const int SamplesPerColumn = 40;

    /// <summary>
    /// Will be allocated a buffer to hold a single sub frame of audio data read from audio stream.
    /// </summary>
    private byte[] audioBuffer = null;

    /// <summary>
    /// Active Kinect sensor
    /// </summary>
    private KinectSensor kinectSensor = null;

    /// <summary>
    /// Reader for audio frames
    /// </summary>
    private AudioBeamFrameReader reader = null;

    /// <summary>
    /// Last observed audio beam angle in radians, in the range [-pi/2, +pi/2]
    /// </summary>
    private float beamAngle = 0;

    /// <summary>
    /// Last observed audio beam angle confidence, in the range [0, 1]
    /// </summary>
    private float beamAngleConfidence = 0;

    /// <summary>
    /// Array of foreground-color pixels corresponding to a line as long as the energy bitmap is tall.
    /// This gets re-used while constructing the energy visualization.
    /// </summary>
    private byte[] foregroundPixels;

    /// <summary>
    /// Number of audio samples accumulated so far to compute the next energy value.
    /// </summary>
    private int accumulatedSampleCount;

    /// <summary>
    /// Index of next element available in audio energy buffer.
    /// </summary>
    private int energyIndex;

    /// <summary>
    /// Number of newly calculated audio stream energy values that have not yet been
    /// displayed.
    /// </summary>
    private int newEnergyAvailable;

    /// <summary>
    /// Error between time slice we wanted to display and time slice that we ended up
    /// displaying, given that we have to display in integer pixels.
    /// </summary>
    private float energyError;

    /// <summary>
    /// Last time energy visualization was rendered to screen.
    /// </summary>
    private DateTime? lastEnergyRefreshTime;

    /// <summary>
    /// Index of first energy element that has never (yet) been displayed to screen.
    /// </summary>
    private int energyRefreshIndex;

    AudioBeam aBeam;

    private DepthFrameReader dFrameReader;
    private Windows.Kinect.AudioSource audioSource;


    public Vector3 SyncSoundSource;

    public bool record = false;
    public float recordingTime = 2;

    //public List<float> audioSignalSample = new List<float>();
    private float[] audioRecording = new float[2056];


    // Use this for initialization
    void Start()
    {
        //Make sure the FFT is sampling at the same rate as kinect recording
        AudioSettings.outputSampleRate = 16000;

        //kinectSensor = KinectSensor.GetDefault();
        // Get its audio source
        reader = null;
        // Open the sensor
        // Get its audio source
        audioSource = KinectManager.Instance.GetSensorData().AudioSource;

        unityAudioSource = GetComponent<UnityEngine.AudioSource>();

        // Allocate 1024 bytes to hold a single audio sub frame. Duration sub frame 
        // is 16 msec, the sample rate is 16khz, which means 256 samples per sub frame. 
        // With 4 bytes per sample, that gives us 1024 bytes.
        audioBuffer = new byte[audioSource.SubFrameLengthInBytes];

        // Open the reader for the audio frames
        Debug.Log("Setup stuff");
        reader = audioSource.OpenReader();
        unityAudioSource = GetComponent<UnityEngine.AudioSource>();
        unityAudioSource.clip = AudioClip.Create("SampleClip", audioRecording.Length, 1, 16000, false);

    }

    public void Update()
    {
        if (reader != null)
        {
            var audioFrames = reader.AcquireLatestBeamFrames();
            if (audioFrames != null)
            {
                if (audioFrames[0] != null)
                {
                    List<float> audioSignalSample = new List<float>();
                    //Get the recorded sound
                    audioSignalSample = GetAudioSignalSample(audioFrames);
                    //Dispose of audioFrame
                    audioFrames[0].Dispose();
                    //Set to null for safety
                    audioFrames[0] = null;
                    //Zero pad saved signal
                    float[] newSignal = ZeroPadSIgnal(audioSignalSample);
                    //Turn the float array into a Complex array to do Fourier Transform mathematics
                    Complex[] complexSignal = new Complex[newSignal.Length];
                    for (int i = 0; i < complexSignal.Length; i++)
                    {
                        //First parameter is the real value second is the imaginary
                        complexSignal[i] = new Complex(newSignal[i], 0);
                    }
                    //Apply Fast fourier transform on the signal
                    FourierTransform.FFT(complexSignal, FourierTransform.Direction.Forward);
                    //Then send the signal
                    Cmd_ProvideServerWithSignalSpectrum(complexSignal);
                }
            }
        }
    }
   
    public Complex[] mySpectrum;

    public List<float> GetAudioSignalSample(IList<AudioBeamFrame> audioFrames)
    {
        List<float> audioSignalSample = new List<float>();
        var subFrameList = audioFrames[0].SubFrames;
        foreach (AudioBeamSubFrame subFrame in subFrameList)
        {
            // Process audio buffer
            subFrame.CopyFrameDataToArray(this.audioBuffer);
            for (int i = 0; i < this.audioBuffer.Length; i += BytesPerSample)
            {
                // Extract the 32-bit IEEE float sample from the byte array
                float audioSample = BitConverter.ToSingle(audioBuffer, i);
                // add audiosample to array for analysis
                if (audioSignalSample.Count > audioBuffer.Length)
                    break;
                audioSignalSample.Add(audioSample);
            }
        }
        return audioSignalSample;
    }


    [Command]
    void Cmd_ProvideServerWithSignalSpectrum(Complex[] spectrum)
    {
        mySpectrum = spectrum;
    }

    public float GetCrossCorrelationCoefficient(Complex[] spectrumA, Complex[] spectrumB)
    {
        return (float) Correlation.CorrelationCoefficient(spectrumA, spectrumB);
    }

    public bool IsSignalCorrelated(Complex[] spectrumA, Complex[] spectrumB, float correlationThreshold)
    {
        if ((float) Correlation.CorrelationCoefficient(spectrumA, spectrumB) > correlationThreshold)
        {
            return true;
        }
        return false;
    }

    public void CrossCorrelate(float[] signalA, float[] signalB)
    {
        Complex[] complexSignalA = new Complex[signalA.Length];
        Complex[] complexSignalB = new Complex[signalB.Length];
        //Convert float values from signal to complex numbers
        for (int i = 0; i < signalA.Length; i++)
        {
            //First parameter is the real value second is the imaginary
            complexSignalA[i] = new Complex(signalA[i], 0);
            complexSignalB[i] = new Complex(signalB[i], 0);
        }

        crossCorrelationCoefficient = Mathf.Lerp((float)crossCorrelationCoefficient,
            (float)Correlation.CorrelationCoefficient(complexSignalA, complexSignalB), 5 * Time.deltaTime);
    }

    [Range(0, 1)]
    public double crossCorrelationCoefficient;

    public int maxSignalSize = 2048;
    public float[] ZeroPadSIgnal(List<float> signalFloats)
    {
        for (int i = signalFloats.Count; i < maxSignalSize; i++)
        {
            signalFloats.Add(0);
        }
        return signalFloats.ToArray();
    }

    public float[] MultiplySIgnals(float[] spectrumFloatsA, float[] spectrumFloatsB)
    {
        int j = spectrumFloatsA.Length;
        float[] newSpectrumFloats = new float[j];

        for (int i = 0; i < j; i++)
        {
            float val = spectrumFloatsA[i]*spectrumFloatsB[i];
            newSpectrumFloats[i] = val;
        }
        return newSpectrumFloats;
    }

    void OnApplicationQuit()
    {
        if (this.reader != null)
        {
            // AudioBeamFrameReader is IDisposable
            audioSource.OpenReader().Dispose();
            this.reader.Dispose();
            this.reader = null;
        }

        if (this.kinectSensor != null)
        {
            this.kinectSensor.Close();
            this.kinectSensor = null;
        }
        Debug.Log("Shutting down readers");
    }

}


