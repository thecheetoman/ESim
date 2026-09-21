using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HubSystem : MonoBehaviour
{
    public List<GameObject> funnelPieces = new List<GameObject>();
    
    [Header("Audio Settings")]
    public AudioSource audioSource;
    [Range(0.8f, 1.2f)] public float minPitch = 0.9f;
    [Range(0.8f, 1.2f)] public float maxPitch = 1.1f;
    [Range(0.0f, 1.0f)] public float volume = 1f;


    public void playSFX(Collision collision)
    {
        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.PlayOneShot(audioSource.clip);
    }

    // Start is called before the first frame update
    void Start()
    {
        //audioSource.volume = volume;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
