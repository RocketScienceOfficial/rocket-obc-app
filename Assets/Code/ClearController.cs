using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ClearController : MonoBehaviour
{
    [SerializeField] private Button m_ClearButton;

    private bool _isClearing;

    private void Start()
    {
        m_ClearButton.onClick.AddListener(() =>
        {
            PanelsManager.Instance.SetPanelActive(PanelType.Clear, true);

            _isClearing = true;

            SerialCommunication.Instance.SerialPortWrite("data-clear-start");
        });

        SerialCommunication.Instance.OnRead += (sender, args) =>
        {
            if (_isClearing && !string.IsNullOrEmpty(args.Data) && args.Data.StartsWith("\\"))
            {
                var cmd = args.Data[1..];

                if (cmd == "data-clear-finish")
                {
                    FinishClearing();
                }
            }
        };

        SerialCommunication.Instance.OnDisconnected += (sender, args) =>
        {
            if (_isClearing)
            {
                print("Disconnected while clearing. Aborting...");

                FinishClearing();
            }
        };
    }

    private void FinishClearing()
    {
        _isClearing = false;

        PanelsManager.Instance.SetPanelActive(PanelType.Clear, false);
    }
}