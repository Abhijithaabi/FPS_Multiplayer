using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] GameObject playerPrefab;
    [SerializeField] float respawnTime=5f;
    GameObject player;
    public GameObject deathEffect;
    public static PlayerSpawner instance;
    void Awake()
    {
        instance=this;   
    }
    void Start()
    {
        if(PhotonNetwork.IsConnected)
        {
            
            SpawnPlayer();
        }
    }
    public void SpawnPlayer()
    {
        Transform spawnPoint = SpawnManager.instance.GetSpawnPoint();
        UIController.Instance.weaponOverheatTxt.gameObject.SetActive(false);
        player =  PhotonNetwork.Instantiate(playerPrefab.name,spawnPoint.position,spawnPoint.rotation);
    }
    public void Die(string damager)
    {
        UIController.Instance.DeathTxt.text="You were Killed By "+damager;
        MatchManager.instance.UpdateStatsSend(PhotonNetwork.LocalPlayer.ActorNumber,1,1);
        StartCoroutine(Death());
        
        
    }
     public IEnumerator Death()
     {
        UIController.Instance.DeathScreen.SetActive(true);
        PhotonNetwork.Instantiate(deathEffect.name,player.transform.position,Quaternion.identity);
        PhotonNetwork.Destroy(player);
        player=null;
        yield return new WaitForSeconds(respawnTime);
        UIController.Instance.DeathScreen.SetActive(false);
        if(MatchManager.instance.state==MatchManager.GameState.Playing && player==null)
        {
            SpawnPlayer();
        }
        

     }


  
}
