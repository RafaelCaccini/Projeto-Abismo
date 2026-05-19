using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPrincipalManager : MonoBehaviour
{
    [Header("CENA PRINCIPAL")]
    [SerializeField] private string nomeDaCena;

    [Header("FASES")]
    [SerializeField] private string fase1 = "Parte1";

    [SerializeField] private string fase2 = "Parte2";

    [SerializeField] private string fase3 = "Parte3";

    [SerializeField] private string fase4 = "Parte4";

    [SerializeField] private string fase5 = "Parte5";           


    [Header("PAINÉIS")]
    [SerializeField] private GameObject painelMenuInicial;

    [SerializeField] private GameObject painelOpcoes;

    [SerializeField] private GameObject painelFases;

    // =====================================
    // JOGAR
    // =====================================

    public void Jogar()
    {
        SceneManager.LoadScene(nomeDaCena);
    }

    // =====================================
    // FASES
    // =====================================

    public void AbrirFases()
    {
        painelMenuInicial.SetActive(false);

        painelFases.SetActive(true);
    }

    public void FecharFases()
    {
        painelFases.SetActive(false);

        painelMenuInicial.SetActive(true);
    }

    // =====================================
    // CARREGAR FASES
    // =====================================

    public void IrParaParte1()
    {
        Debug.Log("Indo para Parte1");

        SceneManager.LoadScene(fase1);
    }

    public void IrParaParte2()
    {
        Debug.Log("Indo para Parte2");

        SceneManager.LoadScene(fase2);
    }

    public void IrParaParte3()
    {
        Debug.Log("Indo para Parte3");

        SceneManager.LoadScene(fase3);
    }

    public void IrParaParte4()
    {
        Debug.Log("Indo para Parte4");

        SceneManager.LoadScene(fase4);
    }

    public void IrParaParte5()
    {
        Debug.Log("Indo para Parte5");
        SceneManager.LoadScene(fase5);
    }

    // =====================================
    // OPÇÕES
    // =====================================

    public void AbrirOpcoes()
    {
        painelMenuInicial.SetActive(false);

        painelOpcoes.SetActive(true);
    }

    public void FecharOpcoes()
    {
        painelOpcoes.SetActive(false);

        painelMenuInicial.SetActive(true);
    }

    // =====================================
    // SAIR
    // =====================================

    public void SairJogo()
    {
        Debug.Log(
            "Saindo do jogo... (só funciona buildado)"
        );

        Application.Quit();
    }
}