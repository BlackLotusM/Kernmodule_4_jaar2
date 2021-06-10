using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class PlayerData
{
    public int id, tafel, connectionID;
    public string PlayerName, session;


    public PlayerData(int id, Unity.Collections.FixedString32 name, Unity.Collections.FixedString64 session, int tafel, int cid)
    {
        this.id = id;
        this.PlayerName = Convert.ToString(name);
        this.session = Convert.ToString(session);
        this.tafel = tafel;
        this.connectionID = cid;
    }
}
