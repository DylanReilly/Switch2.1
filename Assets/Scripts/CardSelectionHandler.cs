using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardSelectionHandler : MonoBehaviour
{
    public event Action<int> CardSelected;
    [SerializeField] private BoxCollider2D collider = null;

    void Update()
    { 
        
    }
}
