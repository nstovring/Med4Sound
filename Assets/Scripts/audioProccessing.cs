using UnityEngine;
using System.Collections;
using UnityEngine.Audio;
using System.Linq;
using System.Collections.Generic;

public class audioProccessing : MonoBehaviour
{

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
        player = Instantiate(playerPrefab, playerPrefab.transform.position, Quaternion.identity) as GameObject;
        recording = AudioClip.Create("theRecording", 100000000, 1, 16000, false);

        monkey = new float[1024];
        recordedSignal = new List<float>();
        isRecording = true;
    }

    void Update()
    {
        playerMovement();
        floatArray = GameObject.FindGameObjectWithTag("SkeletonCreator");
        test = GameObject.Find("testSource").GetComponent<AudioSource>().clip;

        //test.GetData(monkey, 0);
        //recording.SetData(monkey, 0);
        //print(monkey);



        audioStuff();

        if (Input.GetKey(KeyCode.P))
        {

            isRecording = false;

            //monkey = floatArray.GetComponent<AudioAnalyzer>().newSignal;
            //recording.SetData(floatArray.GetComponent<AudioAnalyzer>().newSignal, 0);

            recording.SetData(recordedSignal.ToArray(), 0);



            soundplayer.GetComponent<AudioSource>().clip = recording;

            soundplayer.GetComponent<AudioSource>().Play();
        }

        if (isRecording == false) {
            print("Play");
        }


        if (Input.GetKey(KeyCode.R))
        {
            isRecording = true;
        }


        if (isRecording == true)
        {
           recordedSignal = floatArray.GetComponent<AudioAnalyzer>().nikolaj;
           print("Recording");
        }


    }

    float currentime;





    void audioStuff()
    {

        //print(floatArray.GetComponent<AudioAnalyzer>().newSignal);




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
