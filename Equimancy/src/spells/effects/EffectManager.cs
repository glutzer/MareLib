﻿using MareLib;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace Equimancy;

/// <summary>
/// Manages effects on entities, like buffs/debuffs or spells that can only affect once.
/// Active effects are held in the entity behavior.
/// </summary>
[GameSystem]
public class EffectManager : NetworkedGameSystem
{
    public readonly Dictionary<string, Type> effectTypes = new();

    public EffectManager(bool isServer, ICoreAPI api) : base(isServer, api, "effectmanager")
    {
    }

    public override void Initialize()
    {
        // Load all effect types on client/server.
        (Type, EffectAttribute)[] effectAttributes = AttributeUtilities.GetAllAnnotatedClasses<EffectAttribute>();
        foreach ((Type type, EffectAttribute attribute) in effectAttributes)
        {
            effectTypes.Add(attribute.code, type);
        }
    }

    protected override void RegisterClientMessages(IClientNetworkChannel channel)
    {

    }

    protected override void RegisterServerMessages(IServerNetworkChannel channel)
    {

    }

    public override void OnAssetsLoaded()
    {
        ReplaceBehaviors();
    }

    /// <summary>
    /// Add effect behavior to everything with health.
    /// </summary>
    public void ReplaceBehaviors()
    {
        foreach (EntityProperties entityType in api.World.EntityTypes)
        {
            JObject nuJObject = new()
            {
                ["code"] = "EntityBehaviorEffects"
            };

            JsonObject effectObject = new(nuJObject);

            if (api.Side == EnumAppSide.Server)
            {
                if (entityType.Server.BehaviorsAsJsonObj.FirstOrDefault(x => x.ToString().ToLower().Contains("health")) != null)
                {
                    JsonObject[] newBehaviors = new JsonObject[entityType.Server.BehaviorsAsJsonObj.Length + 1];
                    Array.Copy(entityType.Server.BehaviorsAsJsonObj, 0, newBehaviors, 1, entityType.Server.BehaviorsAsJsonObj.Length);
                    newBehaviors[0] = effectObject;
                    entityType.Server.BehaviorsAsJsonObj = newBehaviors;
                }
            }
            else
            {
                if (entityType.Client.BehaviorsAsJsonObj.FirstOrDefault(x => x.ToString().ToLower().Contains("health")) != null)
                {
                    JsonObject[] newBehaviors = new JsonObject[entityType.Client.BehaviorsAsJsonObj.Length + 1];
                    Array.Copy(entityType.Client.BehaviorsAsJsonObj, 0, newBehaviors, 1, entityType.Client.BehaviorsAsJsonObj.Length);
                    newBehaviors[0] = effectObject;
                    entityType.Client.BehaviorsAsJsonObj = newBehaviors;
                }
            }
        }
    }
}