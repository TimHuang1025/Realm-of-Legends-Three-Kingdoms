using UnityEngine;

public class GachaTicketManager : MonoBehaviour
{
    public static GachaTicketManager I { get; private set; }

    public int Ticket { get; private set; }

    public System.Action<int> OnTicketChanged;   // 订阅 UI

    void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Add(int amount)
    {
        Ticket += amount;
        OnTicketChanged?.Invoke(Ticket);
    }

    public bool TrySpend(int amount)
    {
        if (Ticket < amount) return false;
        Ticket -= amount;
        OnTicketChanged?.Invoke(Ticket);
        return true;
    }
}
