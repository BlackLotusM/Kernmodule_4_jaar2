using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Networking;
using System.Net;
using Newtonsoft.Json.Linq;
using UnityEngine.SceneManagement;

public class NetworkManager : MonoBehaviour
{
    [Header("Selection")]
    [SerializeField]
    private string serverScene = "";
    [SerializeField]
    private string clientScene = "";

    [Header("PlayerData")]
    [SerializeField]
    private string session;
    [SerializeField]
    private string playerName;

    [Header("Connecting")]
    public int serverID;
    public string serverPassword;
    public string serverIP;
    public GameObject connectionPanel;
    public GameObject failedToConnectPanel;
    public int tries;

    public fadeText ft;

    [Header("Login credentials")]
    public TMP_InputField userLogin;
    public TMP_InputField passwordLogin;

    [Header("Register credentials")]
    public TMP_InputField userRegister;
    public TMP_InputField passwordRegister;
    public TMP_InputField displayNameRegister;
    public TMP_InputField ip;

    public static string name;
    public static string sessionID;
    public static int playerID;

    private void Start()
    {
        StartCoroutine(ServerLogin());
        ft.SetZero();
    }

    public void Quit()
    {
        Application.Quit();
    }
    public IEnumerator ServerLogin()
    {
        string url = "https://studenthome.hku.nl/~mikey.woudstra/serverlogin.php?id="+ serverID + "&password=" + serverPassword ;
        var json = new WebClient().DownloadString(url);
        yield return json;
        if (json != null)
        {
            var details = JObject.Parse(json);
            if((int)details["id"] == 1)
            {
                session = (string)details["session"];
                TransportServer.session = session;
                if(session != "")
                {
                    connectionPanel.SetActive(false);
                }
            }
            else
            {
                tries++;
                yield return new WaitForSeconds(3);
                if (tries == 3)
                {
                    failedToConnectPanel.SetActive(true);
                }
                else
                {
                    StartCoroutine(ServerLogin());
                }
            }
        }
        else
        {
            Debug.Log("ERROR: Json is null");
            ResetClient();
            
        }
    }

    public IEnumerator LoginIE()
    {
        string url = "https://studenthome.hku.nl/~mikey.woudstra/authenticate.php?sessionid="+session+"&username="+ userLogin.text + "&password=" + passwordLogin.text;
        var json = new WebClient().DownloadString(url);
        yield return json;
        if (json != null)
        {
            TransportClient.serverIP = ip.text;
            Debug.Log(json);
            var details = JObject.Parse(json);
            int idCheck = (int)details["id"];
            if (idCheck == 0)
            {
                ResetClient();
                ft.setLogin();
            }
            else
            {
                Debug.Log("ID: "+ idCheck);
                playerName = details["displayname"].ToString();
                Debug.Log("Name: " + playerName);

                name = playerName;
                playerID = idCheck;
                sessionID = session;

                SceneManager.LoadScene(clientScene);
            }            
        }
        else
        {
            Debug.Log("ERROR: Json is null");
            ResetClient();
        }
    }

    public void Login()
    {
        if (userLogin.text == "" || passwordLogin.text == "")
        {
            ResetClient();
        }
        else
        {
            StartCoroutine(LoginIE());
        }
    }

    private void ResetClient()
    {
        playerName = "";
    }

    public void Register()
    {
        if(userRegister.text == "" || passwordRegister.text == "" || displayNameRegister.text == "")
        {
            Debug.Log("Fill in all fields");
        }
        else
        {
            StartCoroutine(RegisterIE());
        }
    }

    public void StartServer()
    {
        SceneManager.LoadScene(serverScene);
    }

    public IEnumerator RegisterIE()
    {
        string url = "https://studenthome.hku.nl/~mikey.woudstra/register.php?username=" + userRegister.text + "&password=" + passwordRegister.text + "&displayname=" + displayNameRegister.text + "&sessionid=" + session;
        var json = new WebClient().DownloadString(url);
        yield return json;
        var details = JObject.Parse(json);
        int status = (int)details["id"];
        if(status == 0)
        {
            ft.SetFault();
            userLogin.text = "";
            userRegister.text = "";
            passwordLogin.text = "";
            passwordRegister.text = "";
            ip.text = "";
            displayNameRegister.text = "";
        }
        else
        {
            ft.SetCorrect();
            userLogin.text = "";
            userRegister.text = "";
            passwordLogin.text = "";
            passwordRegister.text = "";
            ip.text = "";
            displayNameRegister.text = "";
        }
    }
}
