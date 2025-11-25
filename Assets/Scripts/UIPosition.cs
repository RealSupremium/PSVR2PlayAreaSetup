using UnityEngine;

public class UIPosition : MonoBehaviour
{
    public Transform head;

    void Update()
    {
        // Slowly move the UI element to follow 1 meter in front the head position
        Vector3 forwardNoY = head.forward;
        forwardNoY.y = 0;
        forwardNoY.Normalize();
        Vector3 targetPosition = head.position + forwardNoY * 2.0f;
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 2.0f);
        transform.LookAt(head.position);
        transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
    }
}
