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
    /// <summary>
    /// Number of samples captured from Kinect audio stream each millisecond.
    /// </summary>
    private const int SamplesPerMillisecond = 16;

    /// <summary>
    /// Number of bytes in each Kinect audio stream sample (32-bit IEEE float).
    /// </summary>
    private const int BytesPerSample = sizeof (float);

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
    [SyncVar]
    public float beamAngle = 0;

    /// <summary>
    /// Last observed audio beam angle confidence, in the range [0, 1]
    /// </summary>
    [SyncVar]
    public float beamAngleConfidence = 0;

    public Complex[] mySpectrum;

    //public List<float> audioSignalSample = new List<float>();
    private float[] audioRecording = new float[2056];


    // Use this for initialization
    void Start()
    {
        // Get its audio source
        reader = null;
        // Open the sensor

        kinectSensor = KinectSensor.GetDefault();

        if (kinectSensor != null)
        {
            if (!kinectSensor.IsOpen)
            {
                kinectSensor.Open();
            }

            //initialize audio reader
            audioSource = kinectSensor.AudioSource;
            this.audioBuffer = new byte[audioSource.SubFrameLengthInBytes];
            reader = kinectSensor.AudioSource.OpenReader();

        }

        // Get its audio source
        //audioSource = KinectManager.Instance.GetSensorData().AudioSource;

        // Allocate 1024 bytes to hold a single audio sub frame. Duration sub frame 
        // is 16 msec, the sample rate is 16khz, which means 256 samples per sub frame. 
        // With 4 bytes per sample, that gives us 1024 bytes.
       // audioBuffer = new byte[audioSource.SubFrameLengthInBytes];

        // Open the reader for the audio frames
        Debug.Log("Setup stuff");
        //reader = audioSource.OpenReader();
    }

    public void Update()
    {
       GatherSoundData();
    }

    public float[] newSignal;

    void GatherSoundData()
    {
        if (reader != null)
        {
            var audioFrames = reader.AcquireLatestBeamFrames();
            if (audioFrames != null && audioFrames[0] != null)
            {
                //Get relevant data from the subframes
                AudioSubFrameData audioSubFrameData = GetSubFrameData(audioFrames);
                //Dispose of audioFrame
                audioFrames[0].Dispose();
                //Set to null for safety
                audioFrames[0] = null;
                //Zero pad saved signal
                audioSubFrameData.ZeroPadSignal();
                newSignal = audioSubFrameData.signal.ToArray();

                //Turn the float array into a Complex array to do FFT
                //Complex[] complexSignal = new Complex[newSignal.Length];

                //for (int i = 0; i < complexSignal.Length; i++)
                //{
                //    //First parameter is the real value second is the imaginary
                //    complexSignal[i] = new Complex(newSignal[i], 0);
                //}
                ////Apply Fast fourier transform on the signal
                //FourierTransform.FFT(complexSignal,
                //    FourierTransform.Direction.Forward);
                //Then send the signal, beam angle and confidence
                //Cmd_ProvideServerWithSignalSpectrum(complexSignal);
                //Cmd_ProvideBeamAngleAndConfidenceToServer(audioSubFrameData.beamAngle, audioSubFrameData.confidence);
                Cmd_ProvideServerWithSignalData(newSignal, 
                    audioSubFrameData.beamAngle, audioSubFrameData.confidence);
            }
        }
    }

    public struct AudioSubFrameData
    {
        public List<float> signal;
        public float beamAngle;
        public float confidence;

        public void ApplyWindowing()
        {
            int N = signal.Count;
            for (int n = 0; n < N; n++)
            {
                float blackmanHarrisWindow = 0.35875f - (0.48829f*Mathf.Cos(1.0f*n/N)) + (0.14128f*Mathf.Cos(2.0f*n/N)) -
                                              (0.01168f*Mathf.Cos(3.0f*n/N));
                signal[n] *= blackmanHarrisWindow;
            }
        }

        public bool IsSignalPowerOfTwo()
        {
            return Mathf.IsPowerOfTwo(signal.Count);
        }

        public void AddOnToSignal(List<float> newSignal)
        {
            foreach (float t in newSignal)
            {
                signal.Add(t);
            }
        }
        public List<float> GetZeroPaddedSignal()
        {
            if (IsSignalPowerOfTwo())
            {
                return signal;
            }
            while (!IsSignalPowerOfTwo())
            {
                signal.Add(0);
            }
            return signal;
        }

        private int[] audioBuffer;

        public void ZeroPadSignal()
        {
            audioBuffer = new int[1024];
            int newSignalSize = audioBuffer.Length - signal.Count;
            for (int i = 0; i < newSignalSize; i++)
            {
                signal.Add(0);
            }
        }
    }


    public AudioSubFrameData GetSubFrameData(IList<AudioBeamFrame> audioFrames)
    {
        AudioSubFrameData data = new AudioSubFrameData();

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
                audioSignalSample.Add(audioSample); // Turn into an array maybe
            }
        }
        data.signal = audioSignalSample;
        data.beamAngle = subFrameList[0].BeamAngle;
        data.confidence = subFrameList[0].BeamAngleConfidence;
        return data;
    }

    //Should return a 256 length float array with audio samples
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
    void Cmd_ProvideServerWithSignalData(float[] newSignal, float beamAngle, float confidence)
    {
        this.newSignal = newSignal;
        this.beamAngle = beamAngle;
        beamAngleConfidence = confidence;
    }

    [Command]
    void Cmd_ProvideServerWithSignalSpectrum(Complex[] spectrum)
    {
        mySpectrum = spectrum;
    }
   
    /// <summary>
    /// CmdProvideBeamAngleToServer recieves one float from a client which it will then update on the server,
    /// which in turn will update it on all other clients
    /// </summary>
    /// <param name="_beamAngle"></param>
    [Command]
    public void Cmd_ProvideBeamAngleAndConfidenceToServer(float beamAngle, float confidence)
    {
        this.beamAngle = beamAngle;
        beamAngleConfidence = confidence;
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

    private int maxSignalSize = 2048;
    private Windows.Kinect.AudioSource audioSource;

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


