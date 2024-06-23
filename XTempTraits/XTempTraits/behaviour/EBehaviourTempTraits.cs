using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace XTempTraits;

public class EBehaviourTempTraits : EntityBehavior
{
    public const string Name = "temptraits";

    protected XTempTraitsModSystem _xTempTraitsModSystem;

    private ILogger log;
    private long lastHour;

    public EBehaviourTempTraits(Entity entity) : base(entity)
    {
        if (entity.GetType() != typeof(EntityPlayer))
        {
            throw new ApplicationException("Invalid entity type. Expected EntityPlayer");
        }

        log = entity.Api.Logger;

        var msys = entity.Api.ModLoader.GetModSystem<XTempTraitsModSystem>();
        _xTempTraitsModSystem = msys ?? throw new ApplicationException("Could not find XTempTraitsModSystem");
    }

    public override string PropertyName() => Name;

    public override void OnGameTick(float dt)
    {
        var player = (EntityPlayer)entity;
        var currentTime = player.Api.World.Calendar.TotalHours;
        if (lastHour == (long)currentTime)
        {
            return;
        }
        log.Debug("Update traits...");
        lastHour = (long)currentTime;

        var list = _xTempTraitsModSystem.GetTempTraits(player);
        if (list.Count == 0)
        {
            log.Debug("Player do not has temporary traits");
        }

        log.Debug($"{player.PlayerUID}; TempTraits {string.Join(", ", list)}");
        foreach (var trait in list)
        {
            if (_xTempTraitsModSystem.IsTraitExpired(player, (float)currentTime, trait.Code))
            {
                log.Debug("Player "+player.PlayerUID+" trait "+trait.Code+" expired");
                _xTempTraitsModSystem.RemoveTrait(player, trait.Code);
                continue;
            }

            _applyTempAttributes(player, trait);
        }
    }

    private void _applyTempAttributes(EntityPlayer player, Trait trait)
    {
        foreach (var attr in trait.Attributes)
        {
            if (attr.Key == "instantHealthDamage")
            {
                var source = new DamageSource
                {
                    Source = EnumDamageSource.Internal,
                    Type = EnumDamageType.Injury,
                    DamageTier = 5,
                    KnockbackStrength = 0
                };

                var val = (float)attr.Value;
                log.Debug(player.PlayerUID + " should receive damage");
                if (player.ShouldReceiveDamage(source, val))
                {
                    player.ReceiveDamage(source, val);
                }
            }
        }
    }
}