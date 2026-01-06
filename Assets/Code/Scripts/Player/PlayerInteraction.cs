using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    DialogManager currentDialog;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("NPC"))
        {
            currentDialog = other.GetComponent<DialogManager>();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (currentDialog != null &&
            other.gameObject == currentDialog.gameObject)
        {
            currentDialog = null;
        }
    }

    public bool GetIsAction()
    {
        if (currentDialog)
            return currentDialog.isAction;

        return false;
    }

    void OnInteract(InputValue value)
    {
        if (currentDialog != null)
            currentDialog.Action();
    }
}
