using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;
using System;
using UnityEngine.SceneManagement;
using TMPro;

public class TransportClient : MonoBehaviour
{
    static Dictionary<GameEvent, GameEventHandler> gameEventDictionary = new Dictionary<GameEvent, GameEventHandler>() {
            // link game events to functions...
            { GameEvent.NUMBER_REPLY, NumberReplyHandler },
            { GameEvent.PONG, PongHandler },
            { GameEvent.TABLE_REPLY, TableReplyHandle },
            { GameEvent.READ_SCORE, ScoreUpdate },
            { GameEvent.HIDE_SET, HideSet },
            { GameEvent.SHOW_SET, ShowSet },
            { GameEvent.SEND_NEWNUMBER, NewNumber },
            { GameEvent.HGHSCORERECIEVE, highscore },
            { GameEvent.FORCEUPDATE, highscoreForce },
            { GameEvent.DISCONNECTHANDLE, discoHandle },
        };

    
    public static int listNumber;

    public static NetworkDriver m_Driver;
    public static NetworkConnection m_Connection;
    public bool Done;
    public static string serverIP;
    public int serverPort;
    NetworkEvent.Type cmd;
    static uint value;
    static bool isConnected;
    static bool pingSend;
    static float timeLeft = 15;
    public int playerID;
    public string displayName;
    public string session;
    public static int tableID = 0;
    public int oldTable;

    public static int scoreDealer;
    public static int table1;
    public static int table2;

    public static bool tableSet1Disable;
    public static bool tableSet2Disable;

    public int scoreDealerTEMP;
    public int table1TEMP;
    public int table2TEMP;

    DataStreamReader stream;
    static DataStreamWriter writer;

    [SerializeField]
    public static List<TableData> TableList = new List<TableData>();
    [SerializeField]
    public List<TableData> TableList2 = new List<TableData>();

    public GameObject tableSet1;
    public GameObject tableSet2;

    public TextMeshProUGUI scoreTafel1;
    public TextMeshProUGUI scoreTafel2;
    public TextMeshProUGUI scoreDealerDisplay;

    public TextMeshProUGUI addTafel1;
    public TextMeshProUGUI addTafel2;
    public TextMeshProUGUI addDealer;

    public GameObject text;
    public static GameObject text2;

    static TextMeshProUGUI addTafel1_s;
    static TextMeshProUGUI addTafel2_s;
    static TextMeshProUGUI addDealer_s;
    public GameObject thingie;
    public static GameObject thingie2;

    public GameObject loadRef;
    public static GameObject load;

    void Start()
    {
        load = loadRef;
        Debug.Log(serverIP);
        thingie2 = thingie;
        addTafel1_s = addTafel1;
        addTafel2_s = addTafel2;
        addDealer_s = addDealer;

        tableSet1.SetActive(false);
        tableSet2.SetActive(false);

        oldTable = tableID;
        TableList = TableList2;
        playerID = NetworkManager.playerID;
        displayName = NetworkManager.name;
        session = NetworkManager.sessionID;

        m_Driver = NetworkDriver.Create();
        m_Connection = default(NetworkConnection);
        if (serverIP == "")
        {
            var endpoint = NetworkEndPoint.Parse("86.91.184.42", (ushort)serverPort);
            m_Connection = m_Driver.Connect(endpoint);
        }
        else
        {
            var endpoint = NetworkEndPoint.Parse("" + serverIP, (ushort)serverPort);
            m_Connection = m_Driver.Connect(endpoint);
        }
        
    }

    public void OnDestroy()
    {
        m_Driver.Dispose();
    }

    public void disconnect()
    {
        int result = m_Driver.BeginSend(m_Connection, out writer);
        if (result == 0)
        {
            writer.WriteUInt((uint)GameEvent.DISCONNECT);
            m_Driver.EndSend(writer);
            m_Connection = default(NetworkConnection);
            SceneManager.LoadScene("MainMenu");
        }
    }

    public void disconnectForce()
    {
        SceneManager.LoadScene("MainMenu");
    }

    private static void discoHandle(DataStreamReader stream, object sender, NetworkConnection connection)
    {
        Debug.Log("Client got disconnected from server");
        m_Connection = default(NetworkConnection);
        SceneManager.LoadScene("MainMenu");
    }

