using UnityEngine;

namespace ProjectDualis.Avatar
{
    /// <summary>
    /// Creates a simple placeholder avatar when no VRM model is available.
    /// A basic robot/figure made from Unity primitives.
    /// </summary>
    public class PlaceholderAvatar : MonoBehaviour
    {
        [Header("Avatar Settings")]
        [SerializeField] private float bodySize = 1f;
        [SerializeField] private Color primaryColor = new Color(0.2f, 0.6f, 1f);
        [SerializeField] private Color secondaryColor = new Color(0.1f, 0.4f, 0.8f);

        private GameObject head;
        private GameObject body;
        private GameObject[] eyes = new GameObject[2];

        public static GameObject CreatePlaceholder(string name = "DualisAvatar")
        {
            GameObject avatar = new GameObject(name);
            PlaceholderAvatar placeholder = avatar.AddComponent<PlaceholderAvatar>();
            placeholder.BuildAvatar();
            return avatar;
        }

        private void BuildAvatar()
        {
            // Body (sphere/capsule)
            body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(transform);
            body.transform.localPosition = Vector3.up * 1f;
            body.transform.localScale = Vector3.one * bodySize;

            // Get body renderer and change color
            Renderer bodyRenderer = body.GetComponent<Renderer>();
            Material bodyMat = new Material(Shader.Find("Standard"));
            bodyMat.color = primaryColor;
            bodyRenderer.material = bodyMat;

            // Remove capsule collider
            Destroy(body.GetComponent<CapsuleCollider>());

            // Head (sphere)
            head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(transform);
            head.transform.localPosition = Vector3.up * (1.8f * bodySize);
            head.transform.localScale = Vector3.one * (0.5f * bodySize);

            Renderer headRenderer = head.GetComponent<Renderer>();
            Material headMat = new Material(Shader.Find("Standard"));
            headMat.color = secondaryColor;
            headRenderer.material = headMat;

            // Remove sphere collider
            Destroy(head.GetComponent<SphereCollider>());

            // Eyes (small spheres)
            CreateEye("LeftEye", new Vector3(-0.15f, 1.85f, 0.4f));
            CreateEye("RightEye", new Vector3(0.15f, 1.85f, 0.4f));

            // Add simple animation component
            SimpleAvatarAnimator animator = gameObject.AddComponent<SimpleAvatarAnimator>();
            animator.head = head.transform;
            animator.body = body.transform;
        }

        private void CreateEye(string name, Vector3 position)
        {
            GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.name = name;
            eye.transform.SetParent(transform);
            eye.transform.localPosition = position * bodySize;
            eye.transform.localScale = Vector3.one * (0.1f * bodySize);

            Renderer eyeRenderer = eye.GetComponent<Renderer>();
            Material eyeMat = new Material(Shader.Find("Standard"));
            eyeMat.color = Color.white;
            eyeRenderer.material = eyeMat;

            Destroy(eye.GetComponent<SphereCollider>());
        }

        /// <summary>
        /// Set emotion by changing colors
        /// </summary>
        public void SetEmotion(AvatarEmotion emotion, float intensity)
        {
            Color emotionColor = GetEmotionColor(emotion);

            if (body != null)
            {
                Renderer bodyRenderer = body.GetComponent<Renderer>();
                if (bodyRenderer.material != null)
                {
                    bodyRenderer.material.color = Color.Lerp(primaryColor, emotionColor, intensity * 0.5f);
                }
            }

            if (head != null)
            {
                Renderer headRenderer = head.GetComponent<Renderer>();
                if (headRenderer.material != null)
                {
                    headRenderer.material.color = Color.Lerp(secondaryColor, emotionColor, intensity * 0.7f);
                }
            }
        }

        private Color GetEmotionColor(AvatarEmotion emotion)
        {
            switch (emotion)
            {
                case AvatarEmotion.Joy:
                case AvatarEmotion.Excitement:
                    return Color.yellow;
                case AvatarEmotion.Sadness:
                    return Color.blue;
                case AvatarEmotion.Anger:
                    return Color.red;
                case AvatarEmotion.Love:
                    return Color.magenta;
                case AvatarEmotion.Surprise:
                    return new Color(1f, 0.5f, 0f);
                default:
                    return Color.white;
            }
        }
    }

    /// <summary>
    /// Simple idle animation for placeholder avatar
    /// </summary>
    public class SimpleAvatarAnimator : MonoBehaviour
    {
        public Transform head;
        public Transform body;

        private float idleTime = 0f;

        private void Update()
        {
            idleTime += Time.deltaTime;

            // Gentle floating motion
            if (body != null)
            {
                body.transform.localPosition = Vector3.up * (1f + Mathf.Sin(idleTime * 0.5f) * 0.05f);
            }

            // Subtle head movement
            if (head != null)
            {
                head.transform.localRotation = Quaternion.Euler(
                    Mathf.Sin(idleTime * 0.3f) * 5f,
                    Mathf.Sin(idleTime * 0.2f) * 5f,
                    0
                );
            }
        }
    }
}
