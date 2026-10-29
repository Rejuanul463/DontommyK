using System;
using UnityEngine;

public class Switch : MonoBehaviour
{
    public bool value;

    [SerializeField] private GameObject on;
    [SerializeField] private GameObject off;
    
    
    public void swap()
    {
        on.SetActive(value);
        off.SetActive(!value);
        value = !value;
    }
}
