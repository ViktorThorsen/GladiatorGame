using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraShakeManager : MonoBehaviour
{
    [SerializeField] private float globalShakerForce = 1f;
    [SerializeField] private CinemachineImpulseSource impulseSource;

    public void CameraShake()
    {
        impulseSource.GenerateImpulseWithForce(globalShakerForce);
    }

}
