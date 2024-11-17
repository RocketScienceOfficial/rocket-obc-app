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

    private readonly CSVFile _file = new();
    private readonly KMLFile _kml = new();
    private bool _isRecovering;
    private int _currentCount;

    private void Start()
    {
        m_RecoveryButton.onClick.AddListener(() =>
        {
            _isRecovering = true;

            _file.Open($"Recovery/FlightLog_{DateTime.Now:yyyy-dd-MM--HH-mm-ss}.csv");
            _kml.Open($"Recovery/FlightKML_{DateTime.Now:yyyy-dd-MM--HH-mm-ss}.kml");

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

                    _file.WriteFileValue(payload.time);
                    _file.WriteFileValue(payload.acc1x);
                    _file.WriteFileValue(payload.acc1y);
                    _file.WriteFileValue(payload.acc1z);
                    _file.WriteFileValue(payload.acc2x);
                    _file.WriteFileValue(payload.acc2y);
                    _file.WriteFileValue(payload.acc2z);
                    _file.WriteFileValue(payload.acc3x);
                    _file.WriteFileValue(payload.acc3y);
                    _file.WriteFileValue(payload.acc3z);
                    _file.WriteFileValue(payload.gyro1x);
                    _file.WriteFileValue(payload.gyro1y);
                    _file.WriteFileValue(payload.gyro1z);
                    _file.WriteFileValue(payload.gyro2x);
                    _file.WriteFileValue(payload.gyro2y);
                    _file.WriteFileValue(payload.gyro2z);
                    _file.WriteFileValue(payload.mag1x);
                    _file.WriteFileValue(payload.mag1y);
                    _file.WriteFileValue(payload.mag1z);
                    _file.WriteFileValue(payload.press);
                    _file.WriteFileValue(payload.kalmanHeight);
                    _file.WriteFileValue(payload.lat);
                    _file.WriteFileValue(payload.lon);
                    _file.WriteFileValue(payload.alt);
                    _file.WriteFileValue(payload.smState);
                    _file.WriteFileValue(payload.batVolts10 / 10.0f);

                    for (int i = 0; i < 8; i++)
                    {
                        var flag = (payload.ignFlags & (1 << i)) >> i;

                        _file.WriteFileValue(flag);
                    }

                    _file.WriteFileValue(payload.gpsData & 0x01);
                    _file.WriteFileValue(payload.gpsData >> 1);

                    _file.EndLine();

                    _kml.AddRecord(payload.lat, payload.lon, (float)payload.alt);

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
        _file.Close();
        _kml.Close();

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