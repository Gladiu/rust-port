using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using SnapshotState = System.Int32;

[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour
{
    public float speed = 3.0F;
    public float rotateSpeed = 3.0F;
    public InputMonitorSystem inputMonitorSystem;
    public ComboSequence combo;

    void Start()
    {
        inputMonitorSystem = new InputMonitorSystem(Input.GetButton, 2,
            "ComboUp",
            "ComboDown",
            "ComboLeft",
            "ComboRight"
        );
        combo = new ComboSequence();
    }

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
    private readonly string[] _monitoredKeys;
    public string[] MonitoredKeys { get { return _monitoredKeys; } }

    private readonly Func<string, bool> _monitorFunction;
    public Func<string, bool> MonitorFunction { get { return _monitorFunction; } }

    public float Timeout { set; get; }
    public float Timer { private set; get; }

    private readonly Dictionary<string, int> _snapshotKeyIndexMap;
    public Dictionary<string, int> SnapshotKeyIndexMap { get { return _snapshotKeyIndexMap; } }

    private readonly InputSnapshot snapshotPrototype; // Leave it as priv readonly, only accessed inside obj
    public InputSnapshot SnapshotState { private set; get; } // State


    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="monitorFunction"></param> - Unity function that will be used to get the state of the key (e.g. InputGetButton).
    /// <param name="timeout"></param> - time window in which input will be registered. During this time window, all input will be treated as a one state snapshot.
    /// <param name="monitoredKeysNames"></param> - Array of key names to be monitored.
    public InputMonitorSystem(Func<string, bool> monitorFunction, float timeout, params string[] monitoredKeysNames)
    {
        this._monitorFunction = monitorFunction;
        this._monitoredKeys = monitoredKeysNames;

        var dict = new Dictionary<string, int>();
        for(int i = 0; i < this.MonitoredKeys.Length; i++)
        {
            dict.Add(this.MonitoredKeys[i], i);
        }
        this._snapshotKeyIndexMap = dict;

        this.snapshotPrototype = new InputSnapshot(this.SnapshotKeyIndexMap);
        this.SnapshotState = this.snapshotPrototype.ShallowCopy();

        this.Timeout = timeout;
        this.Timer = 0;
    }

    /// <summary>
    /// Method that will use <see cref="InputMonitorSystem.monitorFunction"/> combined with <see cref="InputMonitorSystem.monitoredKeys"/>
    /// to record key presses state in the current frame. This method will modify <see cref="InputMonitorSystem.SnapshotState"/> with captured
    /// keys state in the current frame. Values will be OR'ed.
    /// </summary>
    public void CaptureInputSnapshot()
    {
        var currentFrameSnapshotState = new InputSnapshot(this.SnapshotKeyIndexMap);
        foreach(string key in this.MonitoredKeys)
        {
            if(this.MonitorFunction(key))
            {
                currentFrameSnapshotState.SetKeyBitByName(key);
            }
        }
        // OR operation between values of matching keys
        this.SnapshotState.UpdateSnapshot(currentFrameSnapshotState);
    }

    /// <summary>
    /// Method that will return <see cref="InputSnapshot"/> object containing recorded key presses from the last time window.
    /// </summary>
    /// <returns>Object of type <see cref="InputSnapshot"/> containing key presses from the last time window.</returns>
    public InputSnapshot GetSnapshotState() => this.SnapshotState;

    public void ResetSnapshotState()
    {
        // Uses Prototype Design pattern, now this method is single responsibility
        // is not coupled with constructor of the InputSnapshot and we are sure
        // that we get exact same object
        this.SnapshotState = this.snapshotPrototype.ShallowCopy();
    }

    /// <summary>
    /// Check if timer for current time window is running.
    /// </summary>
    /// <returns> true if timer is running and false if it is not. </returns>
    public bool TimerOn() => this.Timer > 0;

    /// <summary>
    /// Check if timer times out.
    /// </summary>
    /// <returns> true if timer timed out or false if it did not. </returns>
    public bool TimerTimeout() => this.Timer > this.Timeout;

    /// <summary>
    /// Update the timer with current delta time of a frame, use <see cref="Input.GetButton(string)"/> or similar to provide delta time.
    /// </summary>
    /// <param name="dt"></param> - delta time in seconds.
    public void TimerUpdate(float dt)  => this.Timer += dt;

    /// <summary>
    /// Reset <see cref="InputSnapshot.timer"/> to 0. Call it after /// <see cref="InputSnapshot.TimerTimeout"/> returns true.
    /// </summary>
    public void TimerReset() => this.Timer = 0;
}

/// <summary>
/// Class mostly for storing the pressed keys input state. <see cref="InputSnapshot.SnapshotState"/> has a type <see cref="int"/> that is just
/// an alias for int. Each key state is stored as a bit inside that int (1 for key pressed and 0 for not pressed). In
/// <see cref="InputSnapshot.snapshotKeyIndexesMap"/> is stored mapping, from keys names to indexes of corresponding
/// state bits in <see cref="InputSnapshot.SnapshotState"/>. This way, we can quickly and efficiently manipulate bits but also use efficient
/// "hash map like" access.
/// </summary>
public class InputSnapshot : IEquatable<InputSnapshot>
{
    private readonly Dictionary<string, int> _snapshotKeyIndexesMap;
    public Dictionary<string, int> SnapshotKeyIndexesMap { get { return _snapshotKeyIndexesMap; } }
    public SnapshotState SnapshotState { private set; get; } // int in which each bit is state of one of keys, mapped by snapshotKeyIndexesMap

    /// <summary>
    /// Constructor for <see cref="InputSnapshot"/> class. Initializes state to 0 (no key presses) and loads mapping from string key names to state bit indexes.
    /// </summary>
    /// <param name="snapshotKeyIndexMap"></param> - mapping from string key name to bit index in <see cref="InputSnapshot.SnapshotState"/>.
    public InputSnapshot(Dictionary<string, int> snapshotKeyIndexMap)
    {
        this._snapshotKeyIndexesMap = snapshotKeyIndexMap;
        this.SnapshotState = 0;
    }

    /// <summary>
    /// Sets state bit for specified key name using <see cref="InputSnapshot.snapshotKeyIndexesMap"/>.
    /// </summary>
    /// <param name="keyName"></param> - key to <see cref="InputSnapshot.snapshotKeyIndexesMap"/> for which state bit will be set.
    public void SetKeyBitByName(string keyName)
    {
        int index = this.SnapshotKeyIndexesMap[keyName];
        SetKeyBitByIndex(index);
    }

    /// <summary>
    /// Clears state bit for specified key name using <see cref="InputSnapshot.snapshotKeyIndexesMap"/>.
    /// </summary>
    /// <param name="keyName"></param> - key to <see cref="InputSnapshot.snapshotKeyIndexesMap"/> for which state bit will be cleared.
    public void ClearKeyBitByName(string keyName)
    {
        int index = this.SnapshotKeyIndexesMap[keyName];
        ClearKeyBitByIndex(index);
    }

    /// <summary>
    /// Gets state bit for specified key name using <see cref="InputSnapshot.snapshotKeyIndexesMap"/>.
    /// </summary>
    /// <param name="keyName"></param> - key to <see cref="InputSnapshot.snapshotKeyIndexesMap"/> for which state will be returned.
    public int GetBitByName(string keyName)
    {
        int index = this.SnapshotKeyIndexesMap[keyName];
        return GetBitByIndex(index);
    }

    /// <summary>
    /// Sets a state bit given the index of it.
    /// </summary>
    /// <param name="idx"></param> - index of bit to set.
    private void SetKeyBitByIndex(int idx) => this.SnapshotState |= (1 << idx);

    /// <summary>
    /// Clears a state bit given the index of it.
    /// </summary>
    /// <param name="idx"></param> - index of bit to set.
    private void ClearKeyBitByIndex(int idx) => this.SnapshotState &= ~(1 << idx);

    /// <summary>
    /// Returns a state bit given the index of it.
    /// </summary>
    /// <param name="idx"></param> - index of a state bit to return.
    /// <returns></returns> - state bit value 0 or 1.
    private int GetBitByIndex(int idx) => (this.SnapshotState >> idx) & 1;

    public void ClearState() => this.SnapshotState = 0;

    /// <summary>
    /// Update this <see cref="InputSnapshot"/> object's state with state values from argument. Values are updated with OR operation.
    /// </summary>
    /// <param name="snapshot"></param> - <see cref="InputSnapshot"/> object to update state from.
    public void UpdateSnapshot(InputSnapshot snapshot)
    {
        this.SnapshotState |= snapshot.SnapshotState;
    }

    /// <summary>
    /// Function to determine if the the snapshot is empty (no key was pressed = no state bit is set).
    /// </summary>
    /// <returns> true when snapshot is empty and false when it is not. </returns>
    public bool IsSnapshotEmpty() => this.SnapshotState == 0;

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
        foreach(var kvp in this.SnapshotKeyIndexesMap)
        {
            int bit = GetBitByName(kvp.Key);
            toPrint += String.Format("Name: {0}, Value: {1}, Index: {2} \n", kvp.Key, bit, kvp.Value);
        }
        return toPrint;
    }

    public bool Equals(InputSnapshot other)
    {
        if(other == null) return false;
        if(this.SnapshotState != other.SnapshotState) return false;
        if(this.SnapshotKeyIndexesMap.Count != other.SnapshotKeyIndexesMap.Count) return false;
        // If difference between sets is different than zero
        if(this.SnapshotKeyIndexesMap.Except(other.SnapshotKeyIndexesMap).Any()) return false;
        if(this.SnapshotState != other.SnapshotState) return false;
        return true;
    }

    //public override bool Equals(System.Object obj)
    //{
        //if(!this.GetType().Equals(obj.GetType())) return false;
        //TODO
    //}

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}

