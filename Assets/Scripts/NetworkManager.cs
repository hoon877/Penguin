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
        // 서버에 연결하는 로직을 여기에 작성합니다.
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

        Debug.Log("서버에 연결되었습니다.");
    }

    private void OnDestroy()
    {
        // 소켓 연결을 종료합니다.
        if (socket != null)
        {
            socket.Disconnect();
            socket.Dispose();
            socket = null;
            Debug.Log("서버와의 연결이 종료되었습니다.");
        }
    }
}
