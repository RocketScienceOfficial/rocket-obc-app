using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PanelType { Connect, Main, }

public class PanelsManager : MonoBehaviour
{
    [SerializeField] private GameObject m_ConnectPanel;
    [SerializeField] private GameObject m_MainPanel;

    private void Awake()
    {
        SetPanel(PanelType.Connect);
    }

    private void Start()
    {
        ConnectionController.OnConnected += (sender, args) =>
        {
            SetPanel(PanelType.Main);
        };

        ConnectionController.OnDisconnected += (sender, args) =>
        {
            SetPanel(PanelType.Connect);
        };
    }

    private void SetPanel(PanelType panel)
    {
        m_ConnectPanel.SetActive(false);
        m_MainPanel.SetActive(false);

        switch (panel)
        {
            case PanelType.Connect:
                m_ConnectPanel.SetActive(true);
                break;
            case PanelType.Main:
                m_MainPanel.SetActive(true);
                break;
            default:
                break;
        }
    }
}