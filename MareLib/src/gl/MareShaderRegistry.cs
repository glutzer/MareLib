﻿using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Vintagestory.API.Client;
using Vintagestory.Client.NoObf;

namespace MareLib;

public class ShaderEntry
{
    public string vertPath;
    public string fragPath;
    public string? geomPath;
    public string shaderName;

    public ShaderEntry(string vertPath, string fragPath, string? geomPath, string shaderName)
    {
        string[] vertInfo = vertPath.Split(':');
        if (vertInfo.Length != 2) vertInfo = new string[] { "game", vertInfo[0] };

        string[] fragInfo = fragPath.Split(':');
        if (fragInfo.Length != 2) fragInfo = new string[] { "game", fragInfo[0] };

        if (geomPath != null)
        {
            string[] geomInfo = geomPath.Split(':');
            if (geomInfo.Length != 2) geomInfo = new string[] { "game", geomInfo[0] };
            this.geomPath = $"{geomInfo[0]}:shaders/{geomInfo[1]}.geom";
        }

        this.vertPath = $"{vertInfo[0]}:shaders/{vertInfo[1]}.vert";
        this.fragPath = $"{fragInfo[0]}:shaders/{fragInfo[1]}.frag";

        this.shaderName = shaderName;
    }
}

public static class MareShaderRegistry
{
    public static Dictionary<string, MareShader> Shaders { get; } = new();
    private static readonly List<ShaderEntry> shaderEntries = new();
    private static bool initialized = false;

    /// <summary>
    /// Get a shader.
    /// </summary>
    public static MareShader Get(string name)
    {
        return Shaders[name];
    }

    /// <summary>
    /// Add a nu-shader, it will be available to use once initialized.
    /// Automatically reloaded.
    /// Example path: "marelib:gui" - same as marelib:shaders/gui.vert.
    /// </summary>
    public static MareShader AddShader(string vertPath, string fragPath, string shaderName, string? geomPath = null)
    {
        if (!initialized) Initialize();

        shaderEntries.Add(new ShaderEntry(vertPath, fragPath, geomPath, shaderName));
        Shaders.Add(shaderName, new MareShader());
        return Shaders[shaderName];
    }

    public static void Initialize()
    {
        MainAPI.Capi.Event.ReloadShader += () =>
        {
            foreach (ShaderEntry entry in shaderEntries)
            {
                RegisterShader(entry.vertPath, entry.fragPath, entry.geomPath, entry.shaderName);
            }

            return true;
        };
    }

    public static string SetUBOBindings(Dictionary<string, int> uniqueBlocks, string code)
    {
        string pattern = @"layout\(std140\)\s+uniform\s+(\w+)";

        return Regex.Replace(code, pattern, match =>
        {
            string blockDefinition = match.Groups[0].Value;
            if (!uniqueBlocks.TryGetValue(blockDefinition, out int id))
            {
                id = uniqueBlocks.Count;
                uniqueBlocks[blockDefinition] = id;
            }
            string modifiedBlock = blockDefinition.Replace("layout(std140)", $"layout(std140, binding = {id})");

            return modifiedBlock;
        });
    }

    private static void RegisterShader(string vertPath, string fragPath, string? geomPath, string shaderName)
    {
        ICoreClientAPI capi = MainAPI.Capi;

        IShaderProgram shader = capi.Shader.NewShaderProgram();

        MethodInfo method = typeof(ShaderRegistry).GetMethod("HandleIncludes", BindingFlags.NonPublic | BindingFlags.Static)!;
        object[] vertParams = new object[] { shader, capi.Assets.Get(vertPath).ToText(), null! };
        object[] fragParams = new object[] { shader, capi.Assets.Get(fragPath).ToText(), null! };

        Dictionary<string, int> uniqueBlocks = new();

        shader.VertexShader = capi.Shader.NewShader(EnumShaderType.VertexShader);
        shader.FragmentShader = capi.Shader.NewShader(EnumShaderType.FragmentShader);

        shader.VertexShader.Code = (string)method.Invoke(null, vertParams)!;
        shader.FragmentShader.Code = (string)method.Invoke(null, fragParams)!;

        shader.VertexShader.Code = SetUBOBindings(uniqueBlocks, shader.VertexShader.Code);
        shader.FragmentShader.Code = SetUBOBindings(uniqueBlocks, shader.FragmentShader.Code);

        if (geomPath != null)
        {
            object[] geomParams = new object[] { shader, capi.Assets.Get(geomPath).ToText(), null! };
            shader.GeometryShader = capi.Shader.NewShader(EnumShaderType.GeometryShader);
            shader.GeometryShader.Code = (string)method.Invoke(null, geomParams)!;
            shader.GeometryShader.Code = SetUBOBindings(uniqueBlocks, shader.GeometryShader.Code);
        }

        capi.Shader.RegisterMemoryShaderProgram(shaderName, shader);

        shader.Compile();

        // Set relevant shader info.
        MareShader nuShader = Shaders[shaderName];
        nuShader.SetProgram((ShaderProgram)shader);
    }

    public static void Dispose()
    {
        // Registered shaders are already disposed by the game.

        initialized = false;
        Shaders.Clear();
        shaderEntries.Clear();
    }
}