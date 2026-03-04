using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace UTool.OSK
{
    public class MultiOSK : MonoBehaviour
    {
        [SerializeField] public UnityEvent OnSubmit;

        public void Submit()
        {
            OnSubmit?.Invoke();
        }
    }
}