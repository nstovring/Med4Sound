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
    }

    void Update()
    {
        playerMovement();
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
