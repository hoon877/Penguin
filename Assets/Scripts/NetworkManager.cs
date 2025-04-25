using SocketIOClient;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkManager : Singleton<NetworkManager>
{
    public SocketIOUnity socket;

    private void Start()
    {
        DontDestroyOnLoad(this.gameObject);

        socket = new SocketIOUnity(Constant.GameServerURL);
        
        socket.OnConnected += (sender, e) => { Debug.Log("connected"); };

        socket.Connect();
        
        socket.OnDisconnected += (sender, e) => { Debug.Log("disconnect: " + e); };
        
    }
    
    private void OnDestroy()
    {
        if (socket != null && socket.Connected)
        {
            socket.Disconnect();
        }
    }


    protected override void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
    }
    
}