    void Update()
    {
        if(listNumber == 0 || listNumber < 5)
        {
            listNumber = 5;
        }
        hs2 = hs;
        text2 = text;
        scoreTafel1.text = ""+table1TEMP;
        scoreTafel2.text = "" + table2TEMP;
        scoreDealerDisplay.text = "" + scoreDealerTEMP;

        TableList2 = TableList;
        m_Driver.ScheduleUpdate().Complete();

        if (!m_Connection.IsCreated)
        {
            if (!Done)
                Debug.Log("Something went wrong during connect");
            return;
        }
        
        while ((cmd = m_Connection.PopEvent(m_Driver, out stream)) != NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Connect)
            {
                //Should start sending dat to server
                Debug.Log("We are now connected to the server");
                int result = m_Driver.BeginSend(m_Connection, out writer);
                if (result == 0)
                {
                    // Game Event
                    Debug.Log("test");
                    writer.WriteUInt((uint)GameEvent.NUMBER);
                    writer.WriteUInt((uint)NetworkManager.playerID);
                    writer.WriteFixedString32(NetworkManager.name);
                    writer.WriteFixedString64(NetworkManager.sessionID);
                    m_Driver.EndSend(writer);
                    int result2 = m_Driver.BeginSend(m_Connection, out writer);
                    if (result2 == 0)
                    {
                        writer.WriteUInt((uint)GameEvent.HGHSCORE);
                        writer.WriteUInt((uint)5);
                        m_Driver.EndSend(writer);
                    }
                }
            }
            else if (cmd == NetworkEvent.Type.Data)
            {
                // Read GameEvent type from stream
                GameEvent gameEventType = (GameEvent)stream.ReadUInt();
                Debug.Log(gameEventType);

                if (gameEventDictionary.ContainsKey(gameEventType))
                {
                    gameEventDictionary[gameEventType].Invoke(stream, this, m_Connection);
                }
                else
                {
                    //Unsupported event received...
                }
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                Debug.Log("Client got disconnected from server");
                m_Connection = default(NetworkConnection);
                SceneManager.LoadScene("MainMenu");
            }
        }

