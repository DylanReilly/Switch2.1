using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lights : MonoBehaviour
{
    public GameObject mainLight = null;
    public GameObject discoLight = null;

    private bool isDiscoMode = false;
    private float discoModeTimeout;

    public void DiscoMode()
    {
        mainLight.SetActive(false);
        discoLight.SetActive(true);
        isDiscoMode = true;
        discoModeTimeout = 30f;
    }

    private void Update()
    {
        if(isDiscoMode)
        {
            if (discoModeTimeout > 0)
            {
                discoModeTimeout -= Time.deltaTime;
            }
            else 
            {
                mainLight.SetActive(true);
                isDiscoMode = false;
            }
        }
    }
}
