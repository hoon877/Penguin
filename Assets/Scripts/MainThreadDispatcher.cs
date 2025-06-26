using System;
using System.Collections.Generic;
using UnityEngine;

//서버 명령어 함수 내에서 클라이언트 코드 처리를 위한 스레드 디스패처 클래스   
public class MainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> executionQueue = new();

    public static void Enqueue(Action action)
    {
        lock (executionQueue)
        {
            executionQueue.Enqueue(action);
        }
    }

    private void Update()
    {
        lock (executionQueue)
        {
            while (executionQueue.Count > 0)
            {
                var action = executionQueue.Dequeue();
                action?.Invoke();
            }
        }
    }
}