//using UnityEngine;
//using System;

//public class CurrencyManager : MonoBehaviour
//{
//    public static CurrencyManager Instance { get; private set; }

//    public event Action<int> OnCoinsChanged;

//    private int _coins = 0;
//    public int Coins => _coins;

//    private void Awake()
//    {
//        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//        Instance = this;
//    }

//    public void AddCoins(int amount)
//    {
//        _coins += amount;
//        OnCoinsChanged?.Invoke(_coins);
//        Debug.Log($"[Currency] Added {amount}. Total: {_coins}");
//    }

//    public bool SpendCoins(int amount)
//    {
//        if (_coins < amount)
//        {
//            Debug.Log("[Currency] Not enough coins!");
//            return false;
//        }
//        _coins -= amount;
//        OnCoinsChanged?.Invoke(_coins);
//        Debug.Log($"[Currency] Spent {amount}. Remaining: {_coins}");
//        return true;
//    }
//}

using UnityEngine;
using System;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    public event Action<int> OnCoinsChanged;

    [Header("Starting Coins")]
    public int startingCoins = 9999;

    private int _coins = 0;
    public int Coins => _coins;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        _coins = startingCoins;
        OnCoinsChanged?.Invoke(_coins);
    }

    public void AddCoins(int amount)
    {
        _coins += amount;
        OnCoinsChanged?.Invoke(_coins);
        Debug.Log($"[Currency] Added {amount}. Total: {_coins}");
    }

    public bool SpendCoins(int amount)
    {
        if (_coins < amount)
        {
            Debug.Log($"[Currency] Not enough coins! Have {_coins}, need {amount}");
            return false;
        }
        _coins -= amount;
        OnCoinsChanged?.Invoke(_coins);
        Debug.Log($"[Currency] Spent {amount}. Remaining: {_coins}");
        return true;
    }
}