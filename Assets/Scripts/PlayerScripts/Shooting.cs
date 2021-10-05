using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shooting : MonoBehaviour
{
    [SerializeField]
    private GameObject bullet;
    private GameObject player;
    private Camera cam;
    private void Start()
    {
        player = transform.gameObject;
        cam = GetComponentInChildren<Camera>();
    }
    // Update is called once per frame
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width/2,Screen.height/2));
            RaycastHit hit;
            GameObject newBullet;
            if(Physics.Raycast(ray, out hit,100)){
                var worldPos = hit.point;
                var angle = Vector3.Angle(transform.position,worldPos);
                newBullet = Instantiate(bullet, transform.position, Quaternion.LookRotation(worldPos-transform.position));
                Debug.Log("hit");
            }else{
                newBullet = Instantiate(bullet, transform.position, Quaternion.LookRotation(ray.direction));
                Debug.Log("Not hit");
            }
            Physics.IgnoreCollision(newBullet.GetComponent<Collider>(), player.GetComponent<Collider>(), true);
        }
     
    }
}
