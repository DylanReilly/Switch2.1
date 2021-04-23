using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EasyInsult : MonoBehaviour
{
    public HudHandler hudHandler = null;

    public void SendInsult(string message)
    {
        hudHandler.SendTextMessage(message);
    }
}
