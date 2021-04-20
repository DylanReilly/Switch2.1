using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MessageHandler : MonoBehaviour
{
    public Text textField = null;

    public void SendGameUpdate(string message)
    {
        textField.text += "\n";
        textField.text += message;
    }
}
