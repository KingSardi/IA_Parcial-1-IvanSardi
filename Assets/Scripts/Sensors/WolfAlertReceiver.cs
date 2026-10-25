using UnityEngine;

public class WolfAlertReceiver : MonoBehaviour
{
    public bool HasAlert { get; private set; }

    public Vector3 AlertPosition { get; private set; }

    public void ReceiveAlert(Vector3 hunterPosition)
    {
        AlertPosition = hunterPosition;
        HasAlert = true;
    }

    public void ClearAlert()
    {
        HasAlert = false;
        AlertPosition = Vector3.zero;
    }
}