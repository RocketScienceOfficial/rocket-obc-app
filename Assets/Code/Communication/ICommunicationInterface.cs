using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICommunicationInterface
{
    string GetPortName();
    bool IsConnected();

    void LoadConfig(Action<ConfigData> callback);
    void SaveConfig(ConfigData data, Action callback);
}

public struct ConfigData
{
    public int parachuteHeight;
    public int launcherHeight;
    public int secondDelay;
    public int parachuteErrorSpeed;
}