using SocketIOClient;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkManager : Singleton<NetworkManager>
{
    public SocketIOUnity socket;
    protected override void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {

    }

    private void Start()
    {
        ConnectToServer();
    }
    private void ConnectToServer()
    {
        // ������ �����ϴ� ������ ���⿡ �ۼ��մϴ�.
        socket = new SocketIOUnity(Constant.ServerURL, new SocketIOOptions
        {
            Query = new Dictionary<string, string>
            {
                { "token", "UNITY" },
                { "version", "0.1" }
            },
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
        });

        socket.Connect();

        Debug.Log("������ ����Ǿ����ϴ�.");
    }

    private void OnDestroy()
    {
        // ���� ������ �����մϴ�.
        if (socket != null)
        {
            socket.Disconnect();
            socket.Dispose();
            socket = null;
            Debug.Log("�������� ������ ����Ǿ����ϴ�.");
        }
    }
}
