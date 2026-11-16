using UnityEngine;

namespace IA.Agents
{
    [DisallowMultipleComponent]
    public class AgentStateLabel : MonoBehaviour
    {
        private static readonly Color BackgroundColor = new Color(0f, 0f, 0f, 0.55f);

        [Tooltip("Altura (m) sobre el agente desde la que se ubica el texto.")]
        [SerializeField] private float heightOffset = 1.2f;
        [Tooltip("Píxeles extra hacia arriba para que el texto no tape al agente.")]
        [SerializeField] private float screenOffset = 22f;
        [SerializeField] private int fontSize = 11;
        [Tooltip("Margen (px) entre el texto y el borde del fondo.")]
        [SerializeField] private float padding = 3f;

        private IStateLabelSource source;
        private Camera cachedCamera;
        private GUIStyle style;
        private Texture2D backgroundTexture;

        private void Awake()
        {
            source = GetComponent<IStateLabelSource>();
        }

        private void OnDestroy()
        {
            if (backgroundTexture != null) Destroy(backgroundTexture);
        }

        private void OnGUI()
        {
            if (source == null) return;

            string text = source.StateLabel;
            if (string.IsNullOrEmpty(text)) return;
            if (!TryGetScreenPosition(out Vector2 screenPosition)) return;

            DrawLabel(text, screenPosition, source.StateLabelColor);
        }

        private bool TryGetScreenPosition(out Vector2 screenPosition)
        {
            screenPosition = Vector2.zero;

            if (cachedCamera == null) cachedCamera = Camera.main;
            if (cachedCamera == null) return false;

            Vector3 point = cachedCamera.WorldToScreenPoint(transform.position + Vector3.up * heightOffset);
            if (point.z <= 0f) return false;

            screenPosition = new Vector2(point.x, Screen.height - point.y - screenOffset);
            return true;
        }

        private void DrawLabel(string text, Vector2 center, Color color)
        {
            EnsureStyle();
            style.normal.textColor = color;

            Vector2 size = style.CalcSize(new GUIContent(text));
            Rect textRect = new Rect(center.x - size.x * 0.5f, center.y - size.y * 0.5f, size.x, size.y);
            Rect backgroundRect = new Rect(textRect.x - padding, textRect.y - padding, textRect.width + padding * 2f, textRect.height + padding * 2f);

            GUI.DrawTexture(backgroundRect, backgroundTexture);
            GUI.Label(textRect, text, style);
        }

        private void EnsureStyle()
        {
            if (style != null) return;

            style = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            backgroundTexture = new Texture2D(1, 1);
            backgroundTexture.SetPixel(0, 0, BackgroundColor);
            backgroundTexture.Apply();
        }
    }
}