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
    
    [SerializeField] private string fase21 = "Parte2.1";
    [SerializeField] private string fase22 = "Parte2.2";
    [SerializeField] private string fase23 = "Parte2.3";
    [SerializeField] private string fase24 = "Parte2.4";



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

    // FASE 2

    public void IrParaParte21()
    {
        Debug.Log("Indo para Parte2.1");
        SceneManager.LoadScene(fase21);
    }

    public void IrParaParte22()
    {
        Debug.Log("Indo para Parte2.2");
        SceneManager.LoadScene(fase22);
    }

    public void IrParaParte23()
    {
        Debug.Log("Indo para Parte2.3");
        SceneManager.LoadScene(fase23);
    }

    public void IrParaParte24()
    {
        Debug.Log("Indo para Parte2.4");
        SceneManager.LoadScene(fase24);
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