using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
        var combo_token = combo_input_handler_system.GetComboToken();
        if(!InputMonitorSystem.IsTokenEmpty(combo_token))
        {
            Debug.Log(combo_token);
        }
    }
}

public class InputMonitorSystem
{
    private string[] combo_keys_names;
    public InputMonitorSystem(params string[] combo_keys_names)
    {
        this.combo_keys_names =  combo_keys_names;
    }

    public Dictionary<string, bool> GetComboToken()
    {
        var combo_token = new Dictionary<string, bool>();
        foreach(string key in this.combo_keys_names)
        {
            combo_token[key] = Input.GetButtonDown(key);
        }
        return combo_token;
    }

    public static bool IsTokenEmpty(Dictionary<string, bool> token) => !token.ContainsValue(true);
}