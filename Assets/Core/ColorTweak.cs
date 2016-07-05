using System;
using UnityEngine;
using System.Collections;
using ProceduralToolkit;
using System.Collections.Generic;

namespace Assets.Core
{
    class ColorTweak : MonoBehaviour
    {
        public Renderer rend;
        private Color myColor = new Color(0,0,0,0);

        void Start()
        {
            myColor = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
            rend = this.GetComponent<Renderer>();
            rend.material.shader = Shader.Find("Unlit/WorldSpaceNormals");//Gets the Shader
            rend.material.SetColor("_colorBalance", myColor);
            
        }

        void Update()
        {

        }
    }
}
