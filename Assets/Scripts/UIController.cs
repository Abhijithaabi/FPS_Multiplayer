using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;

public class UIController : MonoBehaviour
{
    public TMP_Text weaponOverheatTxt;
    public Slider weaponTempSlider;
    public GameObject DeathScreen;
    public TMP_Text DeathTxt;
    public static UIController Instance;
    public Slider healthSlider;
    public TMP_Text killsText;
    public TMP_Text deathText;
    public GameObject leaderboard;
    public LeaderBoardPlayer lboardPlayerDisplay;
    public GameObject endScreen;
    public TMP_Text TimerTxt;
    public GameObject OptionsScreen;
    
    private void Awake() {
        Instance=this;
    }
    void Start()
    {
        
    }

    
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            showHideOptions();
        }
        if(OptionsScreen.activeInHierarchy && Cursor.lockState != CursorLockMode.None)
        {
            Cursor.lockState=CursorLockMode.None;
            Cursor.visible=true;
        }
    }
    public void showHideOptions()
    {
        if(!OptionsScreen.activeInHierarchy)
        {
            OptionsScreen.SetActive(true);
        }
        else
        {
            OptionsScreen.SetActive(false);
        }
    }
    public void ReturnToMainMenu()
    {
        PhotonNetwork.AutomaticallySyncScene=false;
        PhotonNetwork.LeaveRoom();
    }
    public void QuitGame()
    {
        Application.Quit();
    }
}
