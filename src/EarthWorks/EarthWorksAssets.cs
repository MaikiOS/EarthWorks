using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace OstrixMods.EarthWorks
{
    internal static class EarthWorksAssets
    {
        private const string RoadIconResource =
            "OstrixMods.EarthWorks.Resources.earthworks-road-icon.png";

        public static Sprite LoadRoadIcon(out Texture2D texture)
        {
            texture = null;
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                using (Stream stream = assembly.GetManifestResourceStream(RoadIconResource))
                {
                    if (stream == null)
                    {
                        EarthWorksPlugin.Log.LogError(
                            $"Missing embedded icon resource {RoadIconResource}.");
                        return null;
                    }

                    using (MemoryStream memory = new MemoryStream())
                    {
                        stream.CopyTo(memory);
                        texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                        {
                            name = "EarthWorks_RoadIconTexture",
                            filterMode = FilterMode.Bilinear,
                            wrapMode = TextureWrapMode.Clamp,
                            hideFlags = HideFlags.HideAndDontSave
                        };
                        Type imageConversion = FindImageConversionType();
                        if (imageConversion == null)
                        {
                            EarthWorksPlugin.Log.LogError(
                                "UnityEngine.ImageConversion is unavailable.");
                            UnityEngine.Object.Destroy(texture);
                            texture = null;
                            return null;
                        }
                        MethodInfo loadImage = imageConversion.GetMethod(
                            "LoadImage",
                            BindingFlags.Public | BindingFlags.Static,
                            null,
                            new[] { typeof(Texture2D), typeof(byte[]), typeof(bool) },
                            null);
                        if (loadImage == null ||
                            !(bool)loadImage.Invoke(
                                null,
                                new object[] { texture, memory.ToArray(), false }))
                        {
                            UnityEngine.Object.Destroy(texture);
                            texture = null;
                            return null;
                        }
                    }
                }

                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
                sprite.name = "EarthWorks_RoadIcon";
                sprite.hideFlags = HideFlags.HideAndDontSave;
                return sprite;
            }
            catch (Exception exception)
            {
                EarthWorksPlugin.Log.LogError("EarthWorks could not load its road icon.");
                EarthWorksPlugin.Log.LogError(exception);
                if (texture)
                {
                    UnityEngine.Object.Destroy(texture);
                    texture = null;
                }
                return null;
            }
        }

        private static Type FindImageConversionType()
        {
            Type result = Type.GetType(
                "UnityEngine.ImageConversion, UnityEngine.ImageConversionModule",
                false);
            if (result != null)
            {
                return result;
            }
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                result = assembly.GetType("UnityEngine.ImageConversion", false);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }
    }
}
