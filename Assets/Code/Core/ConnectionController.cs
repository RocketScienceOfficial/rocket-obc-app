using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConnectionController : MonoBehaviour
{
    public static event EventHandler OnConnected;
    public static event EventHandler OnDisconnected;
}