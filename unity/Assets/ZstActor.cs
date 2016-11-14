using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Showtime_Zyre;

public class ZstActor : MonoBehaviour {

    public ZstEndpoint endpoint;
    private bool _running;

    private Queue<string> messageQueue;

    // Use this for initialization
    void Start() {
        messageQueue = new Queue<string>();

        endpoint = new ZstEndpoint("unityzyre", (s) => { Debug.Log("INFO: " + s + "\n"); });
        endpoint.incoming += Enqueue; //Debug.Log("INCOMING: " + s);

        _running = true;
        StartCoroutine(Timer());
    }

    private void Enqueue(string s)
    {
        messageQueue.Enqueue("INCOMING: " + s);
    }

    IEnumerator Timer()
    {
        string greeting = "Hello";
        while (_running)
        {
            yield return new WaitForSeconds(2);
            endpoint.Send(greeting);
        }
    }

    public void Update()
    {
        try
        {
            Debug.Log(messageQueue.Dequeue());
        }
        catch (InvalidOperationException e)
        {
            
        }
    }

    public void OnApplicationQuit()
    {
        _running = false;
        endpoint.Close();
    }
}
