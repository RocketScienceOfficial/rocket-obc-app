using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DownloadController : MonoBehaviour
{
    [SerializeField] private Button m_DownloadButton;
    [SerializeField] private Image m_ProgressFill;
    [SerializeField] private TextMeshProUGUI m_ProgressText;

    private readonly CSVFile _file = new();
    private readonly KMLFile _kml = new();
    private Watchdog _watchdog;
    private bool _isDownloading;
    private int _currentCount;
    private int _totalCount;

    private void Start()
    {
        _watchdog = new Watchdog("DownloadController", 1f, () => FinishDownloading());

        m_DownloadButton.onClick.AddListener(() =>
        {
            _isDownloading = true;

            _file.Open($"Downloads/FlightLog_{DateTime.Now:yyyy-dd-MM--HH-mm-ss}.csv");
            _kml.Open($"Downloads/FlightKML_{DateTime.Now:yyyy-dd-MM--HH-mm-ss}.kml");
            _watchdog.Enable();

            UpdateProgress();

            PanelsManager.Instance.SetPanelActive(PanelType.Download, true);

            SerialCommunication.Instance.SerialPortWrite(new DataLinkFrame
            {
                msgId = DataLinkMessageType.DATALINK_MESSAGE_DATA_REQUEST_READ,
            });
        });

        SerialCommunication.Instance.OnRead += (sender, args) =>
        {
            if (_isDownloading)
            {
                var msg = args.Frame;

                if (msg.msgId == DataLinkMessageType.DATALINK_MESSAGE_DATA_SAVED_CHUNK)
                {
                    var payload = BytesConverter.FromBytes<DataLinkFrameDataSavedChunk>(msg.payload);

                    _file.WriteFileValue(payload.dt);
                    _file.WriteFileValue(payload.accX);
                    _file.WriteFileValue(payload.accY);
                    _file.WriteFileValue(payload.accZ);
                    _file.WriteFileValue(payload.velN);
                    _file.WriteFileValue(payload.velE);
                    _file.WriteFileValue(payload.velD);
                    _file.WriteFileValue(payload.posN);
                    _file.WriteFileValue(payload.posE);
                    _file.WriteFileValue(payload.posD);
                    _file.WriteFileValue(payload.qw);
                    _file.WriteFileValue(payload.qx);
                    _file.WriteFileValue(payload.qy);
                    _file.WriteFileValue(payload.qz);
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

                    _watchdog.Update();

                    _currentCount++;

                    UpdateProgress();

                    if (_totalCount == _currentCount)
                    {
                        print("Received all frames!");
                    }
                }
                else if (msg.msgId == DataLinkMessageType.DATALINK_MESSAGE_DATA_SAVED_SIZE)
                {
                    var payload = BytesConverter.FromBytes<DataLinkFrameDataSavedSize>(msg.payload);

                    _totalCount = (int)payload.size;
                    _watchdog.Update();

                    print("New total count is: " + _totalCount);

                    if (_totalCount == 0)
                    {
                        print("Received count is 0. Aborting...");

                        FinishDownloading();
                    }
                    else
                    {
                        UpdateProgress();
                    }
                }
                else if (msg.msgId == DataLinkMessageType.DATALINK_MESSAGE_DATA_FINISH_READ)
                {
                    FinishDownloading();
                }
            }
        };

        SerialCommunication.Instance.OnDisconnected += (sender, args) =>
        {
            if (_isDownloading)
            {
                print("Disconnected while downloading. Aborting...");

                FinishDownloading();
            }
        };
    }

    private void FinishDownloading()
    {
        _file.Close();
        _kml.Close();
        _watchdog.Disable();

        _isDownloading = false;
        _currentCount = 0;
        _totalCount = 0;

        PanelsManager.Instance.SetPanelActive(PanelType.Download, false);

        print("Finished downloading data!");
    }

    private void UpdateProgress()
    {
        m_ProgressFill.fillAmount = _isDownloading ? (float)_currentCount / Mathf.Max(_totalCount, 1) : 0;
        m_ProgressText.SetText(_totalCount != 0 ? $"{_currentCount} / {_totalCount}" : "Loading...");
    }
}