using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HubSystem : MonoBehaviour
{
    public bool isBlueHub = true; 

    public List<GameObject> funnelPieces = new List<GameObject>();

    [Header("Audio Settings")]
    public AudioSource audioSource;
    [Range(0.8f, 1.2f)] public float minPitch = 0.9f;
    [Range(0.8f, 1.2f)] public float maxPitch = 1.1f;
    [Range(0.0f, 1.0f)] public float volume = 1f;

    [Header("Changing panel cover things")]
    [SerializeField] private Material activeMaterial;
    [SerializeField] private Material inactiveMaterial;
    [SerializeField] private List<MeshRenderer> targetMeshes = new List<MeshRenderer>();

    private bool currentState = false;
    private bool isCounting = false;
    private Coroutine endActive;

    public int fuelCount = 0;

    public void SetActive(bool active)
    {
        currentState = active;
        Material materialToApply = currentState ? activeMaterial : inactiveMaterial;

        foreach (MeshRenderer mesh in targetMeshes)
        {
            if (mesh == null) continue;

            Material[] mats = new Material[mesh.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = materialToApply;
            }
            mesh.materials = mats;
        }

        if (active)
        {
            if (endActive != null)
            {
                StopCoroutine(endActive);
                endActive = null;
            }
            isCounting = true;
        }
        else
        {
            if (endActive == null)
            {
                endActive = StartCoroutine(StopCountingAfterDelay());
            }
        }
    }

    private IEnumerator StopCountingAfterDelay()
    {
        yield return new WaitForSeconds(3);
        isCounting = false;
        endActive = null;
    }

    public void ToggleActive()
    {
        SetActive(!currentState);
    }

    public void playSFX(Collision collision)
    {
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.pitch = Random.Range(minPitch, maxPitch);
            audioSource.PlayOneShot(audioSource.clip);
        }
    }

    public void countFuel(bool wasShotFromLegalZone)
    {
        if (!isCounting) return;

        fuelCount++;
        Debug.Log("Fuel Count: " + fuelCount + " | Legal Shot: " + wasShotFromLegalZone);

        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.ScorePoint(isBlueHub, wasShotFromLegalZone);
        }
    }
}