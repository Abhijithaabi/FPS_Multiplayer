using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LeaderBoardPlayer : MonoBehaviour
{
    public TMP_Text playerNameTxt,KillsTxt,deathsTxt;
    public void SetDetails(string name,int kills,int deaths)
    {
        playerNameTxt.text=name;
        KillsTxt.text=kills.ToString();
        deathsTxt.text=deaths.ToString();
    }
}
