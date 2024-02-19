using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyType
{
    army, ballista, biker, buggy, teleporter, disabler, drone, gatling, healer, nuker, slower, supplier, truck,
    undefined
}

public class EngineSoundController : MonoBehaviour {
    private AudioSource _source;
    private ISpeedForEngineSoundsProvider _provider;

    #region Manage Enemy Engine SFX
    private EnemyType _enemyType;
    [ShowInInspector]
    public EnemyType enemyType
    {
        get
        {
            string name = transform.parent.parent.gameObject.name;
            if (name.Contains("Army"))
                _enemyType = EnemyType.army;
            else if (name.Contains("Ballista"))
                _enemyType = EnemyType.ballista;
            else if (name.Contains("Biker"))
                _enemyType = EnemyType.biker;
            else if (name.Contains("Buggy"))
                _enemyType = EnemyType.buggy;
            else if (name.Contains("Teleporter"))
                _enemyType = EnemyType.teleporter;
            else if (name.Contains("Disabler"))
                _enemyType = EnemyType.disabler;
            else if (name.Contains("Drone"))
                _enemyType = EnemyType.drone;
            else if (name.Contains("Gatling"))
                _enemyType = EnemyType.gatling;
            else if (name.Contains("Healer"))
                _enemyType = EnemyType.healer;
            else if (name.Contains("Nuker"))
                _enemyType = EnemyType.nuker;
            else if (name.Contains("Slower"))
                _enemyType = EnemyType.slower;
            else if (name.Contains("Supplier"))
                _enemyType = EnemyType.supplier;
            else if (name.Contains("Truck"))
                _enemyType = EnemyType.truck;
            else
                _enemyType = EnemyType.undefined;

            return _enemyType;
        }
    }

    private float originVolume;
    private static EngineSoundController armyBallistaInstance, bikerInstance, teleporterTruckInstance, buggyDisablerInstance, droneInstance, gatlingSupplierInstance, healerSlowerInstance, nukerInstance;

    private static Dictionary<EnemyType, EnemyEngineSfxData> _enemyEngineDict;
    private static Dictionary<EnemyType, EnemyEngineSfxData> enemyEngineDict
    {
        get
        {
            if (_enemyEngineDict == null)
                _enemyEngineDict = (Resources.Load("Audio/EnemyEngineLibrary") as EnemyEngineLibrary).sfxDict;
            return _enemyEngineDict;
        }
    }

    private void LoadEnemyEngineSfx()
    {
        _source.clip = enemyEngineDict[enemyType].clip;
        _source.volume = enemyEngineDict[enemyType].volume;
        _source.Play();
    }
    #endregion

    void Start() {
        _source = GetComponent<AudioSource>();
        //_provider = GetComponentInParent<ISpeedForEngineSoundsProvider>();
        _provider = GetComponentInParent<EnemyInSwarm>().myWave;
        if (_provider == null) {
            _provider = GetComponentInParent<ISpeedForEngineSoundsProvider>();
        }
        Invoke(nameof(RandomPitchChange), Random.Range(3f,10f));
        LoadEnemyEngineSfx();
        originVolume = _source.volume;
    }


