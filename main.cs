/* ||| !!! SHITCODE ATTENTION !!! ||| */
/* ||| !!! SHITCODE ATTENTION !!! ||| */
/* ||| !!! SHITCODE ATTENTION !!! ||| */
using System;
using System.Xml.Serialization;
using Rocket.API;
using Rocket.Core.Plugins;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using UnityEngine;
using SDG.Unturned;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Rocket.Unturned;
using Rocket.Core;
using Steamworks;
using Rocket.API.Collections;
using Rocket.Unturned.Chat;
using System.IO;

namespace intrcptn {
    [Serializable]
    public class db_player {
        public DateTime jailed_at;
        public string jailed_by;
        public double time;
        public string reason;
        public int cell;
        public Vector3 last_pos;

        public db_player() {
            jailed_at = DateTime.Now;
            jailed_by = string.Empty;
            time = 0.0;
            reason = string.Empty;
            cell = -1;
            last_pos = Vector3.zero;
        }
        public db_player(DateTime jailed_at, string jailed_by, double time, string reason, int cell, Vector3 last_pos) {
            this.jailed_at = jailed_at;
            this.jailed_by = jailed_by;
            this.time = time;
            this.reason = reason;
            this.cell = cell;
            this.last_pos = last_pos;
        }
    }
    
    public class jail_point {
        public string name;
        public Vector3 center;
        public float radius;
    }

    public static class list_ext {
        public static bool contains(this List<jail_point> l, string name, out int index) {
            for (int i = 0; i < l.Count; i++) {
                if (l[i].name.ToLower() == name.ToLower()) {
                    index = i;
                    return true;
                }
            }
            index = -1;
            return false;
        }
    }
    
