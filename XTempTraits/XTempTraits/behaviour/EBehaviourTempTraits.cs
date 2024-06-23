using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace XTempTraits;

public class EBehaviourTempTraits : EntityBehavior
{
    public const string Name = "temptraits";
    
    protected XTempTraitsModSystem _xTempTraitsModSystem;

    public EBehaviourTempTraits(Entity entity) : base(entity)
    {
        if (entity.GetType() != typeof(EntityPlayer))
        {
            throw new ApplicationException("Invalid entity type. Expected EntityPlayer");
        }
        
        var msys = entity.Api.ModLoader.GetModSystem<XTempTraitsModSystem>();
        _xTempTraitsModSystem = msys ?? throw new ApplicationException("Could not find XTempTraitsModSystem");
    }

    public override string PropertyName() => Name;

    public override void OnGameTick(float dt)
    {
        var player = (EntityPlayer)entity;
        var currentTime = (float)player.Api.World.Calendar.TotalHours;
        var list = _xTempTraitsModSystem.GetTempTraits(player);
        foreach (var trait in list)
        {
            if (_xTempTraitsModSystem.IsTraitExpired(player, currentTime, trait.Code))
            {
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
                if (player.ShouldReceiveDamage(source, val))
                {
                    player.ReceiveDamage(source, val);
                }
            }
        }
    }
}