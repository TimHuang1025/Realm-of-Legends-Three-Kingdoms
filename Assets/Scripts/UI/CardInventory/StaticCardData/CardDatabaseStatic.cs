// Assets/Scripts/CardDB/CardDatabaseStatic.cs   ← 新文件
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "CardDatabaseStatic",
    menuName = "CardDB/Static Database")]
public class CardDatabaseStatic : ScriptableObject
{
    [SerializeField] private List<CardInfoStatic> cards = new();
    public IReadOnlyList<CardInfoStatic> All => cards;
    Dictionary<string, CardInfoStatic> lookup;

    void OnEnable()
    {
        lookup = new();
        foreach (var c in cards)
            lookup[c.id] = c;
    }
    public CardInfoStatic Get(string id) => lookup.TryGetValue(id, out var c) ? c : null;
}

[Serializable]
public class CardInfoStatic
{
    public string id;
    public string displayName;
    public Tier tier;
    public Faction faction;
    public Sprite iconSprite;
    public Sprite fullBodySprite;
    public string activeSkillId;
    public string passiveOneId;
    public string passiveTwoId;
    public string trait;
    [TextArea] public string description;
    public float[] base_value_multiplier = {1,1,1,1};
}

public enum Tier    { S, A, B, C, N }
public enum Faction { wei, shu, wu, neutral }
