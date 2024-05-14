using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SerialCommunication : ICommunicationInterface
{
    public string GetPortName()
    {
        throw new NotImplementedException();
    }

    public bool IsConnected()
    {
        throw new NotImplementedException();
    }

    public void LoadConfig(Action<ConfigData> callback)
    {
        throw new NotImplementedException();
    }

    public void SaveConfig(ConfigData data, Action callback)
    {
        throw new NotImplementedException();
    }
}