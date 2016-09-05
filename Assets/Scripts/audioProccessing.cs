using UnityEngine;
using System.Collections;
using UnityEngine.Audio;
using System.Linq;
using System.Collections.Generic;
using Windows.Kinect;

public class audioProccessing : MonoBehaviour
{
    public AudioAnalyzer Analyzer;
    public AudioMixer mixer;
    public GameObject playerPrefab;
    public GameObject soundplayer;

    public float playerSpeed;
    private GameObject player;
    private Vector2 movement;
    private GameObject floatArray;
    private AudioClip recording;

    private AudioClip test;
    private float[] monkey;
    private List<float> recordedSignal;
    bool isRecording;


    void Start()
    {

        _recordedFloats = new List<float>();
        if (!Analyzer)
        {
            Debug.Log("No analyzer doofus");
        }
        else
        {
        }
        //player = Instantiate(playerPrefab, playerPrefab.transform.position, Quaternion.identity) as GameObject;
    }

    private bool oneShotSoundPlayed = false;
    void Update()
    {
       
        soundModulator();
    }

    public int position = 0;

    
    void OnAudioRead(float[] data)
    {
        _recordedFloats = Analyzer.RecordedFloats;
        int count = 0;
        while (count < data.Length)
        {
            data[count] = _recordedFloats[count];
            position++;
            count++;
        }
    }
    void OnAudioSetPosition(int newPosition)
    {
        position = newPosition;
    }

    private List<float> _recordedFloats;
    private float wait;
    private bool check;
    //private AudioSource myAudioSource;
    private bool playingSound= false;
    public void PlaySound(List<float> recordedFloats)
    {
        UnityEngine.AudioSource myAudioSource = GetComponent<UnityEngine.AudioSource>();
        if (check)
        {
            wait -= Time.deltaTime;
        }

        if ((wait < 0f) && check)
        {
            _recordedFloats = new List<float>();
            check = false;
        }


        if (check)
            return;

        if (_recordedFloats.Count > 202400)
            return;

        if (_recordedFloats.Count < 102400)
        {
            for (int i = 0; i < recordedFloats.Count; i++)
            {
                _recordedFloats.Add(recordedFloats[i]);
            }
            return;
        }
        
        Debug.Log("Playing Sound");
        AudioClip clip = AudioClip.Create("Stream", _recordedFloats.Count,1,16000,false);
        //AudioClip clip = AudioClip.Create("Recorded Sound", _RecordedFloats.Count, 1, 16000, false);
        //UnityEngine.AudioSource myAudioSource = GetComponent<UnityEngine.AudioSource>();
        //clip.SetData(_recordedFloats.ToArray(), 0);
        myAudioSource.clip = clip;
        //myAudioSource.loop = true;
        myAudioSource.clip.SetData(_recordedFloats.ToArray(), 0);
        //myAudioSource.PlayOneShot(clip);
        myAudioSource.Play();
        wait = clip.length;
        Debug.Log("wait: " +wait);
        check = true;
        playingSound = true;
    }

    void soundModulator()
    {

        Vector3 soundLocation = GameObject.Find("AudioCalculator").GetComponent<AudioCalculator>().TrackedVector3;


        mixer.SetFloat("Tempo", Mathf.Abs(soundLocation.x) / 5);
        mixer.SetFloat("Pitch", Mathf.Abs(soundLocation.z) / 5);



        mixer.SetFloat("Tempo", Mathf.Abs(movement.x) / 5);
        mixer.SetFloat("Pitch", Mathf.Abs(movement.y) / 5);


        if (movement.x < 0.2f && movement.x > -0.2f)
        {
            mixer.SetFloat("Pitch", 1);
        }
    }

    void playerMovement()
    {
        movement += new Vector2(Input.GetAxis("Horizontal") / playerSpeed, Input.GetAxis("Vertical") / playerSpeed);
        print(movement);
        player.transform.position = movement;
    }
}
