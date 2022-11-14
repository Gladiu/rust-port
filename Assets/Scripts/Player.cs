using UnityEngine;
using System.Collections.Generic;
using System;

[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour
{
    public float speed = 3.0F;
    public float rotateSpeed = 3.0F;
    public InputMonitorSystem inputMonitorSystem = new InputMonitorSystem(Input.GetButton, 2,
        "ComboUp",
        "ComboDown",
        "ComboLeft",
        "ComboRight"
    );

    void Update()
    {
        CharacterController controller = GetComponent<CharacterController>();

        // Rotate around y - axis
        transform.Rotate(0, Input.GetAxis("Horizontal") * rotateSpeed, 0);

        // Move forward / backward
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        float curSpeed = speed * Input.GetAxis("Vertical");
        controller.SimpleMove(forward * curSpeed);

        inputMonitorSystem.CaptureInputSnapshot();

        if(inputMonitorSystem.TimerTimeout())
        {
            var s = inputMonitorSystem.GetSnapshotState();
            Debug.Log(s);
            inputMonitorSystem.TimerReset();
        }
        inputMonitorSystem.TimerUpdate(Time.deltaTime);
    }
}

public class InputMonitorSystem
{
    private string[] monitoredKeys { set; get; }
    private Func<string, bool> monitorFunction { set; get; }
    private float timeout { set; get; }
    private float timer { set; get; }
    public InputSnapshot snapshotState { set; get; } // State

    public InputMonitorSystem(Func<string, bool> monitorFunction, float timeout, params string[] monitoredKeysNames)
    {
        this.monitorFunction = monitorFunction;
        this.monitoredKeys = monitoredKeysNames;
        this.timeout = timeout;
        this.timer = 0;

        var dict = new Dictionary<string, bool>();
        foreach(string key in monitoredKeysNames)
        {
            dict[key] = false;
        }
        this.snapshotState = new InputSnapshot(dict);
    }

    public void CaptureInputSnapshot()
    {
        var currentSnapshotState = new InputSnapshot(new Dictionary<string, bool>());
        foreach(string key in this.monitoredKeys)
        {
            currentSnapshotState.snapshotDict[key] = this.monitorFunction(key);
        }
        // OR operation between values of matching keys
        this.snapshotState.UpdateSnapshot(currentSnapshotState);
    }

    public InputSnapshot GetSnapshotState()
    {
        var temp = this.snapshotState;
        this.snapshotState = new InputSnapshot(this.monitoredKeys);
        return temp;
    }

    public bool TimerOn() => this.timer > 0;

    public bool TimerTimeout() => this.timer > this.timeout;

    public void TimerUpdate(float dt)  => this.timer += dt;

    public void TimerReset() => this.timer = 0;
}

// Simple class wrapping specific dictionary and exposing necessary features
public class InputSnapshot
{
    public Dictionary<string, bool> snapshotDict { set; get; }
    public InputSnapshot(Dictionary<string, bool> snapshotDict)
    {
        this.snapshotDict = snapshotDict;
    }

    public InputSnapshot(string[] snapshotKeys)
    {
        this.snapshotDict = new Dictionary<string, bool>();
        foreach(var key in snapshotKeys)
        {
            snapshotDict.Add(key, false);
        }
    }

    public InputSnapshot(InputSnapshot snapshot)
    {
        this.snapshotDict = snapshot.snapshotDict;
    }

    // Perform OR operation on boolean values representing key presses
    public void UpdateSnapshot(InputSnapshot snapshot)
    {
        // Iterate over two dicts that should have the same keys
        var keys = new List<string>(this.snapshotDict.Keys);
        foreach(var k in keys)
        {
            this.snapshotDict[k] = (snapshot.snapshotDict[k] | this.snapshotDict[k]);
            // Do not recover from key error, critical error
        }
    }

    public bool IsSnapshotEmpty() => !this.snapshotDict.ContainsValue(true);

    public override string ToString()
    {
        string toPrint = "";
        foreach(var kvp in this.snapshotDict)
        {
            toPrint += String.Format("{0} - {1}\n", kvp.Key, kvp.Value);
        }
        return toPrint;
    }
}