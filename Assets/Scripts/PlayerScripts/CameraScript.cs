using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraScript : MonoBehaviour
{
    [SerializeField]
    private float sensitivity = 1f;
    [SerializeField]
    private float alphaSpeed = 1.5f;
    [SerializeField]
    private float transparencyDistance = 5f;

    private Cinemachine.CinemachineFreeLook cam;
    private Camera mainCam;
    private GameObject player;


    private void Start()
    {
        cam = GetComponent<Cinemachine.CinemachineFreeLook>();
        player = transform.parent.gameObject;
        mainCam = transform.parent.GetComponentInChildren<Camera>();
    }
    void LateUpdate()
    {
        Vector2 input = Input.mouseScrollDelta; 
        if(!(input.y < 0 && ((cam.m_Orbits[1].m_Radius + input.y) < cam.m_Orbits[0].m_Radius || (cam.m_Orbits[1].m_Radius + input.y) < cam.m_Orbits[2].m_Radius)))
        {
            cam.m_Orbits[1].m_Radius += input.y * sensitivity;
            cam.m_Orbits[0].m_Height += input.y * sensitivity;
        }
        if ((player.transform.position - mainCam.transform.position).magnitude < transparencyDistance)
        {
            Color c = player.GetComponent<MeshRenderer>().material.color;
            c.a -= alphaSpeed * Time.deltaTime;
            if(c.a < 0.1f)
            {
                c.a = 0.1f;
            }
            player.GetComponent<MeshRenderer>().material.color = c;
        }
        else
        {
            Color c = player.GetComponent<MeshRenderer>().material.color;
            c.a += alphaSpeed * Time.deltaTime;
            if (c.a > 1f)
            {
                c.a = 1f;
            }
            player.GetComponent<MeshRenderer>().material.color = c;
            
        }
    }
}
