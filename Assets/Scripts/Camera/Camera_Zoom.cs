using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camera_Zoom : MonoBehaviour
{

    [SerializeField] private float _scrollSpeed = 2f;

    [SerializeField] private float _minZoom, _maxZoom;

    [SerializeField] private Transform target;
    [SerializeField]
    private Camera _cameraMain, _cameraSecondary;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float distFromTarget = Vector3.Distance(transform.position, target.position);
        float tempDist = 2;
        
        if (Input.GetAxis("Mouse ScrollWheel") > 0 && distFromTarget > _minZoom) // Zoom In
        {
            float CameraMove = _scrollSpeed * Time.deltaTime * 10f ; 
            transform.position = Vector3.MoveTowards(transform.position, target.position, CameraMove);
        }
        else if (Input.GetAxis("Mouse ScrollWheel") < 0 && distFromTarget < _maxZoom) // Zoom out
        {
            float CameraMove = _scrollSpeed * Time.deltaTime * 10f ;
            transform.position = Vector3.MoveTowards(transform.position, target.position, -CameraMove);
        }
        else if (Input.GetAxis("Mouse ScrollWheel") > 0 && distFromTarget <= _minZoom) // Third to First
        {
            tempDist = distFromTarget;
            _cameraMain.enabled = false;
            _cameraSecondary.enabled = true;

        }
        else if (distFromTarget > tempDist) // First to Third
        {
            _cameraMain.enabled = true;
            _cameraSecondary.enabled = false;
        }

    }
}
