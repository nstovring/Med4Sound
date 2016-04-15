using System;
using UnityEngine;
using System.Collections;
using Windows.Kinect;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Windows.Data;

public class AudioAnalyzer : MonoBehaviour
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
    /// Minimum energy of audio to display (a negative number in dB value, where 0 dB is full scale)
    /// </summary>
    private const int MinEnergy = -90;

    /// <summary>
    /// Width of bitmap that stores audio stream energy data ready for visualization.
    /// </summary>
    private const int EnergyBitmapWidth = 780;

    /// <summary>
    /// Height of bitmap that stores audio stream energy data ready for visualization.
    /// </summary>
    private const int EnergyBitmapHeight = 195;

    /// <summary>
    /// Array of background-color pixels corresponding to an area equal to the size of whole energy bitmap.
    /// </summary>
    private readonly byte[] backgroundPixels = new byte[EnergyBitmapWidth*EnergyBitmapHeight];

    /// <summary>
    /// Will be allocated a buffer to hold a single sub frame of audio data read from audio stream.
    /// </summary>
    private byte[] audioBuffer = null;

    /// <summary>
    /// Buffer used to store audio stream energy data as we read audio.
    /// We store 25% more energy values than we strictly need for visualization to allow for a smoother
    /// stream animation effect, since rendering happens on a different schedule with respect to audio
    /// capture.
    /// </summary>
    private readonly float[] energy = new float[(uint) (EnergyBitmapWidth*1.25)];

    /// <summary>
    /// Object for locking energy buffer to synchronize threads.
    /// </summary>
    private readonly object energyLock = new object();

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
    /// Sum of squares of audio samples being accumulated to compute the next energy value.
    /// </summary>
    private float accumulatedSquareSum;

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

    public List<float> audioSignalSample = new List<float>();
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

    void MatlabMath()
    {
      //  mLabClass.
    }
    
    public void Update()
    {
        if (reader != null)
        {
            var audioFrames = reader.AcquireLatestBeamFrames();
            if (audioFrames != null)
            {
                // it gets here just fine
                if (audioFrames[0] != null)
                {
                    // it never gives me any audio frames!
                    audioSignalSample = new List<float>();
                    var subFrameList = audioFrames[0].SubFrames;
                    foreach (AudioBeamSubFrame subFrame in subFrameList)
                    {
                        // Process audio buffer
                        int j = 0;
                        subFrame.CopyFrameDataToArray(this.audioBuffer);
                        for (int i = 0; i < this.audioBuffer.Length; i += BytesPerSample)
                        {
                            // Extract the 32-bit IEEE float sample from the byte array
                            float audioSample = BitConverter.ToSingle(audioBuffer, i);
                            // add audiosample to array for analysis
                            audioSignalSample.Add(audioSample);
                            this.accumulatedSquareSum += audioSample * audioSample;
                            ++this.accumulatedSampleCount;

                            if (this.accumulatedSampleCount < SamplesPerColumn)
                            {
                                continue;
                            }
                        }
                        Debug.Log("Ey!");
                    }
                    //Dispose of audioFrame
                    audioFrames[0].Dispose();
                    //Set to null for safety
                    audioFrames[0] = null;
                    //ZeroPadSavedSignal
                    float[] newSignal = ZeroPadSIgnal(audioSignalSample);
                    audioSignalSample = newSignal.ToList();
                    unityAudioSource.clip = AudioClip.Create("SampleClip", newSignal.Length, 1, 16000, false);
                    unityAudioSource.clip.SetData(newSignal, 0);
                    unityAudioSource.Play();
                    spectrum = new float[256];
                    unityAudioSource.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

                    ApplyFFT();
                }
            }
        }
    }



    public float[] spectrum = new float[256];

    private void ApplyFFT()
    {
        unityAudioSource.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);
        int i = 1;
        while (i < spectrum.Length - 1)
        {
            Debug.DrawLine(new Vector3(i - 1, spectrum[i] + 10, 0), new Vector3(i, spectrum[i + 1] + 10, 0), Color.red);
            Debug.DrawLine(new Vector3(i - 1, Mathf.Log(spectrum[i - 1]) + 10, 2), new Vector3(i, Mathf.Log(spectrum[i]) + 10, 2), Color.cyan);
            Debug.DrawLine(new Vector3(Mathf.Log(i - 1), spectrum[i - 1] - 10, 1), new Vector3(Mathf.Log(i), spectrum[i] - 10, 1), Color.green);
            Debug.DrawLine(new Vector3(Mathf.Log(i - 1), Mathf.Log(spectrum[i - 1]), 3), new Vector3(Mathf.Log(i), Mathf.Log(spectrum[i]), 3), Color.yellow);
            i++;
        }
    }

    public float[] ZeroPadSIgnal(List<float> signalFloats)
    {
        int j = signalFloats.Count;
        for (int i = 0; i < j; i++)
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


