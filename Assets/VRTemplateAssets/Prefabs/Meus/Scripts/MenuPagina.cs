using UnityEngine;

public class MenuPaginas : MonoBehaviour
{
    public bool MostrarPagina(string nomePagina)
    {
        // Primeiro verifica se existe alguma página com esse nome
        Transform paginaEncontrada = null;
        foreach (Transform pagina in transform)
        {
            if (pagina.name == nomePagina)
            {
                paginaEncontrada = pagina;
                break;
            }
        }

        // Se não encontrou, não mexe em nada
        if (paginaEncontrada == null)
            return false;

        // Se encontrou, ativa só ela e desativa as outras
        foreach (Transform pagina in transform)
        {
            pagina.gameObject.SetActive(pagina == paginaEncontrada);
        }

        return true;
    }
}