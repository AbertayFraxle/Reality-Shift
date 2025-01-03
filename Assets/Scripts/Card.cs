using Oculus.Interaction;
using UnityEngine;

public class Card : MonoBehaviour
{

    Texture2D cardImage;
    string cardName;

    private bool Locked = true;

    public Vector3 lockPos;

    public bool onStack = false;

    private void Awake()
    {
        Locked = true;
    }

    private void Start()
    {
       //grabbable = GetComponent<Grabbable>();
    }

    private void Update()
    {

    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnTriggerEnter(Collider other)
    {
        //

        
    }

    private void OnTriggerStay(Collider other)
    {
        if (!Locked)
        {

                if (other.gameObject.tag == "Surface")
                {

                    Quaternion newRot = Quaternion.Euler(-90, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);

                    Vector3 newPos = new Vector3(transform.position.x, other.transform.position.y, transform.position.z);

                    transform.SetPositionAndRotation(newPos, newRot);
                }
            
        }
    }

    public void SetLocked(bool nLocked)
    {
        Locked = nLocked;
    }
}
