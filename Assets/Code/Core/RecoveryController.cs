using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecoveryController : MonoBehaviour
{
    [SerializeField] private Button m_RecoveryButton;
    [SerializeField] private TextMeshProUGUI m_ProgressText;

    private readonly CSVFile _csv = new();
    private readonly KMLFile _kml = new();
    private Watchdog _watchdog;
    private bool _isRecovering;
    private int _currentCount;

    private void Start()
    {
        _watchdog = new Watchdog("RecoveryController", 1f, () => FinishRecovering());

        m_RecoveryButton.onClick.AddListener(() =>
        {
            _isRecovering = true;

            _csv.Open($"Recovery/FlightLog_{DateTime.Now:yyyy-MM-dd--HH-mm-ss}.csv");
            _kml.Open($"Recovery/FlightKML_{DateTime.Now:yyyy-MM-dd--HH-mm-ss}.kml");
            _watchdog.Enable();

            DownloadController.WriteCSVHeader(_csv);
            UpdateProgress();

            PanelsManager.Instance.SetPanelActive(PanelType.Recovery, true);

            SerialCommunication.Instance.SerialPortWrite(new DataLinkFrame
            {
                msgId = DataLinkMessageType.DATALINK_MESSAGE_DATA_REQUEST_RECOVERY,
            });
        });

        SerialCommunication.Instance.OnRead += (sender, args) =>
        {
            if (_isRecovering)
            {
                var msg = args.Frame;
                
                if (msg.msgId == DataLinkMessageType.DATALINK_MESSAGE_DATA_SAVED_CHUNK)
                {
                    DownloadController.ProcessFrame(msg, _csv, _kml, _watchdog);

                    _currentCount++;

                    UpdateProgress();
                }
                else if (msg.msgId == DataLinkMessageType.DATALINK_MESSAGE_DATA_FINISH_RECOVERY)
                {
                    FinishRecovering();
                }
            }
        };

        SerialCommunication.Instance.OnDisconnected += (sender, args) =>
        {
            if (_isRecovering)
            {
                print("Disconnected while recovering data. Aborting...");

                FinishRecovering();
            }
        };
    }

    private void FinishRecovering()
    {
        _csv.Close();
        _kml.Close();
        _watchdog.Disable();

        _isRecovering = false;
        _currentCount = 0;

        PanelsManager.Instance.SetPanelActive(PanelType.Recovery, false);

        print("Finished recovering data!");
    }

    private void UpdateProgress()
    {
        m_ProgressText.SetText($"{_currentCount} frames recovered");
    }
}