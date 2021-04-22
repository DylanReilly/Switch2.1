using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerformanceSettings : MonoBehaviour
{
    GameObject scenery = null;
    void Start()
    {
        scenery = GameObject.Find("Scenery");
    }

    public void EnableScenery()
    {
        scenery.gameObject.SetActive(true);
    }

    public void DisableScenery()
    {
        scenery.gameObject.SetActive(false);
    }
}
