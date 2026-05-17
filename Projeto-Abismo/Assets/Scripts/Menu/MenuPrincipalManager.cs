using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPrincipalManager : MonoBehaviour
{
    [SerializeField] private string nomeDaCena;
    [SerializeField] private GameObject painelMenuInicial;
    [SerializeField] private GameObject painelOpcoes;

    public void Jogar()
    {
        SceneManager.LoadScene(nomeDaCena);
    }

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

    public void SairJogo()
    {
        Debug.Log("Saindo do jogo...(ELE SÓ FUNCIONAA COM O JOGO BUILDAADO!!!!!!!)");
        Application.Quit();
    }
}
