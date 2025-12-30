// Unity 5.6 / C# 4.0

using System;

using Multimorphic.P3App.GUI;
using UnityEngine;
using UnityEngine.UI;

namespace Packages.BMG.Misc
{
    // ReSharper disable once InconsistentNaming - Acceptable Acronym
    public class BMGBackglassOverlay : MonoBehaviour
    {
        [Tooltip("Sets all images on game object \"BackboxImage\" to alpha 0 on Enable and alpha 1 on Disable.")]
        [SerializeField] private bool m_setBackboxImageToTransparent;

        /// <summary>
        /// Hides the <see cref="Multimorphic.P3App.GUI.BackboxImage"/>'s image.'/>
        /// </summary>
        private void Awake()
        {
            DontDestroyOnLoad(this.gameObject);
        }

        private void OnEnable()
        {
            SetAlphaOnBackgroundImages(true);
        }

        private void OnDisable()
        {
            SetAlphaOnBackgroundImages(false);
        }

        private void SetAlphaOnBackgroundImages(bool setAlpha)
        {
            try
            {
                BackboxImage backboxImage = FindObjectOfType<BackboxImage>();
                if (backboxImage != null || backboxImage.gameObject != null)
                {
                    Image[] images = backboxImage.gameObject.GetComponentsInChildren<Image>();
                    for (int i = 0; i < images.Length; i++)
                    {
                        if (images[i] != null)
                        {
                            Debug.Log("Changing alpha image in game object \"" + images[i].gameObject.name + "\"");
                            Color color = images[i].color;
                            color.a = setAlpha ? 0 : 1;
                            images[i].color = color;
                        }
                    }
                }
                else
                {
                    Debug.Log("Could not find a game object with component \"BackboxImage\" on it to hide.");
                }
            }
            catch(Exception e)
            {
                Debug.Log("Could not find a game object with component \"BackboxImage\" on it to hide.");
            }
        }
    }
}