        if (isConnected)
        {
            if (cmd == NetworkEvent.Type.Empty)
            {
                if(tableID != oldTable)
                {
                    oldTable = tableID;
                    int result = m_Driver.BeginSend(m_Connection, out writer);
                    if (result == 0)
                    {
                        writer.WriteUInt((uint)GameEvent.TABLEUPDATE);
                        writer.WriteUInt((uint)NetworkManager.playerID);
                        writer.WriteUInt((uint)tableID);
                        writer.WriteFixedString32(NetworkManager.name);
                        m_Driver.EndSend(writer);
                    }
                }
                else
                if (!pingSend)
                {
                    timeLeft -= Time.deltaTime;
                    if (timeLeft < 0)
                    {
                        pingSend = true;
                        int result = m_Driver.BeginSend(m_Connection, out writer);
                        if (result == 0)
                        {
                            writer.WriteUInt((uint)GameEvent.PING);
                            Debug.Log("_PingSend");
                            m_Driver.EndSend(writer);
                        }
                    }
                }

                foreach(TableData td in TableList)
                {
                    if(td.playerID != 0)
                    {
                        td.hide.SetActive(false);
                    }
                    else
                    {
                        td.hide.SetActive(true);
                    }
                }

                //TO-Do check if other player is connect otherwise show no button to be sure

                if(tableID == 1)
                {
                    if (tableSet1Disable)
                    {
                        tableSet1.SetActive(false);
                    }
                    else
                    if (tableSet1.activeSelf != true)
                    {
                        tableSet1.SetActive(true);
                        tableSet2.SetActive(false);
                    }
                }
                else if(tableID == 2)
                {
                    if (tableSet2Disable)
                    {
                        tableSet2.SetActive(false);
                    }else
                    if (tableSet2.activeSelf != true)
                    {
                        tableSet1.SetActive(false);
                        tableSet2.SetActive(true);
                    }
                }
                else
                {
                    tableSet1.SetActive(false);
                    tableSet2.SetActive(false);
                }

                scoreDealerTEMP = scoreDealer;
                table1TEMP = table1;
                table2TEMP = table2;
            }
        }
    }

    private static void highscoreForce(DataStreamReader stream, object sender, NetworkConnection connection)
    {
        int result = m_Driver.BeginSend(m_Connection, out writer);
        if (result == 0)
        {
            writer.WriteUInt((uint)GameEvent.HGHSCORE);
            writer.WriteUInt((uint)listNumber);
            m_Driver.EndSend(writer);
        }
    }

    public void updateHighScore(int amount)
    {
        int result = m_Driver.BeginSend(m_Connection, out writer);
        if (result == 0)
        {
            listNumber = amount;
            writer.WriteUInt((uint)GameEvent.HGHSCORE);
            writer.WriteUInt((uint)amount);
            m_Driver.EndSend(writer);
        }
    }

    public void updateHighScore()
    {
        int result = m_Driver.BeginSend(m_Connection, out writer);
        if (result == 0)
        {
            writer.WriteUInt((uint)GameEvent.HGHSCORE);
            writer.WriteUInt((uint)listNumber);
            m_Driver.EndSend(writer);
        }
    }

    public void hitButton()
    {
        int result = m_Driver.BeginSend(m_Connection, out writer);
        if (result == 0)
        {
            writer.WriteUInt((uint)GameEvent.TABLE_HIT);
            m_Driver.EndSend(writer);
        }
    }

    public void standButton()
    {
        int result = m_Driver.BeginSend(m_Connection, out writer);
        if (result == 0)
        {
            writer.WriteUInt((uint)GameEvent.TABLE_STAND);
            m_Driver.EndSend(writer);
        }
        updateHighScore();
    }

    static void HideSet(DataStreamReader stream, object sender, NetworkConnection connection)
    {
        if(tableID == 1)
        {
            tableSet1Disable = true;
        }
        else
        {
            tableSet2Disable = true;
        }
    }

    static void NewNumber(DataStreamReader stream, object sender, NetworkConnection connection)
    {
        if(tableID == 1)
        {
            int recieved = (int)stream.ReadUInt();
            addTafel1_s.text = ""+recieved;
            addTafel1_s.color = new Color(addTafel1_s.color.r, addTafel1_s.color.g, addTafel1_s.color.b, 1);
        }
        else
        {
            int recieved = (int)stream.ReadUInt();
            addTafel2_s.text = "" + recieved;
            addTafel2_s.color = new Color(addTafel2_s.color.r, addTafel2_s.color.g, addTafel2_s.color.b, 1);
        }
        //Debug.Log(recieved);
    }

    static void ShowSet(DataStreamReader stream, object sender, NetworkConnection connection)
    {
        if (tableID == 1)
        {
            tableSet1Disable = false;
        }
        else
        {
            tableSet2Disable = false;
        }
    }

    // Event Functions
    static void NumberReplyHandler(DataStreamReader stream, object sender, NetworkConnection connection)
    {
        load.SetActive(false);
        uint value2 = stream.ReadUInt();
        Debug.Log("Got the value = " + value2 + " back from the server");
        value = value2;
        TransportClient client = sender as TransportClient;

        foreach (TableData td in TableList)
        {
            td.playerID = (int)stream.ReadUInt();
            td.PlayerName = Convert.ToString(stream.ReadFixedString32());
            td.tafel = (int)stream.ReadUInt();
        }
        isConnected = true;
    }

    // Event Functions
    public void UpdataTable(int tableId)
    {
        tableID = tableId;
    }

    void OnApplicationQuit()
    {
        int result = m_Driver.BeginSend(m_Connection, out writer);
        if (result == 0)
        {
            writer.WriteUInt((uint)GameEvent.PING);
            Debug.Log("_PingSend");
            m_Driver.EndSend(writer);
        }
        m_Driver.Disconnect(m_Connection);
    }

    static void PongHandler(DataStreamReader stream, object sender, NetworkConnection connection)
    {
        pingSend = false;
        Debug.Log("_PongRecieved");
        timeLeft = 15;
    }

    static void TableReplyHandle(DataStreamReader stream, object sender, NetworkConnection connection)
    {
        TransportClient client = sender as TransportClient;

        foreach (TableData td in TableList)
        {
            td.playerID = (int)stream.ReadUInt();
            td.PlayerName = Convert.ToString(stream.ReadFixedString32());
            td.tafel = (int)stream.ReadUInt();
        }
        isConnected = true;
    }
    public static List<HighScore> hs = new List<HighScore>();
    [SerializeField]
    public List<HighScore> hs2 = new List<HighScore>();
    public static List<GameObject> spawned = new List<GameObject>();
    private static void highscore(DataStreamReader stream, object sender, NetworkConnection connection)
    {
        hs.Clear();
        foreach(GameObject go in spawned)
        {
            Destroy(go);
        }
        int amount = (int)stream.ReadUInt();
        for(int i = 0; i < amount; i++)
        {
            HighScore data = new HighScore();
            data.naam = Convert.ToString(stream.ReadFixedString128());
            GameObject temp = Instantiate(text2);
            temp.transform.parent = thingie2.transform;
            temp.transform.localScale = new Vector3(1, 1, 1);
            temp.GetComponent<TMP_Text>().text = data.naam;
            spawned.Add(temp);

            data.score = (int)stream.ReadUInt();
            GameObject temp2 = Instantiate(text2);
            temp2.transform.parent = thingie2.transform;
            temp2.transform.localScale = new Vector3(1, 1, 1);
            temp2.GetComponent<TMP_Text>().text = "Score: "+ data.score;
            spawned.Add(temp2);
            hs.Add(data);
        }
       
        Debug.Log(amount);
    }
    [System.Serializable]
    public class HighScore
    {
        public string naam;
        public int score;
    }

    static void ScoreUpdate(DataStreamReader stream, object sender, NetworkConnection connection)
    {
        TransportClient client = sender as TransportClient;
        scoreDealer = (int)stream.ReadUInt();
        table1 = (int)stream.ReadUInt();
        table2 = (int)stream.ReadUInt();
    }
}