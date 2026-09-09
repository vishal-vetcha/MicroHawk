using UnityEngine;

namespace MicroHawk.Presentation
{
    /// <summary>Moves cameras only. Has no drone control or world-registry dependency.</summary>
    public sealed class FacilityCameras : MonoBehaviour
    {
        [SerializeField] private Camera overview;
        [SerializeField] private Camera follow;
        [SerializeField] private Transform observedDrone;
        [SerializeField] private Vector3 followOffset = new(1.8f, 1.1f, -2.1f);
        public bool IsOverview => overview.enabled;
        public Camera Overview => overview;
        public Camera Follow => follow;
        private GUIStyle title, subtitle, note;

        private void Awake() => ShowOverview(true);

        public void ShowOverview(bool showOverview)
        {
            overview.enabled = showOverview;
            follow.enabled = !showOverview;
            var a = overview.GetComponent<AudioListener>();
            var b = follow.GetComponent<AudioListener>();
            if (a) a.enabled = showOverview;
            if (b) b.enabled = !showOverview;
        }

        private void LateUpdate()
        {
            if (!observedDrone) return;
            follow.transform.position = observedDrone.position + followOffset;
            follow.transform.LookAt(observedDrone.position + Vector3.up * 0.05f);
        }

        private void OnGUI()
        {
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.Tab) { ShowOverview(!IsOverview); Event.current.Use(); }
                else if (Event.current.keyCode == KeyCode.Alpha1) ShowOverview(true);
                else if (Event.current.keyCode == KeyCode.Alpha2) ShowOverview(false);
            }
            title ??= new GUIStyle(GUI.skin.label) { fontSize = 25, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            subtitle ??= new GUIStyle(GUI.skin.label) { fontSize = 12, normal = { textColor = new Color(0.4f, 0.85f, 0.91f) } };
            note ??= new GUIStyle(GUI.skin.label) { fontSize = 12, normal = { textColor = new Color(0.8f, 0.85f, 0.89f) } };
            GUI.Box(new Rect(18, 18, 355, 110), GUIContent.none);
            GUI.Label(new Rect(32, 26, 320, 35), "MICROHAWK", title);
            GUI.Label(new Rect(33, 62, 320, 22), "INDUSTRIAL TEST FACILITY  /  M1 FLIGHT", subtitle);
            if (GUI.Button(new Rect(32, 91, 152, 25), "1  Overview")) ShowOverview(true);
            if (GUI.Button(new Rect(194, 91, 163, 25), "2  Drone follow")) ShowOverview(false);
            GUI.Box(new Rect(18, Screen.height - 47, Screen.width - 36, 30), GUIContent.none);
            GUI.Label(new Rect(30, Screen.height - 43, Screen.width - 60, 25), "TAB  Switch camera     |     Typed commands • Deterministic safety • Scene props are not detections", note);
        }

#if UNITY_EDITOR
        public void ConfigureForAuthoring(Camera overviewCamera, Camera followCamera, Transform drone)
        {
            overview = overviewCamera;
            follow = followCamera;
            observedDrone = drone;
            ShowOverview(true);
        }
#endif
    }
}


