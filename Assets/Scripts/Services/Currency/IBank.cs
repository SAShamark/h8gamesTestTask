using System;

namespace Services.Currency
{
    public interface IBank
    {
        event Action<int> OnCurrencyChanged;
        event Action<CurrencyType, int> OnNotEnough;
        int Currency { get; }
        void SetCurrency(int value);
        void EarnCurrency(int value);
        bool SpendCurrency(int value);
    }
}