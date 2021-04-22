using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EasyInsultToggle : MonoBehaviour
{
    public GameObject easyInsultBox = null;

    public void EnableEasyInsult()
    {
        easyInsultBox.gameObject.SetActive(true);
    }

    public void DisableEasyInsult()
    {
        easyInsultBox.gameObject.SetActive(false);
    }
}