    internal class cmd_jail : IRocketCommand {
        public void Execute(IRocketPlayer caller, params string[] command) {
            UnturnedPlayer p = (UnturnedPlayer)caller;
            if (command.Length < 4) {
                UnturnedChat.Say(p, Syntax, Color.red);
                return;
            }
            UnturnedPlayer p2 = UnturnedPlayer.FromName(command[0]);
            if (p2 == null || p2.Player == null) {
                UnturnedChat.Say(p, main.instance.Translate("player_not_found"), Color.red);
                return;
            }
            if (main.db.ContainsKey(p2.CSteamID.m_SteamID)) {
                UnturnedChat.Say(p, main.instance.Translate("player_already_jailed"), Color.red);
                return;
            }
            double t;
            if (!double.TryParse(command[1], out t)) {
                UnturnedChat.Say(p, Syntax, Color.red);
                return;
            }
            int jindex;
            if (!main.cfg.jail_points.contains(command[2], out jindex)) {
                UnturnedChat.Say(p, main.instance.Translate("point_not_found"), Color.red);
                return;
            }
            string r = string.Join(" ", command, 3, command.Length-3);
            main.db.Add(p2.CSteamID.m_SteamID, new db_player(DateTime.Now, p.CharacterName, t, r, jindex, p2.Position));
            UnturnedChat.Say(p, main.instance.Translate("player_jailed", p2.CharacterName, t), Color.yellow);
            UnturnedChat.Say(p2, main.instance.Translate("on_jailed"), Color.yellow);
        }

        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "jail";
        public string Help => "null";
        public string Syntax => "/jail [player] [time] [point] [reason]";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "interception.jail.jail" };
    }

    internal class cmd_unjail : IRocketCommand {
        public void Execute(IRocketPlayer caller, params string[] command) {
            UnturnedPlayer p = (UnturnedPlayer)caller;
            if (command.Length < 1) {
                UnturnedChat.Say(p, Syntax, Color.red);
                return;
            }
            UnturnedPlayer p2 = UnturnedPlayer.FromName(command[0]);
            if (p2 == null || p2.Player == null) {
                UnturnedChat.Say(p, main.instance.Translate("player_not_found"), Color.red);
                return;
            }
            if (!main.db.ContainsKey(p2.CSteamID.m_SteamID)) {
                UnturnedChat.Say(p, main.instance.Translate("player_not_jailed"), Color.red);
                return;
            }
            main.db[p2.CSteamID.m_SteamID].jailed_at = new DateTime(2020, 1, 1);
            UnturnedChat.Say(p, main.instance.Translate("player_unjailed", p2.CharacterName), Color.yellow);
            UnturnedChat.Say(p2, main.instance.Translate("on_unjailed"), Color.yellow);
        }

        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "unjail";
        public string Help => "null";
        public string Syntax => "/unjail [player]";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "interception.jail.unjail" };
    }

    internal class cmd_jailpoint : IRocketCommand {
        public void Execute(IRocketPlayer caller, params string[] command) {
            UnturnedPlayer p = (UnturnedPlayer)caller;
            if (command.Length < 2) {
                UnturnedChat.Say(p, Syntax, Color.red);
                return;
            }

            if (command[0].ToLower() == "create") {
                if (command.Length < 3) {
                    UnturnedChat.Say(p, Syntax, Color.red);
                    return;
                }
                if (main.cfg.jail_points.contains(command[1], out _)) {
                    UnturnedChat.Say(p, main.instance.Translate("point_already_exist"), Color.red);
                    return;
                }
                float rad;
                if (!float.TryParse(command[2], out rad)) {
                    UnturnedChat.Say(p, Syntax, Color.red);
                    return;
                }
                main.cfg.jail_points.Add(new jail_point() { 
                    name = command[1],
                    center = p.Position,
                    radius = rad
                });
                UnturnedChat.Say(p, main.instance.Translate("point_created"), Color.yellow);
                return;
            }
            else if (command[0].ToLower() == "remove") {
                int jindex;
                if (!main.cfg.jail_points.contains(command[1], out jindex)) {
                    UnturnedChat.Say(p, main.instance.Translate("point_not_found"), Color.red);
                    return;
                }
                main.cfg.jail_points.RemoveAt(jindex);
                UnturnedChat.Say(p, main.instance.Translate("point_removed"), Color.yellow);
                return;
            }
            else {
                UnturnedChat.Say(p, Syntax, Color.red);
                return;
            }
         }

        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "jailpoint";
        public string Help => "null";
        public string Syntax => "/jailpoint [create|remove] [name] [radius]";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "interception.jail.jailpoint" };
    }

    public class player_jail_component : UnturnedPlayerComponent {
        DateTime cd;
        bool ui_enabled;
        
        protected override void Load() {
            cd = DateTime.Now;
            ui_enabled = false;
        }
        
        void Update() {
            if ((DateTime.Now - cd).TotalSeconds < 0.5) return;
            if (main.db.ContainsKey(base.Player.CSteamID.m_SteamID)) {
                if (!ui_enabled) {
                    ui_enabled = true;
                    EffectManager.sendUIEffect(main.cfg.effect_id, main.cfg.effect_key, base.Player.Player.channel.owner.transportConnection, true);
                    EffectManager.sendUIEffectText(main.cfg.effect_key, base.Player.Player.channel.owner.transportConnection, true, "jail_top_text", main.instance.Translate("ui_top_text"));
                    EffectManager.sendUIEffectText(main.cfg.effect_key, base.Player.Player.channel.owner.transportConnection, true, "jail_jailed_by_text", main.instance.Translate("ui_instigator_text", main.db[base.Player.CSteamID.m_SteamID].jailed_by));
                    EffectManager.sendUIEffectText(main.cfg.effect_key, base.Player.Player.channel.owner.transportConnection, true, "jail_reason_text", main.instance.Translate("ui_reason_text", main.db[base.Player.CSteamID.m_SteamID].reason));
                }
                if (ui_enabled) {
                    EffectManager.sendUIEffectText(main.cfg.effect_key, base.Player.Player.channel.owner.transportConnection, true, "jail_time_remain_text", main.instance.Translate("ui_time_remain_text", (TimeSpan.FromSeconds(main.db[base.Player.CSteamID.m_SteamID].time) - TimeSpan.FromSeconds((DateTime.Now - main.db[base.Player.CSteamID.m_SteamID].jailed_at).TotalSeconds)).ToString(@"hh\:mm\:ss")));
                }
                if ((DateTime.Now - main.db[base.Player.CSteamID.m_SteamID].jailed_at).TotalSeconds >= main.db[base.Player.CSteamID.m_SteamID].time) {
                    base.Player.Player.teleportToLocationUnsafe(main.db[base.Player.CSteamID.m_SteamID].last_pos, base.Player.Rotation);
                    main.db.Remove(base.Player.CSteamID.m_SteamID);
                    if (ui_enabled) {
                        ui_enabled = false;
                        EffectManager.askEffectClearByID(main.cfg.effect_id, base.Player.Player.channel.owner.transportConnection);
                    }
                    UnturnedChat.Say(base.Player, main.instance.Translate("on_unjailed"), Color.yellow);
                    cd = DateTime.Now;
                    return;
                }
                if ((base.Player.Position - main.cfg.jail_points[main.db[base.Player.CSteamID.m_SteamID].cell].center).sqrMagnitude >= (Mathf.Pow(main.cfg.jail_points[main.db[base.Player.CSteamID.m_SteamID].cell].radius, 2))) {
                    base.Player.Player.teleportToLocationUnsafe(main.cfg.jail_points[main.db[base.Player.CSteamID.m_SteamID].cell].center, base.Player.Rotation);
                }
            }
            cd = DateTime.Now;
        }
    }

    public class config : IRocketPluginConfiguration, IDefaultable {
        public ushort effect_id;
        public short effect_key;
        public List<jail_point> jail_points;
       
        public void LoadDefaults() {
            effect_id = 42713;
            effect_key = -22713;
            jail_points = new List<jail_point>();
        }
    }

    public class main : RocketPlugin<config> {
        internal static main instance;
        internal static config cfg;
        static readonly string db_path = Path.Combine(Path.Combine(System.IO.Directory.GetCurrentDirectory(), $"Plugins/{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}"), "database.json");
        public static Dictionary<ulong, db_player> db = new Dictionary<ulong, db_player>();

        protected override void Load() {
            instance = this;
            cfg = instance.Configuration.Instance;
            Level.onPostLevelLoaded += delegate (int xd) {
                if (File.Exists(db_path))
                    db = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<ulong, db_player>>(File.ReadAllText(db_path));
            };
            SaveManager.onPostSave += delegate () {
                Configuration.Save();
                File.WriteAllText(db_path, Newtonsoft.Json.JsonConvert.SerializeObject(db));
            };
            GC.Collect();
        }

        protected override void Unload() {
            cfg = null;
            instance = null;
            GC.Collect();
        }

        public override TranslationList DefaultTranslations => new TranslationList
        {
            { "point_not_found", "Точка с таким именем не найдена" },
            { "point_already_exist", "Точка с таким именем уже существует" },
            { "point_removed", "Точка удалена" },
            { "point_created", "Точка создана" },

            { "player_not_found", "Игрок с таким именем не найден" },
            { "player_already_jailed", "Этот игрок уже находится в тюрьме" },
            { "player_jailed", "Вы посадили игрока {0} на {1} секунд" },

            { "player_not_jailed", "Этот игрок не находится в тюрьме" },
            { "player_unjailed", "Вы выпустили игрока {0} из тюрьмы" },

            { "on_jailed", "Вас посадили в тюрьму" },
            { "on_unjailed", "Вас выпустили из тюрьмы" },

            { "ui_top_text", "Вы находитесь в тюрьме" },
            { "ui_instigator_text", "Вас посадил: {0}" },
            { "ui_reason_text", "Причина: {0}" },
            { "ui_time_remain_text", "Осталось: {0}" }
        };
    }
}

