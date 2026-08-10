using UnityEngine;
using UnityEngine.UI;

public class LifeUI : MonoBehaviour
{
    [Header("Referência da Barra")]
    [SerializeField] private Image barraVida;
    private PlayerController playerController;

    private void Start()
    {
        // cache player reference
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            playerController = playerObj.GetComponent<PlayerController>();

        if (barraVida == null)
        {
            Debug.LogError("LifeUI: BarraVida não foi atribuída!");
            enabled = false;
            return;
        }

        if (playerController == null)
        {
            Debug.LogError("LifeUI: PlayerController não encontrado. As informações de vida virão do PlayerController.");
            enabled = false;
            return;
        }

        AtualizarBarra();
    }

    private void Update()
    {
        AtualizarBarra();
    }

    private void AtualizarBarra()
    {
        int vidaAtual = playerController.CurrentLife;
        int vidaMaxima = playerController.MaxLife;

        if (vidaMaxima <= 0)
            return;

        float ratio = Mathf.Clamp01((float)vidaAtual / vidaMaxima);

        // Prefer using Image.fillAmount if the Image is set to Filled
        if (barraVida.type == Image.Type.Filled)
        {
            barraVida.fillAmount = ratio;
        }
        else
        {
            // fallback: scale the rect transform horizontally
            var rt = barraVida.rectTransform;
            // advise on pivot for correct anchoring
            if (Mathf.Approximately(rt.pivot.x, 0f) == false)
            {
                Debug.LogWarning("LifeUI: BarraVida.rectTransform.pivot.x não está em 0. Para usar scaling corretamente, ajuste o pivot para (0,0.5) ou use Image.Type = Filled.");
            }

            rt.localScale = new Vector3(ratio, 1f, 1f);
        }
    }
}