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
        var combo_token = combo_input_handler_system.GetComboToken();
        bool token_empty = ComboInputHandlerSystem.IsTokenEmpty(combo_token);
        if(!token_empty)
        {
            Debug.Log(combo_token);
        }
    }
}

public class ComboInputHandlerSystem
{
    private string[] combo_keys_names;
    public ComboInputHandlerSystem(params string[] combo_keys_names)
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