public class InputSnapshotEqualityComparer : IEqualityComparer<InputSnapshot>
{
    public bool Equals(InputSnapshot s1, InputSnapshot s2)
    {
        if(s1.SnapshotState == s2.SnapshotState) return true;
        return false;
    }

    public int GetHashCode(InputSnapshot s)
    {
        // snapshotState is unique int value for all combinations of keys, so it is enough to do that
        return s.SnapshotState.GetHashCode();
    }
}

public class ComboSequence : IEquatable<ComboSequence>
{
    public List<InputSnapshot> Sequence { private set; get; }

    public ComboSequence()
    {
        this.Sequence = new List<InputSnapshot>();
    }

    public void AddInputSnapshot(InputSnapshot snapshot) => this.Sequence.Add(snapshot);

    public void ClearComboSequence() => this.Sequence.Clear();

    public override string ToString()
    {
        string comboSequenceString = "Combo sequence: \n";
        for (int i = 0; i < this.Sequence.Count; i++)
        {
            comboSequenceString += String.Format("{0}", this.Sequence[i]);
            if(i != this.Sequence.Count - 1) comboSequenceString += "->";
        }
        return comboSequenceString;
    }

    public bool Equals(ComboSequence other)
    {
        if(other == null) return false;
        if(other.Sequence.Count != this.Sequence.Count) return false;
        if(other.Sequence.Count == 0 && this.Sequence.Count == 0) return true;
        for (int i = 0; i < this.Sequence.Count; i++)
        {
            if(other.Sequence[i] != this.Sequence[i]) return false;
        }
        return true;
    }

