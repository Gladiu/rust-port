using UnityEngine;
using System.Collections.Generic;
using System;

[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour
{
    public float speed = 3.0F;
    public float rotateSpeed = 3.0F;
    public InputMonitorSystem inputMonitorSystem = new InputMonitorSystem(Input.GetButton, 2000,
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
        if(!inputMonitorSystem.currentSnapshotState.IsSnapshotEmpty())
        {
            Debug.Log(inputMonitorSystem.currentSnapshotState);
        }
    }
}

public class InputMonitorSystem
{
    private string[] monitoredKeys { set; get; }
    private Func<string, bool> monitorFunction { set; get; }
    private float monitorWindow { set; get; }
    public InputSnapshot currentSnapshotState { set; get; } // State

    public InputMonitorSystem(Func<string, bool> monitorFunction, float monitorWindow, params string[] monitoredKeysNames)
    {
        this.monitorFunction = monitorFunction;
        this.monitoredKeys = monitoredKeysNames;
        this.monitorWindow = monitorWindow;

        var dict = new Dictionary<string, bool>();
        foreach(string key in monitoredKeysNames)
        {
            dict[key] = false;
        }
        this.currentSnapshotState = new InputSnapshot(dict);
    }

    public void CaptureInputSnapshot()
    {
        foreach(string key in this.monitoredKeys)
        {
            currentSnapshotState.snapshotDict[key] = this.monitorFunction(key);
        }
    }
}

// Simple class wrapping specific dictionary and exposing necessary features
public class InputSnapshot
{
    public Dictionary<string, bool> snapshotDict { set; get; }
    public InputSnapshot(Dictionary<string, bool> snapshotDict)
    {
        this.snapshotDict = snapshotDict;
    }

    public InputSnapshot(InputSnapshot snapshot)
    {
        this.snapshotDict = snapshot.snapshotDict;
    }

    // Perform OR operation on boolean values representing key presses
    public void UpdateSnapshot()
    {

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