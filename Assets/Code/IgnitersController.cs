using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IgnitersController : MonoBehaviour
{
    [SerializeField] private Button m_OpenPanelButton;
    [SerializeField] private Button m_ExitButton;
    [SerializeField] private Button[] m_IGNButtons;
    [SerializeField] private GameObject m_LoadingPanel;

    private bool _isTestingIgn;

    private void Start()
    {
        m_OpenPanelButton.onClick.AddListener(() =>
        {
            m_LoadingPanel.SetActive(false);

            PanelsManager.Instance.SetPanelActive(PanelType.Igniters, true);
        });

        m_ExitButton.onClick.AddListener(() =>
        {
            PanelsManager.Instance.SetPanelActive(PanelType.Igniters, false);
        });

        for (var i = 0; i < m_IGNButtons.Length; i++)
        {
            var index = i;

            m_IGNButtons[i].onClick.AddListener(() =>
            {
                TestIgniter(index);
            });
        }

        SerialCommunication.Instance.OnRead += (sender, args) =>
        {
            if (_isTestingIgn)
            {
                var msg = args.Data;

                if (msg.StartsWith("\\"))
                {
                    var cmd = msg[1..];

                    if (cmd == "ign-test-finish")
                    {
                        _isTestingIgn = false;

                        m_LoadingPanel.SetActive(false);
                    }
                }
            }
        };

        SerialCommunication.Instance.OnDisconnected += (sender, args) =>
        {
            if (_isTestingIgn)
            {
                print("Disconnected while testing igniter. Aborting...");

                _isTestingIgn = false;

                m_LoadingPanel.SetActive(false);

                PanelsManager.Instance.SetPanelActive(PanelType.Igniters, false);
            }
        };

        m_LoadingPanel.SetActive(false);
    }

    private void TestIgniter(int i)
    {
        _isTestingIgn = true;

        m_LoadingPanel.SetActive(true);

        SerialCommunication.Instance.SerialPortWrite($"\\ign-test {i}");
    }
}