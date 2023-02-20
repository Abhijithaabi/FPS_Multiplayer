using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public Transform[] spawnPoints;
    public static SpawnManager instance;
    void Awake()
    {
        instance=this;   
    }
    void Start()
    {
        foreach(Transform spwan in spawnPoints)
        {
            spwan.gameObject.SetActive(false);
        }
    }

    
    void Update()
    {
        
    }
    public Transform GetSpawnPoint()
    {
        return spawnPoints[Random.Range(0,spawnPoints.Length)];
    }
}
