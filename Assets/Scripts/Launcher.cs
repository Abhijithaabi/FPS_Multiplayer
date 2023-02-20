using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class Launcher : MonoBehaviourPunCallbacks
{
    [SerializeField] GameObject loadingScreen;
    [SerializeField] GameObject menuButtons;
    [SerializeField] TMP_Text loadingTxt;
    [SerializeField] GameObject createRoomScreen;
    [SerializeField] TMP_InputField createRoomTxt;
    [SerializeField] GameObject roomScreen;
    [SerializeField] TMP_Text roomNameTxt,playerLabel;
    [SerializeField] GameObject errorScreen;
    [SerializeField] TMP_Text errorTxt;
    [SerializeField] GameObject roomBrowserScreen;
    [SerializeField] GameObject nickNameScreen;
    [SerializeField]TMP_InputField nameInput;
    [SerializeField] string levelToLoad;
    [SerializeField] GameObject startButton;
    [SerializeField] GameObject quickTextButton;
    public string[] allMaps;
    public bool  changeMapBetweenRounds=true;
    public static bool hassetNickName;
    public RoomButton roomButton;
    List<RoomButton> allRoomButtons = new List<RoomButton>();
    List<TMP_Text> allPlayerNames = new List<TMP_Text>();
    public static Launcher instance;
    
    void Awake()
    {
        instance=this;   
    }
    
    void Start()
    {
        CloseMenus();
        loadingScreen.SetActive(true);
        loadingTxt.text = "Connecting To Network";
        if(!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();  
        }
         
        #if UNITY_EDITOR
        quickTextButton.SetActive(true);
        #endif
        Cursor.lockState=CursorLockMode.None;
        Cursor.visible=true;
    }
    void CloseMenus()
    {
        loadingScreen.SetActive(false);
        menuButtons.SetActive(false);
        createRoomScreen.SetActive(false);
        roomScreen.SetActive(false);
        errorScreen.SetActive(false);
        roomBrowserScreen.SetActive(false);
        nickNameScreen.SetActive(false);
    }
    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
        loadingTxt.text = "Joining Lobby...";
        PhotonNetwork.AutomaticallySyncScene=true;
        
    }
    public override void OnJoinedLobby()
    {
        CloseMenus();
        menuButtons.SetActive(true);
        //PhotonNetwork.NickName = Random.Range(0,1000).ToString();
        if(!hassetNickName)
        {
            CloseMenus();
            nickNameScreen.SetActive(true);
            if(PlayerPrefs.HasKey("playerName"))
            {
                nameInput.text=PlayerPrefs.GetString("playerName");
            }
        }
        else
        {
            PhotonNetwork.NickName =PlayerPrefs.GetString("playerName");
        }
    }
   

    public void OpenCreateRoom()
    {
        createRoomScreen.SetActive(true);
    }
    public void CreateRoom()
    {
        if(!string.IsNullOrEmpty(createRoomTxt.text))
        {
            RoomOptions options = new RoomOptions();
            options.MaxPlayers=8;
            PhotonNetwork.CreateRoom(createRoomTxt.text,options);
            CloseMenus();
            loadingTxt.text="Creating Room...";
            loadingScreen.SetActive(true);
        }
    }
    public override void OnJoinedRoom()
    {
        roomScreen.SetActive(true);
        roomNameTxt.text = PhotonNetwork.CurrentRoom.Name;
        ListAllPlayers();
        if(PhotonNetwork.IsMasterClient)
        {
            startButton.SetActive(true);
        }
        else
        {
            startButton.SetActive(false);
        }
    }
    void ListAllPlayers()
    {
        foreach(TMP_Text player in allPlayerNames)
        {
            Destroy(player.gameObject);
        }
        allPlayerNames.Clear();
        Player[] players = PhotonNetwork.PlayerList;
        for(int i=0;i<players.Length;i++)
        {
            TMP_Text newPlayerLabel = Instantiate(playerLabel,playerLabel.transform.parent);
            newPlayerLabel.text=players[i].NickName;
            newPlayerLabel.gameObject.SetActive(true);
            allPlayerNames.Add(newPlayerLabel);
        }
    }
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
            TMP_Text newPlayerLabel = Instantiate(playerLabel,playerLabel.transform.parent);
            newPlayerLabel.text=newPlayer.NickName;
            newPlayerLabel.gameObject.SetActive(true);
            allPlayerNames.Add(newPlayerLabel);
    }
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        ListAllPlayers();
    }
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        CloseMenus();
        errorTxt.text="Failed to connect to room: "+message;
        errorScreen.SetActive(true);
    }
    public void CloseErrorScreen()
    {
        CloseMenus();
        menuButtons.SetActive(true);
    }
    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
        loadingTxt.text="Leaving Room...";
        CloseMenus();
        loadingScreen.SetActive(true);
    }
    public override void OnLeftRoom()
    {
        CloseMenus();
        menuButtons.SetActive(true);
    }
    public void OpenRoomBrowserScreen()
    {
        CloseMenus();
        roomBrowserScreen.SetActive(true);
    }
    public void CloseRoomBrowserScreen()
    {
        CloseMenus();
        menuButtons.SetActive(true);
    }
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach(RoomButton rb in allRoomButtons)
        {
            Destroy(rb.gameObject);
        }
        allRoomButtons.Clear();
        roomButton.gameObject.SetActive(false);
        for(int i=0;i<roomList.Count;i++)
        {
            if(roomList[i].PlayerCount!=roomList[i].MaxPlayers && !roomList[i].RemovedFromList)
            {
                RoomButton newButton = Instantiate(roomButton,roomButton.transform.parent);
                newButton.SetButtonDetails(roomList[i]);
                newButton.gameObject.SetActive(true);
                allRoomButtons.Add(newButton);
            }
        }
    }
    public void JoinRoom(RoomInfo info)
    {
        PhotonNetwork.JoinRoom(info.Name);
        CloseMenus();
        loadingTxt.text = "Joining Room...";
        loadingScreen.SetActive(true);
    }
    public void SetNickName()
    {
        Debug.Log(nameInput.text);
        if(!string.IsNullOrEmpty(nameInput.text))
        {
            PhotonNetwork.NickName=nameInput.text;
            PlayerPrefs.SetString("playerName",nameInput.text);
            CloseMenus();
            menuButtons.SetActive(true);
            hassetNickName=true;
        }
        
    }
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if(PhotonNetwork.IsMasterClient)
        {
            startButton.SetActive(true);
        }
        else
        {
            startButton.SetActive(false);
        }       
    }
    public void QuickTest()
    {
        RoomOptions options = new RoomOptions();
        options.MaxPlayers=8;
        PhotonNetwork.CreateRoom("Test",options);
        CloseMenus();
        loadingTxt.text="Creating Room...";
        loadingScreen.SetActive(true);
    }
    public void StartGame()
    {
        //PhotonNetwork.LoadLevel(levelToLoad);
        PhotonNetwork.LoadLevel(allMaps[Random.Range(0,allMaps.Length)]);
    }
    public void QuitGame()
    {
        Application.Quit();
    }
}
