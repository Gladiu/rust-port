using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour
{
    public float speed = 3.0F;
    public float rotateSpeed = 3.0F;
    private InputMonitorSystem combo_input_handler_system = new InputMonitorSystem(
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
        var combo_token = combo_input_handler_system.GetInputSnapshot();
        if(!InputMonitorSystem.IsSnapshotEmpty(combo_token))
        {
            Debug.Log(combo_token);
        }
    }
}

public class InputMonitorSystem
{
    private string[] monitored_keys_names;
    public InputMonitorSystem(params string[] monitored_keys_names)
    {
        this.monitored_keys_names = monitored_keys_names;
    }

    public Dictionary<string, bool> GetInputSnapshot()
    {
        var input_snapshot = new Dictionary<string, bool>();
        foreach(string key in this.monitored_keys_names)
        {
            input_snapshot[key] = Input.GetButtonDown(key);
        }
        return input_snapshot;
    }

    public static bool IsSnapshotEmpty(Dictionary<string, bool> token) => !token.ContainsValue(true);
}

public class SnapshotComparer : IEqualityComparer<Dictionary<string, bool>>
{
    // Dictionary is reference type - its bits cannot be just checked
    // https://stackoverflow.com/questions/3804367/testing-for-equality-between-dictionaries-in-c-sharp
    // https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.equalitycomparer-1?view=net-6.0
    public bool Equals(Dictionary<string, bool> s1, Dictionary<string, bool> s2)
    {
        if(s1 == s2) return true;
        if((s1 == null) || (s2 == null)) return false;
        if(s1.Count != s2.Count) return false;

        foreach(var kvp in s1)
        {
            bool val;
            if(!s2.TryGetValue(kvp.Key, out val)) return false;
            if(!(kvp.Value == val)) return false;
        }
        return true;
    }

    public int GetHashCode(Dictionary<string, bool> s)
    {
        // TODO: Is correct? Maybe not even necessary
        return s.GetHashCode();
    }
}

// do not use combo class as a key to dict with combos
// Change combo class to combo handler
// Combo handler will add snapshots to list
// it will have timer
// it will also have a list of valid combos?

public class ComboHandlerSystem
{
    public List<Dictionary<string, bool>> combo_sequences { get; set; }
    private int snapshot_size;
    private float combo_input_time;
    private float timer;
    // TODO: Change that func signature to something more logical
    private Dictionary<List<Dictionary<string, bool>>, Func<string>> combos_list;

    public ComboHandlerSystem(int snapshot_size, float combo_input_time)
    {
        this.combo_sequences = new List<Dictionary<string, bool>>();
        this.combos_list = new Dictionary<List<Dictionary<string, bool>>, Func<string>>(new ComboSeqComparer());
        // TODO: add some valid combos
        this.snapshot_size = snapshot_size;
        this.combo_input_time = combo_input_time;
        this.timer = combo_input_time;
    }

    public void AddComboSnapshot(Dictionary<string, bool> new_snapshot)
    {
        if(new_snapshot.Count != snapshot_size)
        {
            string exception_message = String.Format("Provided dictionary has incorrect size. Got {0}, expected {1}", new_snapshot.Count, this.snapshot_size);
            throw new ArgumentException(exception_message);
        }

        this.combo_sequences.Add(new_snapshot);
    }

    public void UpdateComboInputTimer(float dtime) => this.timer -= dtime;

    public Func<string> GetAbilityFromCombo()
    {
        if(this.combo_input_time <= 0)
        {
            this.timer = 0;
            Func<string> ability_closure;
            this.combos_list.TryGetValue(combo_sequences, out ability_closure);
            return ability_closure;
        }
        return null;
    }
}

public class ComboSeqComparer : IEqualityComparer<List<Dictionary<string, bool>>>
{
    public bool Equals(List<Dictionary<string, bool>> c1, List<Dictionary<string, bool>> c2)
    {
        if(c1 == c2) return true;
        if((c1 == null) || (c2 == null)) return false;
        if(c1.Count != c2.Count) return false;

        var snapshot_comparer = new SnapshotComparer();

        for(int i = 0; i < c1.Count; i++)
        {
            // Order matters when performing combo
            if(!snapshot_comparer.Equals(
                c1[i], c2[i]))
                return false;
        }
        return true;
    }

    public int GetHashCode(List<Dictionary<string, bool>> c)
    {
        return c.GetHashCode();
    }
}



