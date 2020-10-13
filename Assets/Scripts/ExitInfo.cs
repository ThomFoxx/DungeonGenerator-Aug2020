using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitInfo : MonoBehaviour
{
    public Vector3 offset;
    [SerializeField]
    private bool _validExit;

    private void OnEnable()
    {
        transform.gameObject.SetActive(!_validExit);
    }

    public void SetExit(bool isValid)
    {
        _validExit = isValid;
    }

    public bool ExitState()
    {
        return _validExit;
    }
}
