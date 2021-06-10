using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class fadeText : MonoBehaviour
{
    public TMP_Text tmp;
    public TMP_Text login;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Color temp = tmp.color;
        temp.a -= 0.5f * Time.deltaTime;
        tmp.color = temp;

        Color temp2 = login.color;
        temp2.a -= 0.5f * Time.deltaTime;
        login.color = temp2;
    }

    public void SetCorrect()
    {
        tmp.text = "Correct";
        tmp.color = new Color(0, 1, 0, 1);
    }

    public void SetFault()
    {
        tmp.text = "Something went wrong";
        tmp.color = new Color(1, 0.2f , 0.5f, 1);
    }

    public void SetZero()
    {
        tmp.text = "";
        tmp.color = new Color(0, 0, 0, 0);
    }

    public void setLogin()
    {
        login.text = "Something went wrong";
        login.color = new Color(1, 0.2f, 0.5f, 1);
    }
}
