using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using RTLTMPro;

using UTool;
using UTool.Utility;
using UTool.Tweening;

using DG.Tweening;
using TNRD.Autohook;

using static UTool.OSK.Keyboard;

namespace UTool.OSK
{
    public class Key : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField][BeginGroup][AutoHook(SearchArea = AutoHookSearchArea.Parent)] private Keyboard keyboard;
        [SpaceArea]
        [SerializeField] private TweenElement feedbackTE;
        [SerializeField] private RTLTextMeshPro keyValueField;
        [SpaceArea]
        [SerializeField] private Image backgroundImage;
        [SpaceArea]
        [SerializeField] private Color color;
        [SerializeField] private Color disabledColor;
        [SerializeField] private Color pressTint;
        [SerializeField][EndGroup] private float duration;
        [SpaceArea]
        [SerializeField] private KeyType type;
        [SerializeField] private string value = "a";

        private bool canInteract => type != KeyType.None;

        private PointerEventData activePointer;

        private Tween colorTween;

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            if (activePointer != null)
                return;

            activePointer = eventData;
            Pressed();
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
        {
            if (activePointer == null)
                return;

            if (activePointer != eventData)
                return;

            activePointer = null;
            Released();
        }

        private void Pressed()
        {
            if (!canInteract)
                return;

            feedbackTE.PlayTween();

            colorTween.KillTween();
            colorTween = backgroundImage.DOColor((color + pressTint), duration)
                .SetEase(Ease.InOutQuad);

            keyboard.OnKeyPress(type, value);
        }

        private void Released()
        {
            if (!canInteract)
                return;

            feedbackTE.ReverseTween();

            colorTween.KillTween();
            colorTween = backgroundImage.DOColor(color, duration)
                .SetEase(Ease.InOutQuad);
        }

        private void OnValidate()
        {
            backgroundImage.color = type == KeyType.None ? disabledColor : color;

            //if (UUtility.IsPrefabSceneView())
            //    return;

            keyValueField.text = value;
            name = $"{value}_Key";
        }
    }
}