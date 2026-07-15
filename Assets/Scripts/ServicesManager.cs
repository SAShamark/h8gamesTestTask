using Services;
using Services.Currency;
using Services.Storage;
using Services.Time;
using UnityEngine;

public class ServicesManager : MonoSingleton<ServicesManager>
{
    [SerializeField] private CurrencyCollection _currencyCollection;

    public StorageService StorageService { get; private set; } = new();
    public CurrencyService CurrencyService { get; private set; } = new();
    public TimerService TimerService { get; private set; } = new();

    public void Initialize()
    {
        TimerService.Init();
        InitializeSingleton(true);
        CurrencyService.Init(StorageService, _currencyCollection);
    }

    private void Update()
    {
        TimerService.Tick();
    }

    private void OnApplicationPause(bool pause) => TimerService.OnApplicationPause(pause);

    private void OnApplicationQuit() => TimerService.OnApplicationQuit();

    private void OnDisable() => TimerService.OnDisable();

    private void OnDestroy() => TimerService.OnDestroy();
}
