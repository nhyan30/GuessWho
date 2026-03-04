using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

using UTool.Tweening;

using static UTool.OSK.Keyboard;

namespace UTool.OSK
{
    public class OSK : MonoBehaviour
    {
        public static OSK activeOSK;

        [SerializeField] private OSKPrefer.OSKType oskType;
        [SerializeField] private TweenElement boardTE;
        [SpaceArea]
        [SerializeField] public bool autoOpen = true;
        [SerializeField] public bool autoClose = true;
        [SpaceArea]
        [SerializeField] public TMP_InputField activeInputField;
        [SerializeField][Disable] private TMP_InputField selectedInputField;
        [SerializeField][Disable] private GameObject currentSelectedObject;
        [SpaceArea]
        [SerializeField] private Keyboard defaultKeyboard;
        [SpaceArea]
        [SerializeField] private Keyboard activeKeyboard;
        [SerializeField] private Keyboard swapKeyboard;
        [SerializeField] private Keyboard numberKeyboard;
        [SpaceArea]
        [SerializeField] private UnityEvent<string> OnSubmit = new UnityEvent<string>();

        private void Update()
        {
            if (currentSelectedObject != EventSystem.current.currentSelectedGameObject)
            {
                currentSelectedObject = EventSystem.current.currentSelectedGameObject;

                if (currentSelectedObject)
                {
                    if (selectedInputField)
                        if (currentSelectedObject == selectedInputField.gameObject)
                            return;

                    TMP_InputField inputField = currentSelectedObject.GetComponent<TMP_InputField>();

                    if (inputField)
                    {
                        OSKPrefer oskPreferType = currentSelectedObject.GetComponent<OSKPrefer>();

                        if (oskPreferType)
                        {
                            if (oskPreferType.type != oskType)
                            {
                                Close();
                                return;
                            }
                        }
                        else
                        {
                            if (oskType != OSKPrefer.OSKType.En)
                                return;
                        }

                        selectedInputField = inputField;

                        if (autoOpen)
                            Open();
                    }
                }
            }
        }

        public void Open()
        {
            boardTE.PlayTween();
        }

        public void Close()
        {
            currentSelectedObject = null;
            selectedInputField = null;

            boardTE.ReverseTween(() =>
            {
                if (activeKeyboard != defaultKeyboard)
                {
                    activeKeyboard.Close();
                    swapKeyboard = activeKeyboard;
                    activeKeyboard = defaultKeyboard;
                }

                if (numberKeyboard)
                    numberKeyboard.Close();
                activeKeyboard.Open();
            });
        }

        public TMP_InputField GetField()
        {
            TMP_InputField field = activeInputField;

            if (!field)
                field = selectedInputField;

            return field;
        }

        public void OnKeyPress(KeyType keyType, string keyValue)
        {
            TMP_InputField field = GetField();
            if (field == null) return;

            switch (keyType)
            {
                case KeyType.Char:
                    field.text += keyValue;
                    break;

                case KeyType.Backspace:
                    if (field.text.Length > 0)
                        field.text = field.text.Remove(field.text.Length - 1, 1);
                    break;

                case KeyType.Clear:                // ← NEW CASE
                    field.text = "";
                    break;

                case KeyType.Submit:
                    OnSubmit?.Invoke(field.text);
                    if (autoClose) Close();
                    break;

                case KeyType.NumberBoard:
                    activeKeyboard.Toggle();
                    numberKeyboard.Toggle();
                    break;

                case KeyType.SwitchBoard:
                    activeKeyboard.Toggle();
                    swapKeyboard.Toggle();
                    var swapK = swapKeyboard;
                    swapKeyboard = activeKeyboard;
                    activeKeyboard = swapK;
                    break;

                case KeyType.None:
                default:
                    break;
            }
        }

        public void ClearText()
        {
            var field = GetField();
            if (field != null)
            {
                field.text = "";
            }
        }
    }
}