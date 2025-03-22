using UnityEngine;
using System.Collections.Generic;

public class ComplexHealthBar : MonoBehaviour
{
    private const int SEGMENTS = 10;
    private const float DANGER_THRESHOLD = 3f;
    private const float CRITICAL_THRESHOLD = 2f;
    private const float DEATH_THRESHOLD = 1f;

    [SerializeField] private HealthConfigurationSO config;
    private List<SpriteRenderer> segments = new List<SpriteRenderer>();
    private float flashTimer;
    private bool flashState;

    private void Awake()
    {
        CreateHealthBarSegments();
    }

    private void CreateHealthBarSegments()
    {
        float startX = -(config.segmentSize.x * SEGMENTS) / 2f;
        
        for (int i = 0; i < SEGMENTS; i++)
        {
            GameObject segment = new GameObject($"Segment_{i}");
            segment.transform.SetParent(transform);
            segment.transform.localPosition = new Vector3(
                startX + (i * config.segmentSize.x),
                config.healthBarOffset.y,
                0
            );

            var renderer = segment.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 100; // Ensure visibility
            segments.Add(renderer);
        }
    }

    public void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        float healthPercentage = (currentHealth / maxHealth) * 100f;

        if (healthPercentage <= 0)
        {
            SetAllSegments(config.redSprite);
            return;
        }

        if (healthPercentage <= DANGER_THRESHOLD)
        {
            UpdateDangerState(healthPercentage);
            return;
        }

        UpdateNormalState(healthPercentage);
    }

    private void SetAllSegments(Sprite sprite)
    {
        foreach (var segment in segments)
        {
            segment.sprite = sprite;
        }
    }

    private void UpdateDangerState(float healthPercentage)
    {
        if (healthPercentage <= DANGER_THRESHOLD && healthPercentage > CRITICAL_THRESHOLD)
        {
            SetDangerText();
            flashTimer = 1f;
        }
        else if (healthPercentage <= CRITICAL_THRESHOLD && healthPercentage > DEATH_THRESHOLD)
        {
            SetDangerText();
            flashTimer = 0.5f;
        }
        else if (healthPercentage <= DEATH_THRESHOLD)
        {
            SetDangerText();
            flashTimer = 0.25f;
        }
    }

    private void SetDangerText()
    {
        segments[0].sprite = config.redSprite;
        segments[1].sprite = config.redSprite;
        segments[2].sprite = config.letterD;
        segments[3].sprite = config.letterA;
        segments[4].sprite = config.letterN;
        segments[5].sprite = config.letterG;
        segments[6].sprite = config.letterE;
        segments[7].sprite = config.letterR;
        segments[8].sprite = config.redSprite;
        segments[9].sprite = config.redSprite;
    }

    private void UpdateNormalState(float healthPercentage)
    {
        int filledSegments = Mathf.CeilToInt(healthPercentage / 10f);

        for (int i = 0; i < SEGMENTS; i++)
        {
            if (i < filledSegments)
            {
                float segmentHealth = (healthPercentage - (i * 10f)) / 10f;
                segments[i].sprite = GetSpriteForSegment(segmentHealth);
            }
            else
            {
                segments[i].sprite = config.darkSprite;
            }
        }
    }

    private Sprite GetSpriteForSegment(float segmentHealth)
    {
        if (segmentHealth >= 0.7f)
            return config.greenSprite;
        else if (segmentHealth >= 0.4f)
            return config.yellowSprite;
        else if (segmentHealth >= 0.1f)
            return config.redSprite;
        else
            return flashState ? config.redSprite : config.darkSprite;
    }

    private void Update()
    {
        if (flashTimer > 0)
        {
            flashState = Mathf.PingPong(Time.time / flashTimer, 1) > 0.5f;
            UpdateDangerState(GetCurrentHealthPercentage());
        }
    }

    private float GetCurrentHealthPercentage()
    {
        var healthController = GetComponentInParent<HealthController>();
        return healthController != null ? (healthController.CurrentHealth / healthController.MaxHealth) * 100f : 0f;
    }
}