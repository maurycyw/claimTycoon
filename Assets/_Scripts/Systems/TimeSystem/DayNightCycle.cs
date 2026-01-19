using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ClaimTycoon.Systems.TimeSystem
{
    [RequireComponent(typeof(Light))]
    public class DayNightCycle : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Light sunLight;

        [Header("Lighting Settings")]
        [SerializeField] private Gradient sunColor;
        [SerializeField] private Gradient ambientColor;
        [SerializeField] private AnimationCurve sunIntensity;
        [SerializeField] private float maxSunIntensity = 1.0f;
        
        [Header("Shadow Settings")]
        [SerializeField] private float shadowDistance = 150f; // Single High-Quality Map
        [SerializeField] private float shadowBias = 0.05f; // Standard bias
        [SerializeField] private float shadowNormalBias = 0.4f;
        
        [Tooltip("Angle of the sun relative to the North (Y rotation)")]
        [SerializeField] private float sunYRotation = 170f;

        private void Reset()
        {
            sunLight = GetComponent<Light>();
            
            // Setup default gradients if possible or leave empty. 
            // It's hard to procedurally generate nice Gradients in Reset without a lot of code,
            // but we can ensure the curve is sensible.
            sunIntensity = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.2f, 0f), // Night
                new Keyframe(0.25f, 0.5f), // Sunrise starts
                new Keyframe(0.5f, 1f), // Noon
                new Keyframe(0.75f, 0.5f), // Sunset ends
                new Keyframe(0.8f, 0f), // Night
                new Keyframe(1f, 0f)
            );
        }

        private void Start()
        {
            if (sunLight == null) sunLight = GetComponent<Light>();
            
            // Fix Shadow Artifacts: The "Rings" are Cascade Splits.
            // URP ignores QualitySettings.shadowCascades. We must set it on the Asset.
            UniversalRenderPipelineAsset urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset != null)
            {
                Debug.Log($"[DayNightCycle] Forcing URP Asset to 1 Cascade. Old count: {urpAsset.shadowCascadeCount}");
                urpAsset.shadowCascadeCount = 1;
                urpAsset.shadowDistance = shadowDistance;
            }
            else
            {
                // Fallback for non-URP or if asset is missing (shouldn't happen in URP project)
                QualitySettings.shadowCascades = 1; 
                QualitySettings.shadowDistance = shadowDistance;
            }

            QualitySettings.shadowResolution = UnityEngine.ShadowResolution.VeryHigh;
            QualitySettings.shadowProjection = ShadowProjection.StableFit; // Reduces shimmering

            // Ensure the Sun object itself (if it has a debug mesh) doesn't cast shadows
            MeshRenderer mr = GetComponent<MeshRenderer>();
            if (mr != null) mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Apply Shadow Settings
            ApplyShadowSettings();
            
            // Restore Fog
            RenderSettings.fog = true;

            // Fallback defaults if not assigned in Inspector
            if (sunColor == null || sunColor.colorKeys.Length == 0) SetupDefaultSunGradient();
            if (ambientColor == null || ambientColor.colorKeys.Length == 0) SetupDefaultAmbientGradient();
            if (sunIntensity == null || sunIntensity.length == 0) Reset();
        }

        private void ApplyShadowSettings()
        {
             sunLight.shadowBias = shadowBias; 
             sunLight.shadowNormalBias = shadowNormalBias;
        }

        private void Update()
        {
            if (TimeManager.Instance == null) return;

            float time = TimeManager.Instance.NormalizedTime; // 0.0 to 1.0

            UpdateSunPosition(time);
            UpdateLighting(time);
        }

        private void UpdateSunPosition(float time)
        {
            // Rotate the sun 360 degrees around the X axis according to time
            // At 0.25 (6:00), we want 0 degrees (Horizon rise)
            // At 0.50 (12:00), we want 90 degrees (Zenith)
            // Formula: (Time * 360) - 90
            float rotX = (time * 360f) - 90f;
            
            sunLight.transform.localRotation = Quaternion.Euler(rotX, sunYRotation, 0);
        }

        private void UpdateLighting(float time)
        {
            // Update Colors
            if (sunColor != null)
                sunLight.color = sunColor.Evaluate(time);
            
            if (ambientColor != null)
                RenderSettings.ambientLight = ambientColor.Evaluate(time);

            // Update Intensity
            if (sunIntensity != null)
            {
                sunLight.intensity = sunIntensity.Evaluate(time) * maxSunIntensity;
            }

            // Fix Broken Shadow Bias:
            // The custom shader's ApplyShadowBias function needs this vector. Without it, Bias is effectively 0.
            Shader.SetGlobalVector("_LightDirection", sunLight.transform.forward);
        }

        private void SetupDefaultSunGradient()
        {
            sunColor = new Gradient();
            
            // Keys: Time (0-1), Color
            // 0.0 (Midnight): Black/Dark
            // 0.25 (Sunrise): Orange/Red
            // 0.5 (Noon): White
            // 0.75 (Sunset): Red/Purple
            // 1.0 (Midnight): Black
            
            GradientColorKey[] colorKeys = new GradientColorKey[] 
            {
                new GradientColorKey(new Color(0.1f, 0.1f, 0.2f), 0.0f),  // Night
                new GradientColorKey(new Color(0.1f, 0.1f, 0.2f), 0.20f), // Pre-Dawn
                new GradientColorKey(new Color(1.0f, 0.6f, 0.2f), 0.25f), // Sunrise (Warm)
                new GradientColorKey(new Color(1.0f, 0.95f, 0.9f), 0.35f), // Morning
                new GradientColorKey(Color.white, 0.5f),                  // Noon
                new GradientColorKey(new Color(1.0f, 0.8f, 0.6f), 0.65f), // Late Afternoon
                new GradientColorKey(new Color(1.0f, 0.5f, 0.2f), 0.75f), // Sunset (Warm)
                new GradientColorKey(new Color(0.1f, 0.1f, 0.2f), 0.80f), // Post-Sunset
                new GradientColorKey(new Color(0.1f, 0.1f, 0.2f), 1.0f)   // Night
            };
            
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            };

            sunColor.SetKeys(colorKeys, alphaKeys);
        }

        private void SetupDefaultAmbientGradient()
        {
            ambientColor = new Gradient();
            // Ambient matches sky tone
             GradientColorKey[] colorKeys = new GradientColorKey[] 
            {
                new GradientColorKey(new Color(0.05f, 0.05f, 0.1f), 0.0f), // Night
                new GradientColorKey(new Color(0.2f, 0.15f, 0.25f), 0.20f),
                new GradientColorKey(new Color(0.5f, 0.4f, 0.3f), 0.25f), // Sunrise
                new GradientColorKey(new Color(0.6f, 0.7f, 0.8f), 0.5f),   // Day (Blue-ish fake sky)
                new GradientColorKey(new Color(0.5f, 0.4f, 0.4f), 0.75f), // Sunset
                new GradientColorKey(new Color(0.05f, 0.05f, 0.1f), 0.85f),
                new GradientColorKey(new Color(0.05f, 0.05f, 0.1f), 1.0f)
            };
            
             GradientAlphaKey[] alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            };
            
            ambientColor.SetKeys(colorKeys, alphaKeys);
        }
    }
}
