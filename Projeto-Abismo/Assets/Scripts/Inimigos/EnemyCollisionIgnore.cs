using UnityEngine;

public class EnemyCollisionIgnore : MonoBehaviour
{
    void Start()
    {
        GameObject[] enemies =
            GameObject.FindGameObjectsWithTag(
                "Enemy"
            );

        for (int i = 0; i < enemies.Length; i++)
        {
            Collider2D colA =
                enemies[i]
                .GetComponent<Collider2D>();

            if (colA == null)
                continue;

            for (int j = i + 1; j < enemies.Length; j++)
            {
                Collider2D colB =
                    enemies[j]
                    .GetComponent<Collider2D>();

                if (colB == null)
                    continue;

                Physics2D.IgnoreCollision(
                    colA,
                    colB,
                    true
                );
            }
        }

        Debug.Log(
            "Colisão entre inimigos ignorada"
        );
    }
}