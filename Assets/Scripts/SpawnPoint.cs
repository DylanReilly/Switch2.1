using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    private bool usedStatus = false;

    public void SetUsed(bool status)
    {
        usedStatus = status;
    }

    public bool GetUsedStatus()
    {
        return usedStatus;
    }
}