    public override int GetHashCode()
    {
        int toHash = 0;
        for (int i = 0; i < this.Sequence.Count; i++)
        {
            toHash ^= this.Sequence[i].SnapshotState;
        }
        return toHash.GetHashCode();
    }
}

public class ComboSequenceEqualityComparer : IEqualityComparer<ComboSequence>
{
    public bool Equals(ComboSequence s1, ComboSequence s2)
    {
        if(s1.Sequence.Count != s2.Sequence.Count) return false;
        if(s1.Sequence.Count == 0 && s2.Sequence.Count == 0) return true;
        for (int i = 0; i < s1.Sequence.Count; i++)
        {
            if(s1.Sequence[i] != s2.Sequence[i]) return false;
        }
        return true;
    }

    public int GetHashCode(ComboSequence s)
    {
        int toHash = 0;
        for (int i = 0; i < s.Sequence.Count; i++)
        {
            toHash ^= s.Sequence[i].SnapshotState;
        }
        return toHash.GetHashCode();
    }
}


public class ComboToActionMap
{
    private Dictionary<ComboSequence, Action<string>> _map;

    public ComboToActionMap()
    {
        this._map = new Dictionary<ComboSequence, Action<string>>();
    }

    public void AddNewCombo(ComboSequence key, Action<string> value)
    {
        // TODO: Check if there are no 'empty' snapshots, as they mean end of combo
        this._map.Add(key, value);
    }

    public void RemoveCombo(ComboSequence key)
    {
        this._map.Remove(key);
    }

    public Action<string> GetActionFromCombo(ComboSequence key)
    {
        return this._map[key];
    }
}