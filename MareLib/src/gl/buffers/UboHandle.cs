﻿using OpenTK.Graphics.OpenGL4;
using System;

namespace MareLib;

public unsafe class UboHandle<T> : IDisposable where T : unmanaged
{
    private readonly int handle;
    private readonly BufferUsageHint usageType;

    public UboHandle(BufferUsageHint usageType)
    {
        handle = GL.GenBuffer();
        this.usageType = usageType;
    }

    public void BufferData(T data)
    {
        GL.BindBuffer(BufferTarget.UniformBuffer, handle);
        GL.BufferData(BufferTarget.UniformBuffer, sizeof(T), ref data, usageType);
    }

    public void UpdateData(T data)
    {
        GL.BindBuffer(BufferTarget.UniformBuffer, handle);
        GL.BufferSubData(BufferTarget.UniformBuffer, IntPtr.Zero, sizeof(T), ref data);
    }

    public void BufferData(T[] data)
    {
        GL.BindBuffer(BufferTarget.UniformBuffer, handle);
        GL.BufferData(BufferTarget.UniformBuffer, sizeof(T) * data.Length, data, usageType);
    }

    public void UpdateData(T[] data)
    {
        GL.BindBuffer(BufferTarget.UniformBuffer, handle);
        GL.BufferSubData(BufferTarget.UniformBuffer, IntPtr.Zero, sizeof(T) * data.Length, data);
    }

    public void Bind(int bindingPoint)
    {
        GL.BindBufferBase(BufferRangeTarget.UniformBuffer, bindingPoint, handle);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        GL.DeleteBuffer(handle);
    }
}