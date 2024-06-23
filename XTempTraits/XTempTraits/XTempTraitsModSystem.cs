using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace XTempTraits;

public class XTempTraitsModSystem : ModSystem
{
    private const string TraitsConfPath = "config/traits.json";
    private const string ExtraTraits = "extraTraits";
    
    private List<Trait> _traits = new();
    private Dictionary<string, Trait> _traitsByCode = new();

    public override void Start(ICoreAPI api)
    {
        _traits = api.Assets.Get(TraitsConfPath).ToObject<List<Trait>>();
        foreach (var trait in _traits)
        {
            _traitsByCode[trait.Code] = trait;
        }
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        api.RegisterEntityBehaviorClass(EBehaviourTempTraits.Name, typeof(EBehaviourTempTraits));
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
    }

    public override void Dispose()
    {
        _traits = new List<Trait>();
        _traitsByCode = new Dictionary<string, Trait>();
        base.Dispose();
    }

    public void ApplyTrait(EntityPlayer player, Trait trait)
    {
        var currentTime = (float)player.Api.World.Calendar.TotalHours;
        
        foreach (var attribute in trait.Attributes)
        {
            player.Stats.Set(attribute.Key, "trait", (float) attribute.Value, true);
            player.Stats.Set(attribute.Key + "-timeout", "trait", currentTime + trait.Time, true);
        }
    }

    public ICollection<Trait> GetTraits(EntityPlayer player)
    {
        if (player == null)
        {
            throw new ApplicationException("player object missed");
        }
        
        var list = new List<Trait>();
        var arr = player.WatchedAttributes.GetStringArray(ExtraTraits);
        foreach (var x in arr)
        {
            var t = _traitsByCode[x];
            if (t == null)
            {
                continue;
            }
            list.Add(t);
        }

        return list;
    }
    
    public ICollection<Trait> GetTempTraits(EntityPlayer player)
    {
        if (player == null)
        {
            throw new ApplicationException("player object missed");
        }
        
        var list = new List<Trait>();
        var arr = player.WatchedAttributes.GetStringArray(ExtraTraits);
        foreach (var x in arr)
        {
            var t = _traitsByCode[x];
            if (t != null && t.IsTemp())
            {
                list.Add(t);
            }
        }

        return list;
    }

    public bool IsTraitExpired(EntityPlayer player, float currentTime, string name)
    {
        if (player == null)
        {
            throw new ApplicationException("player object missed");
        }
        
        var value = player.WatchedAttributes.GetFloat(name + "-timeout");
        return currentTime > value;
    }

    public void RemoveTrait(EntityPlayer player, string name)
    {
        if (player == null)
        {
            throw new ApplicationException("player object missed");
        }
        
        var stat = player.Stats[name];
        if (stat == null)
        {
            return;
        }
        
        stat.Remove("trait");
        
        stat = player.Stats[name + "-timeout"];
        if (stat == null)
        {
            return;
        }
        
        stat.Remove("trait");
    }
}