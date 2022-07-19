using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class avatarRenderer : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

        

    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            //if (transform.GetChild(i).name == "CombinedAvatar" || transform.GetChild(i).name == "GLTF:Scene(Clone)")
            //{
            //    transform.GetChild(i).gameObject.SetActive(false);
            //}
            if (transform.GetChild(i).name == "LoadingAvatarContainer"|| transform.GetChild(i).name == "RotatingTemplates" || transform.GetChild(i).name == "BaseFemale9" || transform.GetChild(i).name == "Armature")
            {
                transform.GetChild(i).gameObject.SetActive(true);
            }
            else
            {
                transform.GetChild(i).gameObject.SetActive(false);
            }
        }
    }
}
