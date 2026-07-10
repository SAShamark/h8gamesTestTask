using System;
using UnityEngine;

namespace Services.Currency
{
    public class CurrencyBank : IBank
    {
        public CurrencyType CurrencyType { get; private set; }
        public event Action<int> OnCurrencyChanged;
        public event Action<CurrencyType, int> OnNotEnough;
        public int Currency { get; private set; }

        public CurrencyBank(CurrencyType currencyType, int initialCurrency)
        {
            CurrencyType = currencyType;
            Currency = initialCurrency;
        }

        public void SetCurrency(int value)
        {
            Currency = value;
            OnCurrencyChanged?.Invoke(Currency);
        }

        public void EarnCurrency(int value) => SetCurrency(Currency + value);

        public bool SpendCurrency(int value)
        {
            if (Currency >= value)
            {
                SetCurrency(Currency - value);
                return true;
            }

            OnNotEnough?.Invoke(CurrencyType, value - Currency);
            Debug.Log($"Not Enough: {value - Currency} {CurrencyType}");
            return false;
        }
    }
}