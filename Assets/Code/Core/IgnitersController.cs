using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IgnitersController : MonoBehaviour
{
    [SerializeField] private Button m_OpenPanelButton;
    [SerializeField] private Button m_ExitButton;
    [SerializeField] private Button[] m_IGNButtons;
    [SerializeField] private GameObject m_TestingPanel;

    private bool _isTestingIgn;
    private Watchdog _watchdog;

    private void Start()
    {
        _watchdog = new Watchdog("IgnitersController", 5f, () => SetTestIGN(false));

        m_OpenPanelButton.onClick.AddListener(() =>
        {
            m_TestingPanel.SetActive(false);

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
                var msg = args.Frame;

                if (msg.msgId == DataLinkMessageType.DATALINK_MESSAGE_IGN_FINISH_TEST)
                {
                    SetTestIGN(false);
                }
            }
        };

        SerialCommunication.Instance.OnDisconnected += (sender, args) =>
        {
            if (_isTestingIgn)
            {
                print("Disconnected while testing igniter. Aborting...");

                SetTestIGN(false);

                PanelsManager.Instance.SetPanelActive(PanelType.Igniters, false);
            }
        };

        m_TestingPanel.SetActive(false);
    }

    private void TestIgniter(int i)
    {
        SetTestIGN(true);

        SerialCommunication.Instance.SerialPortWrite(new DataLinkFrame
        {
            msgId = DataLinkMessageType.DATALINK_MESSAGE_IGN_REQUEST_TEST,
            payload = BytesConverter.GetBytes(new DataLinkFrameIGNRequestTest
            {
                ignNum = (byte)i,
            }),
        });
    }

    private void SetTestIGN(bool en)
    {
        m_TestingPanel.SetActive(en);

        _isTestingIgn = en;

        if (en)
        {
            _watchdog.Enable();
        }
        else
        {
            _watchdog.Disable();
        }
    }
}