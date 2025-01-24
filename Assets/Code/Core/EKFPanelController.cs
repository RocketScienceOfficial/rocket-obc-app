using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EKFPanelController : MonoBehaviour
{
    private const float G = 9.81f;

    [SerializeField] private TextMeshProUGUI m_PositionNorthText;
    [SerializeField] private TextMeshProUGUI m_PositionEastText;
    [SerializeField] private TextMeshProUGUI m_PositionDownText;
    [SerializeField] private TextMeshProUGUI m_DisplacementText;
    [SerializeField] private TextMeshProUGUI m_VelocityNorthText;
    [SerializeField] private TextMeshProUGUI m_VelocityEastText;
    [SerializeField] private TextMeshProUGUI m_VelocityDownText;
    [SerializeField] private TextMeshProUGUI m_SpeedText;
    [SerializeField] private TextMeshProUGUI m_AccelerationNorthText;
    [SerializeField] private TextMeshProUGUI m_AccelerationEastText;
    [SerializeField] private TextMeshProUGUI m_AccelerationDownText;
    [SerializeField] private TextMeshProUGUI m_GForceText;
    [SerializeField] private TextMeshProUGUI m_OrientationRollText;
    [SerializeField] private TextMeshProUGUI m_OrientationPitchText;
    [SerializeField] private TextMeshProUGUI m_OrientationYawText;
    [SerializeField] private Transform m_OrientationRenderObject;
    [SerializeField] private Button m_OpenPanelButton;
    [SerializeField] private Button m_ExitButton;

    private bool _isStreaming;

    private void Start()
    {
        m_OpenPanelButton.onClick.AddListener(() =>
        {
            _isStreaming = true;

            SerialCommunication.Instance.SerialPortWrite(new DataLinkFrame
            {
                msgId = DataLinkMessageType.DATALINK_MESSAGE_OBC_APP_EKF_TRANSMIT_START,
            });

            PanelsManager.Instance.SetPanelActive(PanelType.EKF, true);
        });

        m_ExitButton.onClick.AddListener(() =>
        {
            _isStreaming = false;

            SerialCommunication.Instance.SerialPortWrite(new DataLinkFrame
            {
                msgId = DataLinkMessageType.DATALINK_MESSAGE_OBC_APP_EKF_TRANSMIT_END,
            });

            PanelsManager.Instance.SetPanelActive(PanelType.EKF, false);
        });

        SerialCommunication.Instance.OnRead += (sender, args) =>
        {
            if (_isStreaming)
            {
                var msg = args.Frame;

                if (msg.msgId == DataLinkMessageType.DATALINK_MESSAGE_OBC_APP_EKF_DATA)
                {
                    var payload = BytesConverter.FromBytes<DataLinkFrameOBCAppEKFData>(msg.payload);

                    m_PositionNorthText.SetText($"{Mathf.RoundToInt(payload.positionN)} m");
                    m_PositionEastText.SetText($"{Mathf.RoundToInt(payload.positionE)} m");
                    m_PositionDownText.SetText($"{Mathf.RoundToInt(payload.positionD)} m");
                    m_DisplacementText.SetText($"{Mathf.RoundToInt(Mathf.Sqrt(payload.positionN * payload.positionN + payload.positionE * payload.positionE + payload.positionD * payload.positionD))} m");

                    m_VelocityNorthText.SetText($"{Mathf.RoundToInt(payload.velocityN)} m/s");
                    m_VelocityEastText.SetText($"{Mathf.RoundToInt(payload.velocityE)} m/s");
                    m_VelocityDownText.SetText($"{Mathf.RoundToInt(payload.velocityD)} m/s");
                    m_SpeedText.SetText($"{Mathf.RoundToInt(Mathf.Sqrt(payload.velocityN * payload.velocityN + payload.velocityE * payload.velocityE + payload.velocityD * payload.velocityD) * 3.6f)} km/h");

                    m_AccelerationNorthText.SetText($"{Mathf.RoundToInt(payload.accelerationN)} m/s2");
                    m_AccelerationEastText.SetText($"{Mathf.RoundToInt(payload.accelerationE)} m/s2");
                    m_AccelerationDownText.SetText($"{Mathf.RoundToInt(payload.accelerationD)} m/s2");
                    m_GForceText.SetText($"{string.Format("{0:F1}", Mathf.RoundToInt(Mathf.Sqrt(payload.accelerationN * payload.accelerationN + payload.accelerationE * payload.accelerationE + (payload.accelerationD + G) * (payload.accelerationD + G)) / G * 10.0f) / 10.0f)} G");

                    m_OrientationRenderObject.localRotation = new Quaternion(payload.qx, payload.qy, payload.qz, payload.qw);

                    m_OrientationRollText.SetText($"{Mathf.RoundToInt(m_OrientationRenderObject.eulerAngles.x)}");
                    m_OrientationPitchText.SetText($"{Mathf.RoundToInt(m_OrientationRenderObject.eulerAngles.y)}");
                    m_OrientationYawText.SetText($"{Mathf.RoundToInt(m_OrientationRenderObject.eulerAngles.z)}");
                }
            }
        };

        SerialCommunication.Instance.OnDisconnected += (sender, args) =>
        {
            _isStreaming = false;
        };
    }
}