using System;
using UnityEngine;
using UnityEngine.UIElements;

public class LogInScreen : MonoBehaviour
{

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        Button apple = root.Q<Button>("AppleLogo");
        Button google = root.Q<Button>("GoogleLogo");
        Button email = root.Q<Button>("EmailLogo");
        Button account = root.Q<Button>("AccountLogo");
        Button guest = root.Q<Button>("GuestLogo");

        apple.clicked += () => Debug.Log($"Clicked apple login button");
        google.clicked += () => Debug.Log($"Clicked google login button");
        email.clicked += () => Debug.Log($"Clicked email login button");
        account.clicked += () => Debug.Log($"Clicked account login button");
        guest.clicked += () => Debug.Log($"Clicked guest login button");
    }
}