using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Realtime;

public class RoomButton : MonoBehaviour
{
    [SerializeField] TMP_Text roomBtnTxt;
    RoomInfo info;
    public void SetButtonDetails(RoomInfo inputInfo)
    {
        info=inputInfo;
        roomBtnTxt.text=info.Name;
    }
    public void OpenRoom()
    {
        Launcher.instance.JoinRoom(info);
    }
}
