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

            _csv.Open($"Recovery/FlightLog_{DateTime.Now:yyyy-dd-MM--HH-mm-ss}.csv");
            _kml.Open($"Recovery/FlightKML_{DateTime.Now:yyyy-dd-MM--HH-mm-ss}.kml");
            _watchdog.Enable();

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
                    var payload = BytesConverter.FromBytes<DataLinkFrameDataSavedChunk>(msg.payload);

                    _csv.WriteFileValue(payload.dt);
                    _csv.WriteFileValue(payload.accX);
                    _csv.WriteFileValue(payload.accY);
                    _csv.WriteFileValue(payload.accZ);
                    _csv.WriteFileValue(payload.velN);
                    _csv.WriteFileValue(payload.velE);
                    _csv.WriteFileValue(payload.velD);
                    _csv.WriteFileValue(payload.posN);
                    _csv.WriteFileValue(payload.posE);
                    _csv.WriteFileValue(payload.posD);
                    _csv.WriteFileValue(payload.qw);
                    _csv.WriteFileValue(payload.qx);
                    _csv.WriteFileValue(payload.qy);
                    _csv.WriteFileValue(payload.qz);
                    _csv.WriteFileValue(payload.lat);
                    _csv.WriteFileValue(payload.lon);
                    _csv.WriteFileValue(payload.alt);
                    _csv.WriteFileValue(payload.smState);
                    _csv.WriteFileValue(payload.batVolts10 / 10.0f);

                    for (int i = 0; i < 8; i++)
                    {
                        var flag = (payload.ignFlags & (1 << i)) >> i;

                        _csv.WriteFileValue(flag);
                    }

                    _csv.WriteFileValue(payload.gpsData & 0x01);
                    _csv.WriteFileValue(payload.gpsData >> 1);

                    _csv.EndLine();

                    _kml.AddRecord(payload.lat, payload.lon, (float)payload.alt);

                    _watchdog.Update();

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