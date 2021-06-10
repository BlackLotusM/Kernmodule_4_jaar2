using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;
using System.Linq;
using System;
using System.Net;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

public enum GameEvent
{
    NUMBER = 0,
    NUMBER_REPLY = 1,
    PING = 2,
    PONG = 3,
    TABLEUPDATE = 4,
    TABLE_REPLY = 5,
    TABLE_EXIT = 6,
    READ_SCORE = 7,
    TABLE_HIT = 8,
    TABLE_STAND = 9,
    HIDE_SET = 10,
    SHOW_SET = 11,
    SEND_NEWNUMBER = 12,
    HGHSCORE = 13,
    HGHSCORERECIEVE = 14,
    FORCEUPDATE = 15,
    DISCONNECT = 16,
    DISCONNECTHANDLE = 17,
}

delegate void GameEventHandler(DataStreamReader stream, object sender, NetworkConnection connection);

public class TransportServer : MonoBehaviour
{
    static Dictionary<GameEvent, GameEventHandler> gameEventDictionary = new Dictionary<GameEvent, GameEventHandler>() {
            // link game events to functions...
            { GameEvent.NUMBER, NumberHandler },
            { GameEvent.PING, PingHandler },
            { GameEvent.TABLEUPDATE, TableUpdate },
            { GameEvent.TABLE_EXIT, TableExit },
            { GameEvent.TABLE_HIT, TableHit },
            { GameEvent.TABLE_STAND, TableStand },
            { GameEvent.HGHSCORE, HighScore },
            {GameEvent.DISCONNECT, disco },
        };

   

    public static int scoreDealer;
    static int table1;
    static bool standTable1, table1Done;

    static int table2;
    static bool standTable2, table2Done, table1Bust, table2Bust, started;
    public static string session;
    public int serverPort = 1511;

    public NetworkDriver m_Driver;
    private NativeList<NetworkConnection> m_Connections;
    private static NativeList<NetworkConnection> m_Connections2;
    static int disconect;
    public static List<PlayerData> PlayerList = new List<PlayerData>();
    [SerializeField]
    public List<PlayerData> PlayerList2 = new List<PlayerData>();
    [SerializeField]
    public static List<TableData> TableList = new List<TableData>();
    [SerializeField]
    public List<TableData> TableList2 = new List<TableData>();

    DataStreamWriter writer;
    static TransportServer instance;
    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        TableList = TableList2;
        m_Driver = NetworkDriver.Create();
        var endpoint = NetworkEndPoint.AnyIpv4;
        endpoint.Port = 1511;
        if (m_Driver.Bind(endpoint) != 0)
            Debug.Log("Failed to bind to port 1511");
        else
            m_Driver.Listen();

