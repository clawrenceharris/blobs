using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CartoonUI
{
    public class Close : MonoBehaviour
    {
        public void close()
        {
            gameObject.SetActive(false);
        }
    }
}
