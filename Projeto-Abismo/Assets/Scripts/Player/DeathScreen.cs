using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class DeathScreen : MonoBehaviour
{
    public static DeathScreen instance;

    [SerializeField]
    private GameObject painelMorte;

    [SerializeField]
    private string cenaMenu = "Menu";

    private void Awake()
    {
        instance = this;

        painelMorte.SetActive(false);
    }

    public void MostrarTelaMorte()
    {
        painelMorte.SetActive(true);

        StartCoroutine(PausarJogo());
    }

    IEnumerator PausarJogo()
    {
        yield return new WaitForSecondsRealtime(0.1f);

        Time.timeScale = 0f;
    }

    public void Reiniciar()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }

    public void IrMenu()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(cenaMenu);
    }

}