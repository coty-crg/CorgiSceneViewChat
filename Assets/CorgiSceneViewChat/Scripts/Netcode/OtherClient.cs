using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CorgiSceneChat
{
    [System.Serializable]
    public class OtherClient
    {
        public int ClientId;

        public int GizmoMode; 
        public Vector3 GizmoPosition;
        public Quaternion GizmoRotation;
        public Vector3 GizmoScale;
        public string SelectedTransformStr;
    }
}
