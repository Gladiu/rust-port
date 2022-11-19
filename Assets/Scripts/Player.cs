using UnityEngine;
using System.Collections.Generic;
using System;
using SnapshotState = System.Int32;

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
    private readonly string[] monitoredKeys;
    private readonly Func<string, bool> monitorFunction;
    private float timeout { set; get; }
    private float timer { set; get; }
    private readonly Dictionary<string, int> snapshotKeyIndexMap;
    public InputSnapshot snapshotState { set; get; } // State

    public InputMonitorSystem(Func<string, bool> monitorFunction, float timeout, params string[] monitoredKeysNames)
    {
        this.monitorFunction = monitorFunction;
        this.monitoredKeys = monitoredKeysNames;

        var dict = new Dictionary<string, int>();
        for(int i = 0; i < this.monitoredKeys.Length; i++)
        {
            dict.Add(this.monitoredKeys[i], i);
        }
        this.snapshotKeyIndexMap = dict;
        this.snapshotState = new InputSnapshot(this.snapshotKeyIndexMap);

        this.timeout = timeout;
        this.timer = 0;
    }

    public void CaptureInputSnapshot()
    {
        var currentFrameSnapshotState = new InputSnapshot(this.snapshotKeyIndexMap);
        foreach(string key in this.monitoredKeys)
        {
            if(this.monitorFunction(key))
            {
                currentFrameSnapshotState.SetKeyBitByName(key);
            }
        }
        // OR operation between values of matching keys
        this.snapshotState.UpdateSnapshot(currentFrameSnapshotState);
    }

    public InputSnapshot GetSnapshotState()
    {
        var temp = this.snapshotState;
        this.snapshotState = new InputSnapshot(this.snapshotKeyIndexMap);
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
    private readonly Dictionary<string, int> snapshotKeyIndexesMap; // Map name of a key to index of a bit in snapshotState
    private SnapshotState snapshotState; // int in which each bit is state of one of keys, mapped by snapshotKeyIndexesMap

    public InputSnapshot(Dictionary<string, int> snapshotKeyIndexMap)
    {
        this.snapshotKeyIndexesMap = snapshotKeyIndexMap;
        this.snapshotState = 0;
    }

    public void SetKeyBitByName(string keyName)
    {
        int index = this.snapshotKeyIndexesMap[keyName];
        SetKeyBitByIndex(index);
    }

    public void ClearKeyBitByName(string keyName)
    {
        int index = this.snapshotKeyIndexesMap[keyName];
        ClearKeyBitByIndex(index);
    }

    public int GetBitByName(string keyName)
    {
        int index = this.snapshotKeyIndexesMap[keyName];
        return GetBitByIndex(index);
    }

    private void SetKeyBitByIndex(int idx) => this.snapshotState |= (1 << idx);

    private void ClearKeyBitByIndex(int idx) => this.snapshotState &= ~(1 << idx);

    private int GetBitByIndex(int idx) => (this.snapshotState >> idx) & 1;

    // Perform OR operation
    public void UpdateSnapshot(InputSnapshot snapshot)
    {
        this.snapshotState |= snapshot.snapshotState;
    }

    public void UpdateSnapshot(int snapshotState)
    {
        this.snapshotState |= snapshotState;
    }

    public bool IsSnapshotEmpty() => this.snapshotState == 0;

    public override string ToString()
    {
        string toPrint = "";
        foreach(var kvp in this.snapshotKeyIndexesMap)
        {
            int bit = GetBitByName(kvp.Key);
            toPrint += String.Format("Name: {0}, Value: {1}, Index: {2} \n", kvp.Key, bit, kvp.Value);
        }
        return toPrint;
    }
}

/*
public class InputSnapshotEqComparer : IEqualityComparer<InputSnapshot>
{
    public bool Equals(InputSnapshot s1, InputSnapshot s2)
    {
        foreach(var key in s1.snapshotDict.Keys)
        {
            if(s1.snapshotDict[key] != s2.snapshotDict[key]) return false;
        }
        return true;
    }

    public int GetHashCode(InputSnapshot s)
    {
        // TODO:
        // Change input snapshot to int constructed from sorted bool values
        // Pair it with sorted string names of keys (or not? leave sorted strings in inputmonitor)
        // complexity advantage of 0(1) of hash table is lost if we iterate over keys while inserting
        // Make functions that will abstract bit access
        // In loop we can perform bit op with shift operator (smth like variable << (counter))
        // We can maybe make a dict, that will be used to "key access" bits in the state integer
        // User can give as arg a name of a key that should be modified, and from dict we could get bit index by which we will then modify the value
    }
}

public class ComboKey
{
    private List<InputSnapshot> comboKey;
}
*/