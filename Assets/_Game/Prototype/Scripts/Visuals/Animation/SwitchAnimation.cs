using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchAnimation : MonoBehaviour
{
    private Animator animator;
    
    private void Awake() {
        animator = GetComponent<Animator>();
        
    }

    public void SwitchOff(){
        animator.SetBool("isOff", true);
        animator.SetBool("isOn", false);
    }

    public void SwitchOn(){
        animator.SetBool("isOn", true);
        animator.SetBool("isOff", false);
    }


    
}
