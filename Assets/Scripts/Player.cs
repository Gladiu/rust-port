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

/// <summary>
/// Class for registering key presses during specified time window.
/// </summary>
public class InputMonitorSystem
{
    private readonly string[] monitoredKeys;
    private readonly Func<string, bool> monitorFunction;
    private float timeout { set; get; }
    private float timer { set; get; }
    private readonly Dictionary<string, int> snapshotKeyIndexMap;
    public InputSnapshot snapshotState { set; get; } // State

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="monitorFunction"></param> - Unity function that will be used to get the state of the key (e.g. InputGetButton).
    /// <param name="timeout"></param> - time window in which input will be registered. During this time window, all input will be treated as a one state snapshot.
    /// <param name="monitoredKeysNames"></param> - Array of key names to be monitored.
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

    /// <summary>
    /// Method that will use <see cref="InputMonitorSystem.monitorFunction"/> combined with <see cref="InputMonitorSystem.monitoredKeys"/>
    /// to record key presses state in the current frame. This method will modify <see cref="InputMonitorSystem.snapshotState"/> with captured
    /// keys state in the current frame. Values will be OR'ed.
    /// </summary>
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

    /// <summary>
    /// Method that will return <see cref="InputSnapshot"/> object containing recorded key presses from the last time window.
    /// It will also reset <see cref="InputMonitorSystem.snapshotState"/>.
    /// </summary>
    /// <returns>Object of type <see cref="InputSnapshot"/> containing key presses from the last time window.</returns>
    public InputSnapshot GetSnapshotState()
    {
        var temp = this.snapshotState;
        this.snapshotState = new InputSnapshot(this.snapshotKeyIndexMap);
        return temp;
    }

    /// <summary>
    /// Check if timer for current time window is running.
    /// </summary>
    /// <returns> true if timer is running and false if it is not. </returns>
    public bool TimerOn() => this.timer > 0;

    /// <summary>
    /// Check if timer times out.
    /// </summary>
    /// <returns> true if timer timed out or false if it did not. </returns>
    public bool TimerTimeout() => this.timer > this.timeout;

    /// <summary>
    /// Update the timer with current delta time of a frame, use <see cref="Input.GetButton(string)"/> or similar to provide delta time.
    /// </summary>
    /// <param name="dt"></param> - delta time in seconds.
    public void TimerUpdate(float dt)  => this.timer += dt;

    /// <summary>
    /// Reset <see cref="InputSnapshot.timer"/> to 0. Call it after /// <see cref="InputSnapshot.TimerTimeout"/> returns true.
    /// </summary>
    public void TimerReset() => this.timer = 0;
}

/// <summary>
/// Class mostly for storing the pressed keys input state. <see cref="InputSnapshot.snapshotState"/> has a type <see cref="SnapshotState"/> that is just
/// an alias for int. Each key state is stored as a bit inside that int (1 for key pressed and 0 for not pressed). In
/// <see cref="InputSnapshot.snapshotKeyIndexesMap"/> is stored mapping, from keys names to indexes of corresponding
/// state bits in <see cref="InputSnapshot.snapshotState"/>. This way, we can quickly and efficiently manipulate bits but also use efficient
/// "hash map like" access.
/// </summary>
public class InputSnapshot
{
    private readonly Dictionary<string, int> snapshotKeyIndexesMap; // Map name of a key to index of a bit in snapshotState
    private SnapshotState snapshotState; // int in which each bit is state of one of keys, mapped by snapshotKeyIndexesMap

    /// <summary>
    /// Constructor for <see cref="InputSnapshot"/> class. Initializes state to 0 (no key presses) and loads mapping from string key names to state bit indexes.
    /// </summary>
    /// <param name="snapshotKeyIndexMap"></param> - mapping from string key name to bit index in <see cref="InputSnapshot.snapshotState"/>.
    public InputSnapshot(Dictionary<string, int> snapshotKeyIndexMap)
    {
        this.snapshotKeyIndexesMap = snapshotKeyIndexMap;
        this.snapshotState = 0;
    }

    /// <summary>
    /// Sets state bit for specified key name using <see cref="InputSnapshot.snapshotKeyIndexesMap"/>.
    /// </summary>
    /// <param name="keyName"></param> - key to <see cref="InputSnapshot.snapshotKeyIndexesMap"/> for which state bit will be set.
    public void SetKeyBitByName(string keyName)
    {
        int index = this.snapshotKeyIndexesMap[keyName];
        SetKeyBitByIndex(index);
    }

    /// <summary>
    /// Clears state bit for specified key name using <see cref="InputSnapshot.snapshotKeyIndexesMap"/>.
    /// </summary>
    /// <param name="keyName"></param> - key to <see cref="InputSnapshot.snapshotKeyIndexesMap"/> for which state bit will be cleared.
    public void ClearKeyBitByName(string keyName)
    {
        int index = this.snapshotKeyIndexesMap[keyName];
        ClearKeyBitByIndex(index);
    }

    /// <summary>
    /// Gets state bit for specified key name using <see cref="InputSnapshot.snapshotKeyIndexesMap"/>.
    /// </summary>
    /// <param name="keyName"></param> - key to <see cref="InputSnapshot.snapshotKeyIndexesMap"/> for which state will be returned.
    public int GetBitByName(string keyName)
    {
        int index = this.snapshotKeyIndexesMap[keyName];
        return GetBitByIndex(index);
    }

    /// <summary>
    /// Sets a state bit given the index of it.
    /// </summary>
    /// <param name="idx"></param> - index of bit to set.
    private void SetKeyBitByIndex(int idx) => this.snapshotState |= (1 << idx);

    /// <summary>
    /// Clears a state bit given the index of it.
    /// </summary>
    /// <param name="idx"></param> - index of bit to set.
    private void ClearKeyBitByIndex(int idx) => this.snapshotState &= ~(1 << idx);

    /// <summary>
    /// Returns a state bit given the index of it.
    /// </summary>
    /// <param name="idx"></param> - index of a state bit to return.
    /// <returns></returns> - state bit value 0 or 1.
    private int GetBitByIndex(int idx) => (this.snapshotState >> idx) & 1;

    /// <summary>
    /// Update this <see cref="InputSnapshot"/> object's state with state values from argument. Values are updated with OR operation.
    /// </summary>
    /// <param name="snapshot"></param> - <see cref="InputSnapshot"/> object to update state from.
    public void UpdateSnapshot(InputSnapshot snapshot)
    {
        this.snapshotState |= snapshot.snapshotState;
    }

    public void UpdateSnapshot(int snapshotState)
    {
        this.snapshotState |= snapshotState;
    }

    /// <summary>
    /// Function to determine if the the snapshot is empty (no key was pressed = no state bit is set).
    /// </summary>
    /// <returns> true when snapshot is empty and false when it is not. </returns>
    public bool IsSnapshotEmpty() => this.snapshotState == 0;

    /// <summary>
    /// Override of <see cref="ToString"/> function. Gives clear representation of the <see cref="InputSnapshot"/> object.
    /// </summary>
    /// <returns></returns>
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