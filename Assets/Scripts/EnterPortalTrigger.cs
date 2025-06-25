using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnterPortalTrigger : MonoBehaviour
{
    [Header("이동 대상 좌표")]
    [SerializeField] private Vector3 targetPosition;

    [Header("충돌 대상 레이어 (이름)")]
    [SerializeField] private string playerLayerName = "Player";

    private int playerLayer;

    private void Start()
    {
        playerLayer = LayerMask.NameToLayer(playerLayerName);
        if (playerLayer == -1)
        {
            Debug.LogError($" Layer \"{playerLayerName}\" is not defined in Project Settings.");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == playerLayer)
        {
            other.transform.position = targetPosition;
            FollowCamera cam = Camera.main.GetComponent<FollowCamera>();
            if (cam != null)
            {
                cam.SetTarget(other.transform);
                cam.SnapToTarget(); 
            }
        }
    }
}
