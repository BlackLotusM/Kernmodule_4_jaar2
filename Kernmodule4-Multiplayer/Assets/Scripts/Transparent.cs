using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Transparent : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        this.GetComponent<TextMeshProUGUI>().color = new Color(this.GetComponent<TextMeshProUGUI>().color.r, this.GetComponent<TextMeshProUGUI>().color.g, this.GetComponent<TextMeshProUGUI>().color.b, this.GetComponent<TextMeshProUGUI>().color.a - 0.001f);
    }
}
