// Copyright (c) Meta Platforms, Inc. and affiliates.



using UnityEngine;

using UnityEngine.EventSystems;



namespace PassthroughCameraSamples.MainApp

{

    /// <summary>

    /// DebugUIBuilder Content 패널에 붙여서 레이저가 메뉴 밖으로 나갔는지 감지합니다.

    /// </summary>

    public class MenuHoverTracker : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler

    {

        public bool IsPointerInside { get; private set; }

        public bool HasBeenInside { get; private set; }



        public void ResetTracking()

        {

            IsPointerInside = false;

            HasBeenInside = false;

        }



        public void OnPointerEnter(PointerEventData eventData)

        {

            IsPointerInside = true;

            HasBeenInside = true;

        }



        public void OnPointerExit(PointerEventData eventData)

        {

            IsPointerInside = false;

        }

    }

}


