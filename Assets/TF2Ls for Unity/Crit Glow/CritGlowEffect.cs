using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CritGlowEffect : MonoBehaviour
{
    [ColorUsage(true, true)]
    [SerializeField] Color mainCritColor;
    [SerializeField] Color particleSparkColor;
    [SerializeField] Color particleGlowColor;

    [SerializeField] Renderer[] renderers;
    [SerializeField] ParticleSystem[] particleSystems;

    int useEmissionID;
    int emissionID;

    // Start is called before the first frame update
    void Start()
    {
        useEmissionID = Shader.PropertyToID("_Emission");
        emissionID = Shader.PropertyToID("_EmissionColor");
    }

    [ContextMenu(nameof(EnableCrits))]
    public void EnableCrits()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            for (int j = 0; j < renderers[i].materials.Length; j++)
            {
                var mat = renderers[i].materials[j];
                mat.SetInt(useEmissionID, 1);
                mat.SetColor(emissionID, mainCritColor);
            }
        }

        for (int i = 0; i < particleSystems.Length; i++)
        {
            var main = particleSystems[i].main;
            main.startColor = particleSparkColor;

            ParticleSystem child;
            if (particleSystems[i].TryGetComponent(out child))
            {
                var m = child.main;
                m.startColor = particleGlowColor;
            }

            particleSystems[i].Play();
        }
    }

    [ContextMenu(nameof(DisableCrits))]
    public void DisableCrits()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            for (int j = 0; j < renderers[i].materials.Length; j++)
            {
                var mat = renderers[i].materials[j];
                mat.SetInt(useEmissionID, 0);
            }
        }

        for (int i = 0; i < particleSystems.Length; i++)
        {
            particleSystems[i].Stop();
        }
    }
}