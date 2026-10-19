
using UnityEngine;

public class FoodObject : CellObject
{
    [Header("Food Settings")]
    [SerializeField] private int amountGranted = 5;

    [Header("Particle Effects")]
    [SerializeField]
    private ParticleSystem foodCollectEffectPrefab;

    public override void PlayerEntered()
    {
        // 1. Increase the player's food.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddFood(amountGranted);
        }

        // 2. Play the food collection sound.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayFood();
        }

        // 3. Play particles using Object Pooling.
        if (ParticlePool.Instance != null &&
            foodCollectEffectPrefab != null)
        {
            ParticlePool.Instance.Play(
                foodCollectEffectPrefab,
                transform.position
            );
        }

        // 4. Remove collected food from the scene.
        Destroy(gameObject);
    }
}
