using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour
{
    public float speed = 3.0F;
    public float rotateSpeed = 3.0F;
    private ComboInputHandlerSystem combo_input_handler_system = new ComboInputHandlerSystem(
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

        combo_input_handler_system.CaptureComboKeysState();
        combo_input_handler_system.PrintComboKeysState();
    }
}

public class ComboInputHandlerSystem
{
    private ComboKey[] combo_keys;
    // TODO:
    // Make a snapshooting function to record combo keys presses per frame
    // Modify it to only be called when there is some key press, do not record empty tokens
    // Return such token and collect it into array of tokens (make token another class?)
    // Make this array hashable type and use it as a a key for Dictionary where values will be functions representing attacks / abilities

    public ComboInputHandlerSystem(params string[] combo_keys_names)
    {
        this.combo_keys = new ComboKey[combo_keys_names.Length];
        for(int i = 0; i < combo_keys_names.Length; i++)
        {
            this.combo_keys[i] = new ComboKey(combo_keys_names[i]);
        }
    }

    public void CaptureComboKeysState()
    {
        foreach(ComboKey k in this.combo_keys)
        {
            k.ReadComboKeyState();
        }
    }

    public void PrintComboKeysState()
    {
        foreach(ComboKey k in this.combo_keys)
        {
            if(k.current_state)
            {
                Debug.Log(k);
            }
        }
    }
}
public class ComboKey
{
    private string name;
    public bool current_state { get; set; }

    public ComboKey(string name)
    {
        this.name = name;
        this.current_state = false;
    }

    public void ReadComboKeyState()
    {
        this.current_state = Input.GetButtonDown(name);
    }

    public override string ToString()
    {
        return string.Format("{0} - {1}", this.name, this.current_state);
    }
}

