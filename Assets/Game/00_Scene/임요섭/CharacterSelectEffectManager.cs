using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CharacterSelectEffectManager : MonoBehaviour
{
    public ParticleSystem selectParticles;
    public ParticleSystem otherCharacterParticles;
    public void OnCharacterSelected()
    {
        if (selectParticles != null)
        {
            if (otherCharacterParticles != null)
            {
                otherCharacterParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            selectParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            selectParticles.Play();
        }
    }
}
