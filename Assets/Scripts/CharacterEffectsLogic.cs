using UnityEngine;

[System.Serializable]
public class CharacterEffectsLogic
{
    [SerializeField] private ParticleSystem _stepDust;
    [SerializeField] private bool _useStepDustLoop = true;

    [SerializeField] private GameObject _landingSmokePrefab;
    [SerializeField] private Vector3 _landingSmokeLocalOffset = new(0f, 0.5f, 0f);

    private ParticleSystem _landingSmoke;
    private bool _isStepDustPlaying;

    public void Initialize()
    {
        ConfigureStepDust();
        _landingSmoke = CreateEffect(_landingSmokePrefab);
    }

    public void UpdateStepDust(bool isMovingOnGround)
    {
        if (isMovingOnGround)
        {
            PlayStepDust();
            return;
        }

        StopStepDust();
    }

    public void PlayLandingSmoke(Vector3 groundPosition)
    {
        PlayEffect(_landingSmoke, groundPosition + _landingSmokeLocalOffset);
    }

    private void PlayStepDust()
    {
        if (_stepDust == null || _isStepDustPlaying)
        {
            return;
        }

        _stepDust.gameObject.SetActive(true);
        _stepDust.Play(true);
        _isStepDustPlaying = true;
    }

    private ParticleSystem CreateEffect(GameObject prefab)
    {
        if (prefab == null)
        {
            return null;
        }

        GameObject instance = Object.Instantiate(prefab);
        instance.SetActive(false);

        ParticleSystem particleSystem = instance.GetComponentInChildren<ParticleSystem>(true);
        ConfigureAsOneShot(particleSystem);
        return particleSystem;
    }

    private void ConfigureAsOneShot(ParticleSystem particleSystem)
    {
        if (particleSystem == null)
        {
            return;
        }

        ParticleSystem[] systems = particleSystem.GetComponentsInChildren<ParticleSystem>(true);

        for (int i = 0; i < systems.Length; i++)
        {
            ParticleSystem.MainModule main = systems[i].main;
            main.loop = false;
            main.playOnAwake = false;
        }
    }

    private void ConfigureStepDust()
    {
        if (_stepDust == null)
        {
            return;
        }

        ParticleSystem[] systems = _stepDust.GetComponentsInChildren<ParticleSystem>(true);

        for (int i = 0; i < systems.Length; i++)
        {
            ParticleSystem.MainModule main = systems[i].main;
            main.loop = _useStepDustLoop;
            main.playOnAwake = false;
            systems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        _stepDust.gameObject.SetActive(false);
    }

    public void StopStepDust()
    {
        if (_stepDust == null)
        {
            return;
        }

        _stepDust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _stepDust.gameObject.SetActive(false);
        _isStepDustPlaying = false;
    }

    private void PlayEffect(ParticleSystem particleSystem, Vector3 position)
    {
        if (particleSystem == null)
        {
            return;
        }

        Transform effectTransform = particleSystem.transform;
        effectTransform.SetPositionAndRotation(
            position,
            Quaternion.identity);

        GameObject effectObject = effectTransform.gameObject;
        effectObject.SetActive(true);

        ParticleSystem[] systems = particleSystem.GetComponentsInChildren<ParticleSystem>(true);

        foreach (var particle in systems)
        {
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particle.Play(true);
        }
    }
}
