using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class CollectibleSystem : MonoBehaviour
{
    public enum Tipo
    {
        Coletavel,
        Porta
    }

    [Header("Tipo do objeto")]
    [SerializeField] private Tipo tipo = Tipo.Coletavel;

    // =====================================================
    // COLETÁVEL
    // =====================================================

    [Header("Configuração do Coletável")]
    [SerializeField] private bool destruirAoColetar = true;

    // =====================================================
    // PORTA
    // =====================================================

    [Header("Configuração da Porta")]
    [SerializeField] private Collider2D colliderPorta;
    [SerializeField] private SpriteRenderer spritePorta;
    [SerializeField] private bool portaDestravaAutomaticamente = true;

    // =====================================================
    // ANIMAÇÃO DE FLUTUAR
    // =====================================================

    [Header("Animação de Flutuar")]
    [SerializeField] private bool ativarFlutuar = true;
    [SerializeField] private float velocidadeFlutuar = 1.5f;   // quão rápido sobe e desce
    [SerializeField] private float alturaFlutuar = 0.3f;       // distância que sobe/desce
    [SerializeField] private float defasagem = 0f;             // offset de fase (útil pra dessincronizar vários coletáveis)

    private Vector3 posicaoInicial;

    // =====================================================
    // UI
    // =====================================================

    [Header("Contador")]
    [SerializeField] private TMP_Text textoContador;

    // =====================================================
    // CONTROLE GLOBAL DA CENA
    // =====================================================

    private static int totalColetaveis;
    private static int coletados;
    private static bool cenaInicializada;

    // =====================================================
    // START
    // =====================================================

    private void Start()
    {
        posicaoInicial = transform.position;

        if (!cenaInicializada)
        {
            InicializarCena();
        }

        if (tipo == Tipo.Porta)
        {
            ConfigurarPorta();
        }

        AtualizarContador();
    }

    // =====================================================
    // INICIALIZAR CENA
    // =====================================================

    private void InicializarCena()
    {
        cenaInicializada = true;
        coletados = 0;

        CollectibleSystem[] objetos =
            FindObjectsByType<CollectibleSystem>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        totalColetaveis = 0;

        foreach (CollectibleSystem objeto in objetos)
        {
            if (objeto.tipo == Tipo.Coletavel)
                totalColetaveis++;
        }

        Debug.Log(
            $"[Coletáveis] Cena: {SceneManager.GetActiveScene().name} | " +
            $"Total: {totalColetaveis}"
        );
    }

    // =====================================================
    // TRIGGER DO COLETÁVEL
    // =====================================================

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (tipo != Tipo.Coletavel)
            return;

        if (!other.CompareTag("Player"))
            return;

        Coletar();
    }

    // =====================================================
    // COLETAR
    // =====================================================

    private void Coletar()
    {
        coletados++;

        Debug.Log($"[Coletável] Coletado! {coletados}/{totalColetaveis}");

        AtualizarContador();

        if (destruirAoColetar)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }

    // =====================================================
    // PORTA
    // =====================================================

    private void ConfigurarPorta()
    {
        if (colliderPorta == null)
            colliderPorta = GetComponent<Collider2D>();

        VerificarPorta();
    }

    // =====================================================
    // VERIFICAR PORTA
    // =====================================================

    private void VerificarPorta()
    {
        if (tipo != Tipo.Porta)
            return;

        bool liberada = coletados >= totalColetaveis;

        if (colliderPorta != null)
            colliderPorta.enabled = !liberada;

        if (liberada)
        {
            if (colliderPorta != null)
                colliderPorta.enabled = false;

            if (spritePorta != null)
                spritePorta.enabled = false;

            Debug.Log("[Coletáveis] Todos os coletáveis foram encontrados. Porta liberada!");
        }
    }

    // =====================================================
    // ATUALIZAR
    // =====================================================

    private void Update()
    {
        if (tipo == Tipo.Porta)
            VerificarPorta();

        AnimarFlutuar();
        AtualizarContador();
    }

    // =====================================================
    // ANIMAÇÃO FLUTUAR
    // =====================================================

    private void AnimarFlutuar()
    {
        if (!ativarFlutuar) return;

        // Seno vai de -1 a 1 suavemente, multiplicado pela altura desejada
        float offsetY = Mathf.Sin((Time.time + defasagem) * velocidadeFlutuar) * alturaFlutuar;
        transform.position = new Vector3(posicaoInicial.x, posicaoInicial.y + offsetY, posicaoInicial.z);
    }

    // =====================================================
    // CONTADOR UI
    // =====================================================

    private void AtualizarContador()
    {
        if (textoContador == null)
            return;

        textoContador.text = $"{coletados}/{totalColetaveis}";
    }

    // =====================================================
    // RESETAR AO TROCAR DE CENA
    // =====================================================

    private void OnDestroy()
    {
        if (gameObject.scene.isLoaded)
            return;

        cenaInicializada = false;
        coletados = 0;
        totalColetaveis = 0;
    }
}