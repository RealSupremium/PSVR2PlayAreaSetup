using UnityEngine;

public class RoomFloor : MonoBehaviour
{
    public ChaperoneMesh chaperoneMesh;

    public void Update()
    {
        if (chaperoneMesh != null)
        {
            gameObject.transform.position = new Vector3(0.0f, chaperoneMesh.GetFloorHeight(), 0.0f);
        }
    }
}