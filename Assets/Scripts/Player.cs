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
    public ComboSequence combo = new ComboSequence();

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
        {   // TODO: Rename this GetSnapshotState() method, name collides with InputSnapshot's method and is confusing
            InputSnapshot s = inputMonitorSystem.GetSnapshotState();
            inputMonitorSystem.ResetSnapshotState();
            if (s.IsSnapshotEmpty()) // Equivalent with end of the combo
            {
                Debug.Log(combo);
                combo.ClearComboSequence();
            }
            else
            {
                combo.AddInputSnapshot(s);
            }
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
    private readonly InputSnapshot snapshotPrototype;
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

        this.snapshotPrototype = new InputSnapshot(this.snapshotKeyIndexMap);
        this.snapshotState = this.snapshotPrototype.ShallowCopy();

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
    /// </summary>
    /// <returns>Object of type <see cref="InputSnapshot"/> containing key presses from the last time window.</returns>
    public InputSnapshot GetSnapshotState() => this.snapshotState;

    public void ResetSnapshotState()
    {
        // Uses Prototype Design pattern, now this method is single responsibility
        // is not coupled with constructor of the InputSnapshot and we are sure
        // that we get exact same object
        this.snapshotState = this.snapshotPrototype.ShallowCopy();
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

    public void ClearState() => this.snapshotState = 0;

    /// <summary>
    /// Update this <see cref="InputSnapshot"/> object's state with state values from argument. Values are updated with OR operation.
    /// </summary>
    /// <param name="snapshot"></param> - <see cref="InputSnapshot"/> object to update state from.
    public void UpdateSnapshot(InputSnapshot snapshot)
    {
        this.snapshotState |= snapshot.snapshotState;
    }

    /// <summary>
    /// Function to determine if the the snapshot is empty (no key was pressed = no state bit is set).
    /// </summary>
    /// <returns> true when snapshot is empty and false when it is not. </returns>
    public bool IsSnapshotEmpty() => this.snapshotState == 0;

    public static bool IsSnapshotEmpty(SnapshotState state) => state == 0;

    /// <summary>
    /// Returns a <see cref="InputSnapshot.snapshotState"/>.
    /// </summary>
    /// <returns> Returns <see cref="InputSnapshot.snapshotState"/></returns>
    public int GetSnapshotState() => this.snapshotState;

    public InputSnapshot ShallowCopy()
    {
        return (InputSnapshot)this.MemberwiseClone();
    }

    /// <summary>
    /// Override of <see cref="ToString"/> function. Gives clear representation of the <see cref="InputSnapshot"/> object.
    /// </summary>
    /// <returns> String representation of the object. </returns>
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

public class InputSnapshotEqualityComparer : IEqualityComparer<InputSnapshot>
{
    public bool Equals(InputSnapshot s1, InputSnapshot s2)
    {
        if(s1.GetSnapshotState() == s2.GetSnapshotState()) return true;
        return false;
    }

    public int GetHashCode(InputSnapshot s)
    {
        // snapshotState is unique int value for all combinations of keys, so it is enough to do that
        return s.GetSnapshotState().GetHashCode();
    }
}

public class ComboSequence
{
    public List<InputSnapshot> comboSequence { set; get; }

    public ComboSequence()
    {
        this.comboSequence = new List<InputSnapshot>();
    }

    public void AddInputSnapshot(InputSnapshot snapshot) => this.comboSequence.Add(snapshot);

    public void ClearComboSequence() => this.comboSequence.Clear();

    public override string ToString()
    {
        string comboSequenceString = "Combo sequence: \n";
        for (int i = 0; i < this.comboSequence.Count; i++)
        {
            comboSequenceString += String.Format("{0}", this.comboSequence[i].GetSnapshotState());
            if(i != this.comboSequence.Count - 1) comboSequenceString += "->";
        }
        return comboSequenceString;
    }
}

public class ComboSequenceEqualityComparer : IEqualityComparer<ComboSequence>
{
    public bool Equals(ComboSequence s1, ComboSequence s2)
    {
        if(s1.comboSequence.Count != s2.comboSequence.Count) return false;
        if(s1.comboSequence.Count == 0 && s2.comboSequence.Count == 0) return true;
        for (int i = 0; i < s1.comboSequence.Count; i++)
        {
            if(s1.comboSequence[i] != s2.comboSequence[i]) return false;
        }
        return true;
    }

    public int GetHashCode(ComboSequence s)
    {
        int toHash = 0;
        for (int i = 0; i < s.comboSequence.Count; i++)
        {
            toHash ^= s.comboSequence[i].GetSnapshotState();
        }
        return toHash.GetHashCode();
    }
}