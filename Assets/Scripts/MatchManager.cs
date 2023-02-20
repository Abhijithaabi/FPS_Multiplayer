using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class MatchManager : MonoBehaviourPunCallbacks,IOnEventCallback
{
    public enum EventCodes : byte
    {
        NewPlayer,
        ListPlayers,
        UpdateStats,
        NextMatch,
        TimerSync
    }
    public enum GameState
    {
        Waiting,
        Playing,
        Ending
    }
    public int killsToWin=3;
    public Transform mapCamPoint;
    public GameState state = GameState.Waiting;
    public float waitAfterRnding = 5f;
    public bool perpetual;
    public float matchTime=180f;
    float currentMatchTime;
    float sendTimer;

    public List<PlayerInfo> allPlayers = new List<PlayerInfo>();
    int index;
    List<LeaderBoardPlayer> lboardPlayers = new List<LeaderBoardPlayer>();
    public static MatchManager instance;
    void Awake()
    {
        instance=this;    
    }
    
    void Start()
    {
        if(!PhotonNetwork.IsConnected)
        {
            SceneManager.LoadScene(0);
        }
        else
        {
            NewPlayerSend(PhotonNetwork.NickName);
            state = GameState.Playing;
            SetUpTimer();
        }
        if(!PhotonNetwork.IsMasterClient)
        {
            UIController.Instance.TimerTxt.gameObject.SetActive(false);
        }
    }

    
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Tab) && state != GameState.Ending)
        {
            if(UIController.Instance.leaderboard.activeInHierarchy)
            {
                UIController.Instance.leaderboard.SetActive(false);
            }
            else
            {
                ShowLeaderBoard();
            }
            
        }
        if(PhotonNetwork.IsMasterClient)
        {
            if(currentMatchTime > 0f && state == GameState.Playing)
            {
                currentMatchTime -= Time.deltaTime;
                if(currentMatchTime<=0)
                {
                    currentMatchTime=0f;
                    state=GameState.Ending;
                    Debug.Log(state);
                    ListPlayersSend();
                    StateCheck();
                    
                }
                UpdateMatchTimer();
                sendTimer-=Time.deltaTime;
                if(sendTimer<=0)
                {
                    sendTimer+=1;
                    TimerSyncSend();
                }
            }
        }
       
    }
    public void OnEvent(EventData photonEvent)
    {
        if(photonEvent.Code<200)
        {
            EventCodes theEvent = (EventCodes)photonEvent.Code;
            Debug.Log("The recieved eventcode: " + theEvent);
            object[] data = (object[])photonEvent.CustomData;
            switch(theEvent)
            {
                case EventCodes.NewPlayer:
                    NewPlayerRecieve(data);
                    break;
                case EventCodes.ListPlayers:
                    ListPlayersRecieve(data);
                    break;
                case EventCodes.UpdateStats:
                    UpdateStatsRecieve(data);
                    break;
                case EventCodes.NextMatch:
                    NextMatchReceive();
                    break;
                case EventCodes.TimerSync:
                    TimerSyncReceive(data);
                    break;
            }   
        }

    }
    public override void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }
    public override void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }
    public void NewPlayerSend(string userName)
    {
        object[] package = new object[4];
        package[0]=userName;
        package[1]=PhotonNetwork.LocalPlayer.ActorNumber;
        package[2]=0;
        package[3]=0;

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.NewPlayer,
            package,
            new RaiseEventOptions{Receivers = ReceiverGroup.MasterClient},
            new SendOptions{Reliability=true}
        );
    }
    public void NewPlayerRecieve(object[] dataRecevied)
    {
        PlayerInfo player = new PlayerInfo((string)dataRecevied[0],(int)dataRecevied[1],(int)dataRecevied[2],(int)dataRecevied[3]);
        allPlayers.Add(player);
        ListPlayersSend();
    }
    public void ListPlayersSend()
    {
        object[] package = new object[allPlayers.Count+1];
        package[0] = state;
        for(int i=0;i<allPlayers.Count;i++)
        {
            object[] piece = new object[4];
            piece[0]=allPlayers[i].name;
            piece[1]=allPlayers[i].actor;
            piece[2]=allPlayers[i].kills;
            piece[3] = allPlayers[i].deaths;
            package[i+1]=piece;

        }
        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.ListPlayers,
            package,
            new RaiseEventOptions{Receivers = ReceiverGroup.All},
            new SendOptions{Reliability=true}
        );

    }
    public void ListPlayersRecieve(object[] dataRecevied)
    {
        allPlayers.Clear();
        state = (GameState)dataRecevied[0];
        for(int i=1; i<dataRecevied.Length;i++)
        {
            object[] peice = (object[])dataRecevied[i];
            PlayerInfo player = new PlayerInfo(
                (string)peice[0],
                (int)peice[1],
                (int)peice[2],
                (int)peice[3]
            );
            allPlayers.Add(player);
            if(PhotonNetwork.LocalPlayer.ActorNumber==player.actor)
            {
                index=i-1;
            }
        }
        StateCheck();
        
    }
    public void UpdateStatsSend(int actorSending,int statToUpdate,int amountTochange)
    {
        object[] package = new object[]{actorSending,statToUpdate,amountTochange};
        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.UpdateStats,
            package,
            new RaiseEventOptions{Receivers = ReceiverGroup.All},
            new SendOptions{Reliability=true}
        );
    }
    public void UpdateStatsRecieve(object[] dataRecevied)
    {
        int actor=(int)dataRecevied[0];
        int statType = (int)dataRecevied[1];
        int amount = (int)dataRecevied[2];
        for(int i=0;i<allPlayers.Count;i++)
        {
            if(allPlayers[i].actor==actor)
            {
                switch(statType)
                {
                    case 0://Kills
                        allPlayers[i].kills+=amount;
                        Debug.Log("Player "+allPlayers[i].name+" Kills: "+allPlayers[i].kills);
                        break;
                    case 1://deaths
                        allPlayers[i].deaths+=amount;
                        Debug.Log("Player "+allPlayers[i].name+" Deaths: "+allPlayers[i].deaths);
                        break;
                }
                if(index==i)
                {
                    UpdateStatsDisplay();
                }
                if(UIController.Instance.leaderboard.activeInHierarchy)
                {
                    ShowLeaderBoard();
                }
                break;
            }
        }
        ScoreCheck();
        
    }
    public void UpdateStatsDisplay()
    {
        if(allPlayers.Count>index)
        {
            UIController.Instance.killsText.text="Kills: "+allPlayers[index].kills;
            UIController.Instance.deathText.text="Deaths: "+allPlayers[index].deaths;
        }
        else
        {
            UIController.Instance.killsText.text="Kills: 0";
            UIController.Instance.killsText.text="Deaths: 0";
        }
    }
    public void NextMatchSend()
    {
        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.NextMatch,
            null,
            new RaiseEventOptions{Receivers = ReceiverGroup.All},
            new SendOptions{Reliability=true}
        );
    }
    public void NextMatchReceive()
    {
        state = GameState.Playing;
        UIController.Instance.endScreen.SetActive(false);
        UIController.Instance.leaderboard.SetActive(false);
        foreach(PlayerInfo player in allPlayers)
        {
            player.kills=0;
            player.deaths=0;
        }
        UpdateStatsDisplay();
        PlayerSpawner.instance.SpawnPlayer();
        SetUpTimer();
    }
    public void TimerSyncSend()
    {
        object[] package = new object[]{(int)currentMatchTime,state};
         PhotonNetwork.RaiseEvent(
            (byte)EventCodes.TimerSync,
            package,
            new RaiseEventOptions{Receivers = ReceiverGroup.All},
            new SendOptions{Reliability=true}
        );
    }
    public void TimerSyncReceive(object[] dataRecevied)
    {
        currentMatchTime=(int)dataRecevied[0];
        state = (GameState)dataRecevied[1];
        UpdateMatchTimer();
        UIController.Instance.TimerTxt.gameObject.SetActive(true);
    }
    void ShowLeaderBoard()
    {
        UIController.Instance.leaderboard.SetActive(true);
        foreach(LeaderBoardPlayer lp in lboardPlayers)
        {
            Destroy(lp.gameObject);
        }
        lboardPlayers.Clear();
        UIController.Instance.lboardPlayerDisplay.gameObject.SetActive(false);
        List<PlayerInfo> sorted = SortPlayers(allPlayers);
        foreach(PlayerInfo player in sorted)
        {
            LeaderBoardPlayer newDisplay = Instantiate(UIController.Instance.lboardPlayerDisplay,UIController.Instance.lboardPlayerDisplay.transform.parent);
            newDisplay.SetDetails(player.name,player.kills,player.deaths);
            newDisplay.gameObject.SetActive(true);
            lboardPlayers.Add(newDisplay);
        }
    }
    List<PlayerInfo> SortPlayers(List<PlayerInfo> players)
    {
        List<PlayerInfo> sorted = new List<PlayerInfo>();
        while(sorted.Count<players.Count)
        {
            int highest=-1;
            PlayerInfo selectedPlayer = players[0];
            foreach(PlayerInfo player in players)
            {
                if(!sorted.Contains(player))
                {
                    if(player.kills>highest)
                    {
                        selectedPlayer=player;
                        highest=player.kills;
                    }
                }
            }
            sorted.Add(selectedPlayer);
        }
        return sorted;
    }
    public override void OnLeftRoom()
    {
        base.OnLeftRoom();
        SceneManager.LoadScene(0);
    }
    void ScoreCheck()
    {
        bool winnerFound=false;
        foreach(PlayerInfo player in allPlayers)
        {
            if(player.kills>=killsToWin && killsToWin>0)
            {
                winnerFound=true;
                break;
            }
        }
        if(winnerFound)
        {
            if(PhotonNetwork.IsMasterClient && state !=GameState.Ending)
            {
                state = GameState.Ending;
                ListPlayersSend();
            }
        }
    }
    void StateCheck()
    {
        if(state==GameState.Ending)
        {
            EndGame();
        }

    }
    void EndGame()
    {
        state = GameState.Ending;
        if(PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.DestroyAll();
        }
        UIController.Instance.endScreen.SetActive(true);
        ShowLeaderBoard();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible=true;
        Camera.main.transform.position=mapCamPoint.position;
        Camera.main.transform.rotation=mapCamPoint.rotation;
        StartCoroutine(EndCo());
    }
    IEnumerator EndCo()
    {
        yield return new WaitForSeconds(waitAfterRnding);
        if(!perpetual)
        {
            PhotonNetwork.AutomaticallySyncScene=false;
            PhotonNetwork.LeaveRoom();
        }
        else
        {

            if(PhotonNetwork.IsMasterClient)
            {
                if(!Launcher.instance.changeMapBetweenRounds)
                {
                    NextMatchSend();
                }
                else
                {
                    int newLevel = Random.Range(0,Launcher.instance.allMaps.Length);
                    if(Launcher.instance.allMaps[newLevel] == SceneManager.GetActiveScene().name)
                    {
                        NextMatchSend();
                    }
                    else
                    {
                        PhotonNetwork.LoadLevel(Launcher.instance.allMaps[newLevel]);
                    }
                }
                
            }
        }
        
    }
    public void SetUpTimer()
    {
        if(matchTime>0)
        {
            currentMatchTime=matchTime;
            UpdateMatchTimer();
        }
    }
    public void UpdateMatchTimer()
    {
        var timeToDisplay = System.TimeSpan.FromSeconds(currentMatchTime);
        UIController.Instance.TimerTxt.text= timeToDisplay.Minutes.ToString("00")+":"+timeToDisplay.Seconds.ToString("00");
    }
}
[System.Serializable]
public class PlayerInfo
{
    public string name;
    public int actor,kills,deaths;
    public PlayerInfo(string _name,int _actor,int _kills,int _deaths)
    {
        name =_name;
        actor = _actor;
        kills = _kills;
        deaths = _deaths;
    }
}
