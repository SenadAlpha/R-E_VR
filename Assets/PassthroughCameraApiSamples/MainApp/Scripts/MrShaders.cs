// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace PassthroughCameraSamples.MainApp
{
    public static class MrShaders
    {
        public static Shader EdgeSobel()
        {
            return Resources.Load<Shader>("Shaders/EdgeDetectionSobel")
                   ?? Shader.Find("MainApp/EdgeDetectionSobel");
        }

        public static Shader Water()
        {
            return Resources.Load<Shader>("Shaders/ShaderSampleWater")
                   ?? Shader.Find("Meta/PCA/ShaderSampleWater");
        }

        public static Material CreateWaterMaterial()
        {
            var source = Resources.Load<Material>("Materials/ShaderSampleWaterMat");
            if (source)
            {
                return new Material(source);
            }

            var shader = Water() ?? Shader.Find("Unlit/Texture") ?? Shader.Find("Sprites/Default");
            return shader ? new Material(shader) : null;
        }

        public static Shader PhotoPerspective()
        {
            return Resources.Load<Shader>("Shaders/PhotoPerspective")
                   ?? Shader.Find("MainApp/PhotoPerspective");
        }

        public static Material CreatePhotoPerspective()
        {
            var shader = PhotoPerspective() ?? UnlitTexture();
            return shader ? new Material(shader) : null;
        }

        public static Shader UnlitTexture()
        {
            return Resources.Load<Shader>("Shaders/UnlitTexture")
                   ?? Shader.Find("MainApp/UnlitTexture")
                   ?? Shader.Find("Unlit/Texture")
                   ?? Shader.Find("Sprites/Default");
        }

        public static Material CreateUnlitTexture(Texture texture)
        {
            var shader = UnlitTexture();
            if (!shader)
            {
                return null;
            }

            var material = new Material(shader);
            if (texture)
            {
                material.mainTexture = texture;
                material.SetTexture("_MainTex", texture);
            }

            if (material.HasProperty("_Color"))
            {
                material.color = Color.white;
            }

            return material;
        }

        public static Material CreateUnlitTransparent(Color color)
        {
            var shader = Resources.Load<Shader>("Shaders/UnlitTransparent")
                         ?? Shader.Find("MainApp/UnlitTransparent")
                         ?? Shader.Find("Sprites/Default");
            if (!shader)
            {
                return CreateUnlitTexture(Texture2D.whiteTexture);
            }

            var material = new Material(shader);
            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", Texture2D.whiteTexture);
            }

            material.color = color;
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            return material;
        }
    }
}
