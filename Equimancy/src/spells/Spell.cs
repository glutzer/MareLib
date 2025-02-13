﻿using MareLib;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Equimancy;

public class SpellAttribute : ClassAttribute
{
    public SpellAttribute()
    {

    }
}

/// <summary>
/// Spell, instance is spawned by the spell manager.
/// </summary>
public abstract class Spell
{
    // Only set on client.
    private Vector3d lastPosition;
    private Vector3d nextPosition;

    public Vector3d Position { get; private set; }

    /// <summary>
    /// Set position here when the client receives a packet.
    /// </summary>
    public void OnNewClientPosition(Vector3d position)
    {
        lastPosition = Position;
        nextPosition = position;
    }

    /// <summary>
    /// Sets client position.
    /// Takes current delta (time between last position received and the interval between packets) at a 0-1 range.
    /// </summary>
    public void SetPositionFromTickDelta(float delta)
    {
        Position = Vector3d.Lerp(lastPosition, nextPosition, delta);
    }

    public bool Alive { get; private set; } = true;

    /// <summary>
    /// For the server, what players are in range and will receive updates.
    /// </summary>
    public readonly HashSet<TrackedPlayer> trackedPlayers = new();

    public Entity? castedBy;

    public readonly SpellManager spellManager;
    public readonly long InstanceId;
    public readonly string code;
    public readonly SpellConfig config;
    public readonly long castedAtTime;

    protected bool isServer;

    public Spell(SpellManager spellManager, long instanceId, Vector3d position, string code, SpellConfig? config)
    {
        this.spellManager = spellManager;
        InstanceId = instanceId;

        lastPosition = position;
        Position = position;
        nextPosition = position;

        this.code = code;

        this.config = config ?? new SpellConfig();

        isServer = spellManager.isServer;

        // Get entity casting this.
        if (config != null)
        {
            long casterId = config.GetCastedBy();
            castedBy = spellManager.api.World.GetEntityById(casterId);
        }

        castedAtTime = spellManager.api.World.ElapsedMilliseconds;
    }

    /// <summary>
    /// Initialize a spell when spawned.
    /// Client/server.
    /// </summary>
    public virtual void Initialize()
    {

    }

    /// <summary>
    /// Called 10 times per second on active spells.
    /// Client/server.
    /// </summary>
    public virtual void OnTick()
    {

    }

    /// <summary>
    /// Removes spell.
    /// Will be removed during next tick update.
    /// Client/server.
    /// </summary>
    public void Kill()
    {
        Alive = false;
    }

    /// <summary>
    /// Handle a packet on the client.
    /// </summary>
    public virtual void HandlePacket(SpellPacket packet)
    {

    }

    /// <summary>
    /// Send update packet from server.
    /// </summary>
    public void SendPacket(int id)
    {
        foreach (TrackedPlayer player in trackedPlayers)
        {
            player.AddSpellPacket(this, id, null);
        }
    }

    /// <summary>
    /// Send update packet from server with object.
    /// </summary>
    public void SendPacket<T>(int id, T data)
    {
        foreach (TrackedPlayer player in trackedPlayers)
        {
            player.AddSpellPacket(this, id, SerializerUtil.Serialize(data));
        }
    }

    /// <summary>
    /// When spell is removed by any means.
    /// Client/server.
    /// </summary>
    public virtual void OnRemoved()
    {

    }

    /// <summary>
    /// When a player is now tracking the spell.
    /// Server only.
    /// </summary>
    public virtual void OnTrackingPlayer(TrackedPlayer player)
    {

    }

    /// <summary>
    /// When a player is no longer tracking or has logged out.
    /// Server only.
    /// </summary>
    public virtual void OnNoLongerTrackingPlayer(TrackedPlayer player)
    {

    }

    /// <summary>
    /// Not really correct but not sure what game matrix transformation is doing.
    /// Returns the horn pos in relation to the entity's position.
    /// </summary>
    public static Vector3 GetLocalHornPos(EntityPlayer player)
    {
        AttachmentPointAndPose? pose = player.AnimManager.Animator?.GetAttachmentPointPose("Eyes");
        if (pose == null) return Vector3.Zero;
        AttachmentPoint attachPoint = pose.AttachPoint;
        double rotationX = attachPoint.RotationX * GameMath.DEG2RAD;
        double rotationY = attachPoint.RotationY * GameMath.DEG2RAD;
        double rotationZ = attachPoint.RotationZ * GameMath.DEG2RAD;
        Matrixf mat = new();

        mat.Identity();

        mat.RotateX(player.Pos.Roll);
        mat.RotateY(player.BodyYaw + (-90 * GameMath.DEG2RAD));

        //mat.RotateX((float)rotationX);
        mat.RotateY((float)rotationY);
        mat.RotateZ((float)rotationZ);

        mat.Translate((attachPoint.PosX / 16) - 0.5f, attachPoint.PosY / 16, (attachPoint.PosZ / 16) - 0.5f);
        mat.Mul(pose.AnimModelMatrix);
        Vec4f pos = mat.TransformVector(new Vec4f(0, 0, 0, 1));
        return new Vector3(pos.X, pos.Y + (float)player.LocalEyePos.Y + 0.1f, pos.Z);
    }
}