using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


public class FFTClass : MonoBehaviour
{
    public AudioClip aClip;
    public AudioClip bClip;

    public AudioSource aSource;
    public AudioSource bSource;

    // Use this for initialization
    void Start () {
	    
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    private List<float> audioSignalSample;

    void FFTtesting(List<float> _audioSignalSample)
    {
        float[] newSignalA = new float[aSource.clip.samples * aSource.clip.channels];
        float[] newSignalB = new float[bSource.clip.samples * bSource.clip.channels];

        aClip.GetData(newSignalA, 0);
        bClip.GetData(newSignalB, 0);

        newSignalA = ZeroPadSIgnal(newSignalA);
        newSignalB = ZeroPadSIgnal(newSignalB);

        //audioSignalSample = newSignal.ToList();

        //aSource.clip = AudioClip.Create("SampleClip", newSignal.Length, 1, 16000, false);
        //bSource.clip = AudioClip.Create("SampleClip", newSignal.Length, 1, 16000, false);

        aSource.clip.SetData(newSignalA, 0);
        bSource.clip.SetData(newSignalB, 0);

        aSource.Play();
        bSource.Play();

        spectrumA = new float[256];
        aSource.GetSpectrumData(spectrumA, 0, FFTWindow.BlackmanHarris);

    }

    void FFT(List<float> _audioSignalSample)
    {
        float[] newSignal = ZeroPadSIgnal(_audioSignalSample.ToArray());
        audioSignalSample = newSignal.ToList();

        aSource.clip = AudioClip.Create("SampleClip", newSignal.Length, 1, 16000, false);
        bSource.clip = AudioClip.Create("SampleClip", newSignal.Length, 1, 16000, false);

        aSource.clip.SetData(newSignal, 0);
        bSource.clip.SetData(newSignal, 0);

        aSource.Play();
        bSource.Play();

        spectrumA = new float[256];
        aSource.GetSpectrumData(spectrumA, 0, FFTWindow.BlackmanHarris);

    }

    private float[] spectrumA = new float[256];

    public float[] ZeroPadSIgnal(float[] signalFloats)
    {
        int j = signalFloats.Length;
        List<float> tempSignalFloats = signalFloats.ToList();

        for (int i = 0; i < j; i++)
        {
            tempSignalFloats.Add(0);
        }
        return tempSignalFloats.ToArray();
    }

    public float[] MultiplySIgnals(float[] spectrumFloatsA, float[] spectrumFloatsB)
    {
        int j = spectrumFloatsA.Length;
        float[] newSpectrumFloats = new float[j];

        for (int i = 0; i < j; i++)
        {
            float val = spectrumFloatsA[i] * spectrumFloatsB[i];
            newSpectrumFloats[i] = val;
        }
        return newSpectrumFloats;
    }
}
