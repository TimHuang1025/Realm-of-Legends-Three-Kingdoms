using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GiftDatabase", menuName = "Game/Gift Database")]
public class GiftDatabase : ScriptableObject
{
    public List<GiftData> gifts = new();
}

[System.Serializable]
public class GiftData
{
    public string name;
    public Sprite icon;
    public int value;
    public int stock;
}
