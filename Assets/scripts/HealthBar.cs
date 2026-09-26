using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
public class HealthBar : MonoBehaviour
{
    public Slider healthSlider;
    public void SetMaxHealth(int health)
    {
        if (healthSlider == null)
            healthSlider = GetComponentInChildren<Slider>(true);
        if (healthSlider == null)
            return;

        healthSlider.maxValue = health;
        healthSlider.value = health;
    }

    public void SetHealth(int health)
    {
        if (healthSlider == null)
            healthSlider = GetComponentInChildren<Slider>(true);
        if (healthSlider != null)
            healthSlider.value = health;
    }
}
