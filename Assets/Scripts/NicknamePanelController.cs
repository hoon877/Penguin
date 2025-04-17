using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NicknamePanelController : MonoBehaviour
{
    [SerializeField] private TMP_InputField nicknameInputField;
    
    public void OnClickNicknameButton()
    {
        if (nicknameInputField.text == "")
        {
            NetworkManager.Instance.socket.Emit("LoginCheck", "user");
        }
        else
        {
            NetworkManager.Instance.socket.Emit("LoginCheck", nicknameInputField.text);
        }
    }
}
