using System.Collections;
using System.Collections.Generic;
using SocketIOClient;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private SocketIOUnity socket;

    private void Start()
    {
        socket = new SocketIOUnity(Constant.GameServerURL);
        
        socket.OnConnected += (sender, e) => { Debug.Log("connected"); };

        socket.Connect();
        
        socket.OnDisconnected += (sender, e) => { Debug.Log("disconnect: " + e); };
        
    }
    
    private void OnDestroy()
    {
        socket.Disconnect();
    }
}
