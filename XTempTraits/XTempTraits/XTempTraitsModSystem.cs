using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace XTempTraits;

public class XTempTraitsModSystem : ModSystem
{
    private const string ActiveTraits = "activetraits";
    private static readonly AssetLocation ClassDefinitionLoc = new("game:config/characterclasses.json");
    private static readonly AssetLocation TraitsConfLoc = new("game:config/traits.json");

    private ICoreAPI _api;
    private ILogger _log;
    private List<CharacterClass> _characterClasses = new();
    private Dictionary<string, CharacterClass> _characterClassesByCode = new();
    private List<Trait> _traits = new();
    private Dictionary<string, Trait> _traitsByCode = new();
    private readonly Dictionary<string, Trait> _tempTraitsByCode = new();

    public override void Start(ICoreAPI api)
    {
        _api = api;
        _log = api.Logger;
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        api.RegisterEntityBehaviorClass(EBehaviourTempTraits.Name, typeof(EBehaviourTempTraits));

        api.Network.GetChannel("charselection")
            .SetMessageHandler(new NetworkClientMessageHandler<CharacterSelectionPacket>(OnCharacterSelection));
        api.Event.PlayerJoin += Event_PlayerJoinServer;
        api.Event.ServerRunPhase(EnumServerRunPhase.ModsAndConfigReady, LoadData);
    }

    private void LoadData()
    {
        _log.Debug("Load traits definitions");
        var list = new List<Trait>();
        var l = _api.Assets.Get(TraitsConfLoc).ToObject<List<Trait>>();
        list.AddRange(l);

        _log.Debug($"Loaded traits - {string.Join(", ", list)}");

        _traits = list;
        foreach (var trait in _traits)
        {
            _traitsByCode[trait.Code] = trait;
            if (trait.IsTemp())
            {
                _tempTraitsByCode[trait.Code] = trait;
            }
        }

        _characterClasses = _api.Assets.Get(ClassDefinitionLoc).ToObject<List<CharacterClass>>();
        foreach (var characterClass in _characterClasses)
        {
            _characterClassesByCode[characterClass.Code] = characterClass;
        }
    }

    public override void StartClientSide(ICoreClientAPI capi)
    {
        capi.Event.PlayerJoin += Event_PlayerJoin;
        capi.Event.BlockTexturesLoaded += LoadData;
    }

    public override void Dispose()
    {
        _traits = new List<Trait>();
        _traitsByCode = new Dictionary<string, Trait>();
        _characterClasses = new List<CharacterClass>();
        _characterClassesByCode = new Dictionary<string, CharacterClass>();
        base.Dispose();
    }

    public void ApplyTrait(EntityPlayer player, Trait trait)
    {
        var currentTime = (float)player.Api.World.Calendar.TotalHours;

        foreach (var attribute in trait.Attributes)
        {
            player.Stats.Set(attribute.Key, "trait", (float)attribute.Value, true);
            player.Stats.Set(attribute.Key + "-timeout", "trait", currentTime + trait.Time, true);
        }
    }

    public ICollection<Trait> GetTraits(EntityPlayer player)
    {
        if (player == null)
        {
            throw new ApplicationException("player object missed");
        }

        var arr = player.WatchedAttributes.GetStringArray(ActiveTraits, Array.Empty<string>());
        return arr.Select(x => _traitsByCode[x]).Where(t => t != null).ToList();
    }

    public ICollection<Trait> GetTempTraits(EntityPlayer player)
    {
        if (player == null)
        {
            throw new ApplicationException("player object missed");
        }

        var list = new List<Trait>();
        var arr = player.WatchedAttributes.GetStringArray(ActiveTraits, Array.Empty<string>());
        foreach (var kv in player.Stats)
        {
            _log.Debug($"STAT {kv.Key} : ${kv.Value}");
        }

        foreach (var kv in player.WatchedAttributes)
        {
            _log.Debug($"WATTR {kv.Key} : {kv.Value.ToJsonToken()}");
        }

        _log.Debug(string.Join(", ", arr));
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

        var json = player.WatchedAttributes.GetString(ActiveTraits, "[]");
        var list = (List<TraitExpirationEntry>)JsonSerializer.Deserialize(json, typeof(List<TraitExpirationEntry>));
        if (list == null)
        {
            throw new ApplicationException("No data about active traits");
        }
        
        foreach (var entry in list)
        {
            var trait = _traitsByCode[entry.Code];
        }

        const string code = "trait";
        player.Stats[name]?.Remove(code);
        player.Stats[name + "-timeout"]?.Remove(code);
    }

    private void OnCharacterSelection(IServerPlayer player, CharacterSelectionPacket p)
    {
        SaveActiveTraits(player.Entity, p.CharacterClass);
    }

    private void Event_PlayerJoin(IClientPlayer player)
    {
        var entity = player.Entity;
        SaveActiveTraits(entity, entity.WatchedAttributes.GetString("characterClass"));
    }

    private void Event_PlayerJoinServer(IServerPlayer player)
    {
        _log.Event($">>>>Player {player.PlayerUID} joined to the server");
        var entity = player.Entity;
        SaveActiveTraits(entity, entity.WatchedAttributes.GetString("characterClass"));
    }

    private void SaveActiveTraits(EntityPlayer entity, string className)
    {
        var currentTime = entity.World.Calendar.TotalHours;

        _log.Debug($"Save active traits of class '{className}' for {entity.PlayerUID}");
        var cl = _characterClassesByCode[className];
        if (cl == null)
        {
            throw new ArgumentException("Not a valid character class code!");
        }

        entity.WatchedAttributes.RemoveAttribute(ActiveTraits);
        var activeTraits = cl.Traits.Select(name => _traitsByCode[name])
            .Select(trait => new TraitExpirationEntry(trait.Code, currentTime + trait.Time))
            .ToList();

        // todo: Tayron save attributes to stats as trait but really we must to save traits and attributes of them
        // todo: we must write to stats only traits and 
        foreach (var trait in activeTraits)
        {
            entity.Stats.Set(trait.Code, "trait")
        }
        
        entity.WatchedAttributes.SetString(
            ActiveTraits,
            JsonSerializer.Serialize(activeTraits)
        );
    }
}