using SocketIOClient;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkManager : Singleton<NetworkManager>
{
    public SocketIOUnity socket;

    public string HostId { get; private set; }

    public string AssignedRoleRaw { get; set; } = null;

    private void Start()
    {
        DontDestroyOnLoad(this.gameObject);

        socket = new SocketIOUnity(Constant.GameServerURL);
        
        socket.OnConnected += (sender, e) => {  };

        socket.Connect();
        
        socket.OnDisconnected += (sender, e) => { };
        
    }

    public void SetHostId(string hostId)
    {
        HostId = hostId;
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
