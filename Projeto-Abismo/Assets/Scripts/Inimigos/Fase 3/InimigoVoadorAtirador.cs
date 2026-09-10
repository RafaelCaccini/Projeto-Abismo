using UnityEngine;

// =========================================================
// SCRIPT ÚNICO DO INIMIGO VOADOR
// =========================================================
public class InimigoVoador : MonoBehaviour
{
    [Header("Configurações do Disparo")]
    [SerializeField] private GameObject prefabProjetil;
    [SerializeField] private Transform pontoDisparo;
    [SerializeField] private float intervaloAtaque = 2f;
    [SerializeField] private float velocidadeProjetil = 10f;
    [SerializeField] private int danoProjetil = 1;

    [Header("Detecção")]
    [SerializeField] private float alcanceVisao = 12f;

    [Header("Vida")]
    [SerializeField] private int vidaMaxima = 1;
    [SerializeField] private int danoDash = 1;

    private int vidaAtual;
    private Transform playerTransform;
    private float cronometroAtaque;

    private void Start()
    {
        vidaAtual = vidaMaxima;

        // Encontra o Player na cena pela Tag
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
        else
        {
            Debug.LogWarning($"[{name}] Nenhum GameObject com a tag 'Player' foi encontrado na cena. O inimigo não vai atirar até isso ser corrigido.");
        }

        // Tenta achar o PontoDisparoAtirador nos filhos se não for atribuído no Inspector
        if (pontoDisparo == null)
        {
            Transform filho = transform.Find("PontoDisparoAtirador");
            pontoDisparo = filho != null ? filho : transform;
        }

        if (prefabProjetil == null)
        {
            Debug.LogWarning($"[{name}] O campo 'Prefab Projetil' está vazio no Inspector. Arraste o prefab do tiro para esse campo.");
        }
    }

    private void Update()
    {
        if (playerTransform == null) return;

        // Distância até o jogador
        float distancia = Vector2.Distance(transform.position, playerTransform.position);

        if (distancia <= alcanceVisao)
        {
            cronometroAtaque += Time.deltaTime;

            if (cronometroAtaque >= intervaloAtaque)
            {
                Atirar();
                cronometroAtaque = 0f;
            }
        }
    }

    private void Atirar()
    {
        if (prefabProjetil == null) return; // já avisado no Start()

        // Direção reta apontando para a posição atual do Player
        Vector2 direcao = (playerTransform.position - pontoDisparo.position).normalized;

        // Instancia o Prefab do tiro
        GameObject projetilObj = Instantiate(prefabProjetil, pontoDisparo.position, Quaternion.identity);

        // Adiciona e configura o componente de movimento do tiro no próprio objeto instanciado
        ComportamentoProjetil tiro = projetilObj.GetComponent<ComportamentoProjetil>();
        if (tiro == null)
        {
            tiro = projetilObj.AddComponent<ComportamentoProjetil>();
        }

        tiro.Inicializar(direcao, velocidadeProjetil, danoProjetil);
    }

    // =========================================================
    // DETECÇÃO DE MORTE PELO DASH
    // =========================================================

    public void TomarDano(int quantidade = 1)
    {
        vidaAtual -= quantidade;

        if (vidaAtual <= 0)
        {
            Morrer();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerController player = collision.GetComponent<PlayerController>();

            // O dash agora dá dano em vez de matar na hora — precisa de vários hits conforme a vida
            if (player != null && player.IsDashing)
            {
                TomarDano(danoDash);
            }
        }
    }

    private void Morrer()
    {
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, alcanceVisao);
    }
}

// =========================================================
// COMPORTAMENTO INTERNO DO TIRO (Sem arquivo separado)
// =========================================================
public class ComportamentoProjetil : MonoBehaviour
{
    private Vector2 direcao;
    private float velocidade;
    private int dano;

    public void Inicializar(Vector2 novaDirecao, float novaVelocidade, int novoDano)
    {
        direcao = novaDirecao;
        velocidade = novaVelocidade;
        dano = novoDano;
    }

    private void Update()
    {
        transform.position += (Vector3)(direcao * velocidade * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerController player = collision.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(dano, gameObject);
            }

            Destroy(gameObject);
            return;
        }

        if (collision.gameObject.layer == LayerMask.NameToLayer("Default"))
        {
            Destroy(gameObject);
        }
    }
}