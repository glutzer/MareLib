﻿using OpenTK.Graphics.OpenGL4;
using SkiaSharp;
using System;
using Vintagestory.API.Common;

namespace MareLib;

public class Texture : IDisposable
{
    public int Handle { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    /// <summary>
    /// Takes full asset location.
    /// </summary>
    public static Texture Create(string assetPath, bool aliased = true, bool mipmaps = false)
    {
        IAsset? textureAsset = MainAPI.Capi.Assets.Get(new AssetLocation(assetPath)) ?? throw new Exception($"Texture asset not found: {assetPath}!");
        byte[] pngData = textureAsset.Data;

        return Create(pngData, aliased, mipmaps);
    }

    public static Texture Create(byte[] pngData, bool aliased = true, bool mipmaps = false)
    {
        SKBitmap bmp = SKBitmap.Decode(pngData);
        return Create(bmp, aliased, mipmaps);
    }

    public static Texture Create(SKBitmap bitmap, bool aliased = true, bool mipmaps = false)
    {
        int textureHandle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, textureHandle);

        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bitmap.Width, bitmap.Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, bitmap.Pixels);

        SetAliasing(aliased, mipmaps, TextureTarget.Texture2D);
        if (mipmaps) SetMipmaps(GetMaxMipmaps(bitmap.Width, bitmap.Height), TextureTarget.Texture2D);

        Texture texture = new()
        {
            Handle = textureHandle,
            Width = bitmap.Width,
            Height = bitmap.Height
        };

        return texture;
    }

    public static int GetMaxMipmaps(int width, int height)
    {
        return 1 + (int)Math.Log2(Math.Max(width, height));
    }

    public static void SetAliasing(bool aliased, bool mipmaps, TextureTarget target)
    {
        GL.TexParameter(target, TextureParameterName.TextureMinFilter, aliased ? mipmaps ? (int)TextureMinFilter.NearestMipmapNearest : (int)TextureMinFilter.Nearest : mipmaps ? (int)TextureMinFilter.LinearMipmapLinear : (int)TextureMinFilter.Linear);
        GL.TexParameter(target, TextureParameterName.TextureMagFilter, aliased ? (int)TextureMagFilter.Nearest : (int)TextureMagFilter.Linear);
    }

    public static void SetMipmaps(int maxLevel, TextureTarget target)
    {
        GL.TexParameter(target, TextureParameterName.TextureBaseLevel, 0);
        GL.TexParameter(target, TextureParameterName.TextureMaxLevel, maxLevel);
        GL.TexParameter(target, TextureParameterName.TextureLodBias, 0f);
        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
    }

    public void Dispose()
    {
        GL.DeleteTexture(Handle);
        GC.SuppressFinalize(this);
        Handle = 0;
    }
}