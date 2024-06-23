using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace XTempTraits;

public class XTempTraitsModSystem : ModSystem
{
    private ICoreAPI api;
    private ILogger log;
    private const string ExtraTraits = "extraTraits";
    private string[] TraitsConfPaths = {"game:config/traits.json", "config/traits.json"};

    private List<Trait> _traits = new();
    private Dictionary<string, Trait> _traitsByCode = new();
    private Dictionary<string, Trait> _tempTraitsByCode = new();

    //todo: write traits list to player attributes
    
    public override void Start(ICoreAPI api)
    {
        this.api = api;
        log = api.Logger;
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        api.RegisterEntityBehaviorClass(EBehaviourTempTraits.Name, typeof(EBehaviourTempTraits));
        api.Event.ServerRunPhase(EnumServerRunPhase.ModsAndConfigReady, LoadData);
    }

    private void LoadData()
    {
        var list = new List<Trait>();

        foreach (var path in TraitsConfPaths)
        {
            var ok = api.Assets.Exists(new AssetLocation(path));
            if (!ok)
            {
                throw new ApplicationException("Expected config file - \""+path+"\" but not found");
            }
            var l = api.Assets.Get(path).ToObject<List<Trait>>();
            list.AddRange(l);
        }

        _traits = list;
        foreach (var trait in _traits)
        {
            _traitsByCode[trait.Code] = trait;
            if (trait.IsTemp())
            {
                _tempTraitsByCode[trait.Code] = trait;
            }
        }
    }

    public override void StartClientSide(ICoreClientAPI capi)
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
        foreach (var kv in player.Stats)
        {
            log.Debug($"STAT {kv.Key} : ${kv.Value}");
        }
        foreach (var kv in player.WatchedAttributes)
        {
            log.Debug($"WATTR {kv.Key} : {kv.Value.ToJsonToken()}");
        }
        if (arr == null)
        {
            return list;
        }
        
        log.Debug(string.Join(", ", arr));

        list.AddRange(arr.Select(x => _traitsByCode[x]).Where(t => t != null && t.IsTemp()));

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