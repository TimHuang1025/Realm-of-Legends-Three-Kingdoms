using System;
using UnityEngine;
using UnityEngine.UIElements;

public class LogInScreen : MonoBehaviour
{

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        Button guest = root.Q<Button>("Guest");
        Button apple = root.Q<Button>("Apple");
        Button email = root.Q<Button>("Email");
        Button namePassword = root.Q<Button>("NamePassword");   

        guest.clicked += () => Debug.Log($"Clicked guest login button");
        apple.clicked += () => Debug.Log($"Clicked apple login button");
        email.clicked += () => Debug.Log($"Clicked email login button");
        namePassword.clicked += () => Debug.Log($"Clicked namePassword login button");
    }
}