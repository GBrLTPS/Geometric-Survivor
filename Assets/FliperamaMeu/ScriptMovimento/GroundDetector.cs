using UnityEngine;

public class GroundTriggerDetector : MonoBehaviour
{
    public bool isGrounded { get; private set; }
    private int triggerCount = 0;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Ignora Triggers, o próprio jogador e projéteis
        if (other.isTrigger) return;
        if (other.transform.IsChildOf(transform.parent)) return;
        if (other.GetComponentInParent<ArcadeEnemy>() != null) return;
        if (other.GetComponent<ArcadeProjectile>() != null) return;

        triggerCount++;
        isGrounded = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.isTrigger) return;
        if (other.transform.IsChildOf(transform.parent)) return;
        if (other.GetComponentInParent<ArcadeEnemy>() != null) return;
        if (other.GetComponent<ArcadeProjectile>() != null) return;

        triggerCount = Mathf.Max(0, triggerCount - 1);
        if (triggerCount == 0)
        {
            isGrounded = false;
        }
    }
}