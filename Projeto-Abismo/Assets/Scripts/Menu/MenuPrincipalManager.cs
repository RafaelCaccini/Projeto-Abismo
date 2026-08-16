using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPrincipalManager : MonoBehaviour
{
    [Header("CENA PRINCIPAL")]
    [SerializeField] private string nomeDaCena;

    [Header("FASES")]      
    [SerializeField] private string faseTutorial = "TutorialTeste";
    [SerializeField] private string fase1Junta = "Fase1Junta";
    [SerializeField] private string fase2Junta = "Fase2Junta";
    [SerializeField] private string fase3Junta = "";


    [Header("PAINÉIS")]
    [SerializeField] private GameObject painelMenuInicial;

    [SerializeField] private GameObject painelOpcoes;

    [SerializeField] private GameObject painelFases;

    // =====================================
    // JOGAR
    // =====================================

    public void IrParaOVideoCutscene()
    {
        SceneManager.LoadScene("IntroVideo");
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



    // FASE 2

    public void Tutorial()
    {
        Debug.Log("Indo para Tutorial");
        SceneManager.LoadScene(faseTutorial);
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

    // =====================================
    // FASES JUNTAS
    // =====================================

    public void IrParaFase1Junta()
    {
        Debug.Log("Indo para Fase1Junta");

        SceneManager.LoadScene(fase1Junta);
    }

    public void IrParaFase2Junta()
    {
        Debug.Log("Indo para Fase2Junta");

        SceneManager.LoadScene(fase2Junta);
    }
}