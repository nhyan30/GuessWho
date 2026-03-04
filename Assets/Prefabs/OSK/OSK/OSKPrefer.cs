using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UTool.OSK
{
    public class OSKPrefer : MonoBehaviour
    {
        [SerializeField] public OSKType type;

        public enum OSKType
        {
            En,
            EnAr,
            Email,
            NumPad
        }
    }
}