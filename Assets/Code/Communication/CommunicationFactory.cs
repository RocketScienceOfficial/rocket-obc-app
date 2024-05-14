using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CommunicationFactory
{
    private static ICommunicationInterface _communicationInterface;

    public static ICommunicationInterface GetCommunication()
    {
        _communicationInterface ??= new SerialCommunication();

        return _communicationInterface;
    }
}