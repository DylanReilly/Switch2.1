using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiscoSpin : MonoBehaviour
{
    public float discoTimeout;

    void OnEnable()
    {
        discoTimeout = 30f;
    }

    void Update()
    {
        if (discoTimeout > 0)
        {
            transform.Rotate(0, 30 * Time.deltaTime, 0);
            discoTimeout -= Time.deltaTime;
        }
        else 
        {
            gameObject.SetActive(false);
        }
    }
}
