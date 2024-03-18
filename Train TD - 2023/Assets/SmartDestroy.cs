using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmartDestroy : MonoBehaviour
{
    public void Engage() {
        var particles = GetComponentsInChildren<ParticleSystem>();

        foreach (var particle in particles) {
            //particle.transform.SetParent(null);
            particle.Stop();
            //Destroy(particle.gameObject, 5f);
        }

        var light = GetComponent<Light>();

        if (light != null) {
            StartCoroutine(FadeLight(light));
        }
        
        Destroy(gameObject,5f);
    }

    IEnumerator FadeLight(Light light) {
        while (light.intensity > 0) {
            light.intensity = Mathf.MoveTowards(light.intensity, 0, 1 * Time.deltaTime);
            yield return null;
        }

        light.enabled = false;
    }
}
