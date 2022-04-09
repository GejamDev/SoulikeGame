using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursorManager : MonoBehaviour
{
    void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

    }
}
