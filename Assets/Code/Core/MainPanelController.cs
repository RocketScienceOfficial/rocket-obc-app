using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MainPanelController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_PortText;

    private void Start()
    {
        SerialCommunication.Instance.OnConnected += (sender, args) =>
        {
            m_PortText.SetText(SerialCommunication.Instance.CurrentPortName());
        };
    }
}