        m_Connections = new NativeList<NetworkConnection>(2, Allocator.Persistent);
    }

    private void OnDestroy()
    {
        m_Driver.Dispose();
        m_Connections.Dispose();
    }

    void Update()
    {
        
        m_Connections2 = m_Connections;
        PlayerList2 = PlayerList;
        m_Driver.ScheduleUpdate().Complete();

        // Clean up connections
        for (int i = 0; i < m_Connections.Length; i++)
        {
            if (!m_Connections[i].IsCreated)
            {
                m_Connections.RemoveAtSwapBack(i);
                --i;
            }
        }

        // Accept new connections
        NetworkConnection c;
        while ((c = m_Driver.Accept()) != default(NetworkConnection))
        {
            m_Connections.Add(c);
            Debug.Log("Accepted a connection");
        }

        DataStreamReader stream;
        for (int i = 0; i < m_Connections.Length; i++)
        {
            if (!m_Connections[i].IsCreated)
                continue;

            NetworkEvent.Type cmd;
            while ((cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream)) != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Data)
                {
                    // Check which GameEvent we've received
                    GameEvent gameEventType = (GameEvent)stream.ReadUInt();
                    //Debug.Log(gameEventType);

                    if (gameEventDictionary.ContainsKey(gameEventType))
                    {
                        gameEventDictionary[gameEventType].Invoke(stream, this, m_Connections[i]);
                    }
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    disconect = m_Connections[i].InternalId;
                    Debug.Log("Client disconnected from server");
                    try
                    {
                        PlayerList.Remove(PlayerList.Where(p => p.connectionID == disconect).First());
                    }
                    catch
                    {
                    }
                    m_Connections[i] = default(NetworkConnection);
                    TableExit(stream, this, m_Connections[i]);
                }

                if (TableList[0].playerID != 0 && TableList[1].playerID != 0)
                {
                    if (scoreDealer == 0 && table1 == 0 && table2 == 0)
                    {
                        scoreDealer += UnityEngine.Random.Range(0, 10);
                        table1 += UnityEngine.Random.Range(0, 10);
                        table2 += UnityEngine.Random.Range(0, 10);
                        UpdateScore(stream, this, m_Connections[i]);
                    }
                    else
                    {
                        if (table1Done && table2Done)
                        {
                            if (standTable1 && standTable2)
                            {
                                if (!started)
                                {
                                    started = true;
                                    StartCoroutine(dealerHand(stream, this, m_Connections[i]));
                                }
                            }
                            else
                            {
                                if (!standTable1)
                                {
                                    //Give new number
                                    //Reset button
                                    int number = UnityEngine.Random.Range(0, 10);
                                    table1 += number;
                                    if (table1 > 21)
                                    {
                                        //Bust
                                        table1Bust = true;
                                        standTable1 = true;
                                    }
                                    else
                                    {
                                        table1Done = false;
                                        ResetTable1(stream, this, m_Connections[i]);
                                    }
                                    SendScore(stream, this, m_Connections[i], 1, number);
                                    UpdateScore(stream, this, m_Connections[i]);
                                }

                                if (!standTable2)
                                {
                                    //Give new number
                                    //Reset button
                                    int number = UnityEngine.Random.Range(0, 10);
                                    table2 += number;
                                    if (table2 > 21)
                                    {
                                        table2Bust = true;
                                        standTable2 = true;
                                    }
                                    else
                                    {
                                        table2Done = false;
                                        ResetTable2(stream, this, m_Connections[i]);
                                    }
                                    SendScore(stream, this, m_Connections[i], 2, number);
                                    UpdateScore(stream, this, m_Connections[i]);
                                }
                            }
                        }
                    }
                }
                else
                {
                    scoreDealer = 0;
                    table1 = 0;
                    table2 = 0;
                    UpdateScore(stream, this, m_Connections[i]);
                }
            }
        }
    }

    private static void disco(DataStreamReader stream, object sender, NetworkConnection connection)
    {
        ResetTable1(stream, sender, connection);
        ResetTable2(stream, sender, connection);
        disconect = connection.InternalId;
        Debug.Log("Client disconnected from server");
        PlayerList.Remove(PlayerList.Where(p => p.connectionID == disconect).First());
        connection = default(NetworkConnection);
        TableExit(stream, sender, connection);

        TransportServer server = sender as TransportServer;
        DataStreamWriter writer;
        int result2 = server.m_Driver.BeginSend(NetworkPipeline.Null, connection, out writer);
        if (result2 == 0)
        {
            // Add GameEvent Reply uint
            writer.WriteUInt((uint)GameEvent.DISCONNECTHANDLE);
            server.m_Driver.EndSend(writer);
        }
    }


    public static bool table1Count;
    public static bool table2Count;
    public static IEnumerator dealerHand(DataStreamReader stream, object sender, NetworkConnection connection)
    {
        //Should be rewriten

        while (standTable1 && standTable2)
        {
            table1Count = false;
            table2Count = false;

            //If table bus dealer win
            if (table1Bust == true && table2Bust == true)
            {
                Debug.Log("Dealer Win");
                NewRound(stream, sender, connection);
            }
            else if (scoreDealer < 17)
            {
                //Draw till 17
                scoreDealer += UnityEngine.Random.Range(0, 10);
                UpdateScore(stream, sender, connection);
                yield return new WaitForSeconds(2);
            }
            else
            {
                if (!table1Bust)
                {
                    if (scoreDealer < table1)
                    {
                        scoreDealer += UnityEngine.Random.Range(0, 10);
                        UpdateScore(stream, sender, connection);
                        yield return new WaitForSeconds(2);
                    }
                    else
                    {
                        table1Count = true;
                    }
                }
                else
                {
                    table1Count = true;
                }

                if (!table2Bust)
                {
                    if (scoreDealer < table2)
                    {
                        scoreDealer += UnityEngine.Random.Range(0, 10);
                        UpdateScore(stream, sender, connection);
                        yield return new WaitForSeconds(2);
                    }
                    else
                    {
                        table2Count = true;
                    }
                }
                else
                {
                    table2Count = true;
                }

                if(table2Count && table1Count)
                {
                    if (!table1Bust)
                    {
                        if(table1 >= scoreDealer || scoreDealer > 21)
                        {
                            Debug.Log("Table1 win");
                            TableData data = TableList.FirstOrDefault(w => w.tafel == 1);
                            instance.StartCoroutine(addScore(session, data.playerID, 5));
                        }
                    }
                    else
                    {
                        Debug.Log("Table 1 Lose");
                    }

                    if (!table2Bust)
                    {
                        if (table2 >= scoreDealer || scoreDealer > 21)
                        {
                            Debug.Log("Table2 win");
                            TableData data = TableList.FirstOrDefault(w => w.tafel == 2);
                            instance.StartCoroutine(addScore(session, data.playerID, 5));
                        }
                    }
                    else
                    {
                        Debug.Log("Table 2 Lose");
                    }
                    NewRound(stream, sender, connection);
                }
            }
        }
    }

    public static IEnumerator addScore(string ses, int id, int score)
    {
        string url = "https://studenthome.hku.nl/~mikey.woudstra/addscore.php?sessionid="+ses+"&id="+id+"&score="+score;
        var json = new WebClient().DownloadString(url);
        yield return json;
        if (json != null)
        {
            var details = JObject.Parse(json);
            if ((int)details["id"] == 1)
            {
                //session = (string)details["session"];
            }
            else
            {
                string url2 = "https://studenthome.hku.nl/~mikey.woudstra/serverlogin.php?id=1&password=test";
                var json2 = new WebClient().DownloadString(url2);
                yield return json2;
                if (json2 != null)
                {
                    var details2 = JObject.Parse(json2);
                    if ((int)details2["id"] == 1)
                    {
                        session = (string)details2["session"];
                        string url3 = "https://studenthome.hku.nl/~mikey.woudstra/addscore.php?sessionid=" + session + "&id=" + id + "&score=" + score;
                        var json3 = new WebClient().DownloadString(url3);
                        yield return json3;
                    }
                }
            }
        }
    }

    static void SendScore(DataStreamReader stream, object sender, NetworkConnection connection, int tafelID, int score)
    {
        TransportServer server = sender as TransportServer;
        TableData data = TableList.FirstOrDefault(w => w.tafel == tafelID);

        DataStreamWriter writer;
        int result2 = server.m_Driver.BeginSend(NetworkPipeline.Null, m_Connections2[data.connectionID], out writer);
        if (result2 == 0)
        {
            // Add GameEvent Reply uint
            writer.WriteUInt((uint)GameEvent.SEND_NEWNUMBER);
            writer.WriteUInt((uint)score);
            server.m_Driver.EndSend(writer);
        }
    }

    // Event Functions
    static void UpdateScore(DataStreamReader stream, object sender, NetworkConnection connection)
    {
        TransportServer server = sender as TransportServer;
        foreach (NetworkConnection n in m_Connections2)
        {
            DataStreamWriter writer;
            int result2 = server.m_Driver.BeginSend(NetworkPipeline.Null, n, out writer);
            if (result2 == 0)
            {
                // Add GameEvent Reply uint
                writer.WriteUInt((uint)GameEvent.READ_SCORE);
                writer.WriteUInt((uint)scoreDealer);
                writer.WriteUInt((uint)table1);
                writer.WriteUInt((uint)table2);
                server.m_Driver.EndSend(writer);
            }
        }
    }

    static void NewRound(DataStreamReader stream, object sender, NetworkConnection connection)
    {
        
        scoreDealer = 0;
        table1 = 0;
        table2 = 0;
        started = false;

        ResetTable1(stream, sender, connection);
        ResetTable2(stream, sender, connection);

        table1Bust = false;
        table2Bust = false;
        table1Done = false;
        table2Done = false;
        standTable1 = false;
        standTable2 = false;
        scoreDealer += UnityEngine.Random.Range(0, 10);
        table1 += UnityEngine.Random.Range(0, 10);
        table2 += UnityEngine.Random.Range(0, 10);
        UpdateScore(stream, sender, connection);
        HighScore2(stream, sender, connection);
    }

    static void TableHit(DataStreamReader stream, object sender, NetworkConnection connection)
    {
        TransportServer server = sender as TransportServer;

        TableData data = TableList.FirstOrDefault(w => w.connectionID == connection.InternalId);
        if (data.tafel == 1)
        {
            table1Done = true;
        }
        else
        {
            table2Done = true;
        }

        DataStreamWriter writer;
        int result = server.m_Driver.BeginSend(NetworkPipeline.Null, connection, out writer);

        if (result == 0)
        {
            // Add GameEvent Reply uint
            writer.WriteUInt((uint)GameEvent.HIDE_SET);
            server.m_Driver.EndSend(writer);
        }
    }

    static void ResetTable1(DataStreamReader stream, object sender, NetworkConnection connection)
    {
        TransportServer server = sender as TransportServer;

        TableData data = TableList.FirstOrDefault(w => w.tafel == 1);
        DataStreamWriter writer;
        int result = server.m_Driver.BeginSend(NetworkPipeline.Null, m_Connections2[data.connectionID], out writer);

        if (result == 0)
        {
            // Add GameEvent Reply uint
            writer.WriteUInt((uint)GameEvent.SHOW_SET);
            server.m_Driver.EndSend(writer);
        }
    }

    static void ResetTable2(DataStreamReader stream, object sender, NetworkConnection connection)
    {
        TransportServer server = sender as TransportServer;

        TableData data = TableList.FirstOrDefault(w => w.tafel == 2);
        DataStreamWriter writer;
        int result = server.m_Driver.BeginSend(NetworkPipeline.Null, m_Connections2[data.connectionID], out writer);

        if (result == 0)
        {
            // Add GameEvent Reply uint
            writer.WriteUInt((uint)GameEvent.SHOW_SET);
            server.m_Driver.EndSend(writer);
        }
    }

    static void TableStand(DataStreamReader stream, object sender, NetworkConnection connection)
    {
        TransportServer server = sender as TransportServer;
        TableData data = TableList.FirstOrDefault(w => w.connectionID == connection.InternalId);
        Debug.Log(data.connectionID);
        if (data.tafel == 1)
        {
            standTable1 = true;
            table1Done = true;
        }
        else
        {
            standTable2 = true;
            table2Done = true;
        }

        DataStreamWriter writer;
        int result = server.m_Driver.BeginSend(NetworkPipeline.Null, connection, out writer);

        if (result == 0)
        {
            // Add GameEvent Reply uint
            writer.WriteUInt((uint)GameEvent.HIDE_SET);
            server.m_Driver.EndSend(writer);
        }
    }

    // Event Functions
    static void NumberHandler(DataStreamReader stream, object sender, NetworkConnection connection)
    {
        uint id = stream.ReadUInt();
        Unity.Collections.FixedString32 naam = stream.ReadFixedString32();
        Unity.Collections.FixedString64 session = stream.ReadFixedString64();

        PlayerList.Add(new PlayerData((int)id, naam, session, 0, connection.InternalId));

        TransportServer server = sender as TransportServer;

        DataStreamWriter writer;
        int result = server.m_Driver.BeginSend(NetworkPipeline.Null, connection, out writer);

        if (result == 0)
        {
            // Add GameEvent Reply uint
            writer.WriteUInt((uint)GameEvent.NUMBER_REPLY);
            writer.WriteUInt(id);

            foreach (TableData td in TableList)
            {
                writer.WriteUInt((uint)td.playerID);
                writer.WriteFixedString32(td.PlayerName);
                writer.WriteUInt((uint)td.tafel);
            }
            server.m_Driver.EndSend(writer);
        }
    }

    static void PingHandler(DataStreamReader stream, object sender, NetworkConnection connection)
    {
        TransportServer server = sender as TransportServer;
        DataStreamWriter writer;
        server.m_Driver.BeginSend(NetworkPipeline.Null, connection, out writer);
        writer.WriteUInt((uint)GameEvent.PONG);
        server.m_Driver.EndSend(writer);
    }

    static void TableExit(DataStreamReader stream, object sender, NetworkConnection connection)
    {
        //disconect
        TransportServer server = sender as TransportServer;


        TableList.Where(w => w.connectionID == disconect).ToList().ForEach(s => s.PlayerName = "");
        TableList.Where(w => w.connectionID == disconect).ToList().ForEach(s => s.playerID = 0);
        TableList.Where(w => w.connectionID == disconect).ToList().ForEach(s => s.connectionID = 0);


        foreach (NetworkConnection n in m_Connections2)
        {
            DataStreamWriter writer2;
            int result2 = server.m_Driver.BeginSend(NetworkPipeline.Null, n, out writer2);
            if (result2 == 0)
            {

                // Add GameEvent Reply uint
                writer2.WriteUInt((uint)GameEvent.TABLE_REPLY);
                foreach (TableData td in TableList)
                {
                    writer2.WriteUInt((uint)td.playerID);
                    writer2.WriteFixedString32(td.PlayerName);
                    writer2.WriteUInt((uint)td.tafel);
                }
                server.m_Driver.EndSend(writer2);
            }
        }
    }

    static void TableUpdate(DataStreamReader stream, object sender, NetworkConnection connection)
    {
        Debug.Log("_PingRecieved");
        TransportServer server = sender as TransportServer;
        uint PID = stream.ReadUInt();
        uint tableID = stream.ReadUInt();
        string name = Convert.ToString(stream.ReadFixedString32());

        TableList.Where(w => w.playerID == (int)PID).ToList().ForEach(s => s.PlayerName = "");
        TableList.Where(w => w.playerID == (int)PID).ToList().ForEach(s => s.playerID = 0);

        TableList.Where(w => w.tafel == tableID).ToList().ForEach(s => s.playerID = (int)PID);
        TableList.Where(w => w.tafel == tableID).ToList().ForEach(s => s.connectionID = connection.InternalId);
        TableList.Where(w => w.tafel == tableID).ToList().ForEach(s => s.PlayerName = Convert.ToString(name));
        TableList.Where(w => w.tafel == tableID).ToList().ForEach(s => s.tafel = (int)tableID);

        foreach (NetworkConnection n in m_Connections2)
        {
            DataStreamWriter writer2;
            int result2 = server.m_Driver.BeginSend(NetworkPipeline.Null, n, out writer2);
            if (result2 == 0)
            {

                // Add GameEvent Reply uint
                writer2.WriteUInt((uint)GameEvent.TABLE_REPLY);
                foreach (TableData td in TableList)
                {
                    writer2.WriteUInt((uint)td.playerID);
                    writer2.WriteFixedString32(td.PlayerName);
                    writer2.WriteUInt((uint)td.tafel);
                }
                server.m_Driver.EndSend(writer2);
            }
        }
    }

    static int count;
    public static IEnumerator HighScoreIE(int amount, DataStreamReader stream, object sender, NetworkConnection connection)
    {
        string url = "https://studenthome.hku.nl/~mikey.woudstra/highscore.php?sessionid=" + session + "&amount=" + amount;
        var json = new WebClient().DownloadString(url);
        yield return json;
        try
        {
            var RootObjects = JsonConvert.DeserializeObject<List<MyItems>>(json);

            TransportServer server = sender as TransportServer;
            DataStreamWriter writer2;

            int result2 = server.m_Driver.BeginSend(NetworkPipeline.Null, connection, out writer2);
            if (result2 == 0)
            {
                count = 0;
                foreach (var rootObject in RootObjects)
                {
                    count++;
                }
                // Add GameEvent Reply uint
                writer2.WriteUInt((uint)GameEvent.HGHSCORERECIEVE);
                writer2.WriteUInt((uint)count);
                foreach (var rootObject in RootObjects)
                {
                    writer2.WriteFixedString128(rootObject.naam);
                    writer2.WriteUInt((uint)rootObject.score);
                }
                server.m_Driver.EndSend(writer2);
            }
        }
        catch
        {

        }
        
    }
    private static void HighScore2(DataStreamReader stream, object sender, NetworkConnection connection)
    {
        TransportServer server = sender as TransportServer;
        DataStreamWriter writer2;
        foreach (NetworkConnection n in m_Connections2)
        {
            int result2 = server.m_Driver.BeginSend(NetworkPipeline.Null, n, out writer2);
            if (result2 == 0)
            {
                writer2.WriteUInt((uint)GameEvent.FORCEUPDATE);
                server.m_Driver.EndSend(writer2);
            }
        }
    }

    private static void HighScore(DataStreamReader stream, object sender, NetworkConnection connection)
    {
        uint amount = stream.ReadUInt();
        instance.StartCoroutine(HighScoreIE((int)amount, stream, sender, connection));
    }

    public class MyItems
    {
        public string naam;
        public int score;
    }
}