    public float naturalPitch = 1f;
    public float targetPitch = 1f;
    public float pitchMultiplier = 1f;
    void Update() {
        targetPitch = _provider.GetSpeed().Remap(0, 10, 0.5f, 1.5f);
        
        _source.pitch = Mathf.Lerp(_source.pitch, targetPitch * pitchMultiplier, 1f*Time.deltaTime);
        // _source.pitch = Mathf.Lerp(_source.pitch, naturalPitch * targetPitch * pitchMultiplier, 1f*Time.deltaTime);

        float lerpSpeed = 0.5f;
        // make only one instance of each enemy type appear
        switch (enemyType)
        {
            case EnemyType.army:
            case EnemyType.ballista:
                // if this is the instance but it is no longer engaging, set the instance to null
                if (armyBallistaInstance == this && !IsEngaging(this))
                    armyBallistaInstance = null;
                // else if there is no instance of this type and this instance is engaging, set this as the singleton instance
                else if (armyBallistaInstance == null && IsEngaging(this))
                    armyBallistaInstance = this;

                _source.volume = Mathf.Lerp(_source.volume, armyBallistaInstance == this ? originVolume : 0, Time.unscaledDeltaTime * lerpSpeed);
                break;

            case EnemyType.buggy:
            case EnemyType.disabler:
                if (buggyDisablerInstance == this && !IsEngaging(this))
                    buggyDisablerInstance = null;
                else if (buggyDisablerInstance == null && IsEngaging(this))
                    buggyDisablerInstance = this;

                _source.volume = Mathf.Lerp(_source.volume, buggyDisablerInstance == this ? originVolume : 0, Time.unscaledDeltaTime * lerpSpeed);
                break;

            case EnemyType.teleporter:
            case EnemyType.truck:
                if (teleporterTruckInstance == this && !IsEngaging(this))
                    teleporterTruckInstance = null;
                else if (teleporterTruckInstance == null && IsEngaging(this))
                    teleporterTruckInstance = this;

                _source.volume = Mathf.Lerp(_source.volume, teleporterTruckInstance == this ? originVolume : 0, Time.unscaledDeltaTime * lerpSpeed);
                break;

            case EnemyType.drone:
                if (droneInstance == this && !IsEngaging(this))
                    droneInstance = null;
                else if (droneInstance == null && IsEngaging(this))
                    droneInstance = this;

                _source.volume = Mathf.Lerp(_source.volume, droneInstance == this ? originVolume : 0, Time.unscaledDeltaTime * lerpSpeed);
                break;

            case EnemyType.biker:
                if (bikerInstance == this && !IsEngaging(this))
                    bikerInstance = null;
                else if (bikerInstance == null && IsEngaging(this))
                    bikerInstance = this;

                _source.volume = Mathf.Lerp(_source.volume, bikerInstance == this ? originVolume : 0, Time.unscaledDeltaTime * lerpSpeed);
                break;

            case EnemyType.gatling:
            case EnemyType.supplier:
                if (gatlingSupplierInstance == this && !IsEngaging(this))
                    gatlingSupplierInstance = null;
                else if (gatlingSupplierInstance == null && IsEngaging(this))
                    gatlingSupplierInstance = this;

                _source.volume = Mathf.Lerp(_source.volume, gatlingSupplierInstance == this ? originVolume : 0, Time.unscaledDeltaTime * lerpSpeed);
                break;

            case EnemyType.nuker:
                if (nukerInstance == this && !IsEngaging(this))
                    nukerInstance = null;
                else if (nukerInstance == null && IsEngaging(this))
                    nukerInstance = this;

                _source.volume = Mathf.Lerp(_source.volume, nukerInstance == this ? originVolume : 0, Time.unscaledDeltaTime * lerpSpeed);
                break;

            case EnemyType.slower:
            case EnemyType.healer:
                if (healerSlowerInstance == this && !IsEngaging(this))
                    healerSlowerInstance = null;
                else if (healerSlowerInstance == null && IsEngaging(this))
                    healerSlowerInstance = this;

                _source.volume = Mathf.Lerp(_source.volume, healerSlowerInstance == this ? originVolume : 0, Time.unscaledDeltaTime * lerpSpeed);
                break;
        }
    }

    private bool IsEngaging(EngineSoundController instance)
    {
        EnemyWave wave = instance.GetComponentInParent<EnemyInSwarm>().myWave;
        if (wave.distance < 20 && !wave.isLeaving)
            return true;
        else
            return false;
    }

    void RandomPitchChange() {
        pitchMultiplier = 1.1f;
        Invoke(nameof(RevertRandomPitchChange), Random.Range(0.1f,0.2f));
    }

    void RevertRandomPitchChange() {
        pitchMultiplier = 1f;
        Invoke(nameof(RandomPitchChange), Random.Range(3f,10f));
    }
}

public interface ISpeedForEngineSoundsProvider {
    public float GetSpeed();
}
