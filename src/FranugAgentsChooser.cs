using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Admin;
using MySqlConnector;
using Microsoft.Data.Sqlite;
using System.Text.Json.Serialization;

namespace FranugAgentsChooser;

public class ConfigGen : BasePluginConfig
{
    [JsonPropertyName("AccessFlag")] public string AccessFlag { get; set; } = "";
    [JsonPropertyName("UsableTeam")] public int UsableTeam { get; set; } = 3;
    [JsonPropertyName("DatabaseType")]
    public string DatabaseType { get; set; } = "SQLite";
    [JsonPropertyName("DatabaseFilePath")]
    public string DatabaseFilePath { get; set; } = "/csgo/addons/counterstrikesharp/plugins/FranugAgentsChooser/franug-agentschooser-db.sqlite";
    [JsonPropertyName("DatabaseHost")]
    public string DatabaseHost { get; set; } = "";
    [JsonPropertyName("DatabasePort")]
    public int DatabasePort { get; set; }
    [JsonPropertyName("DatabaseUser")]
    public string DatabaseUser { get; set; } = "";
    [JsonPropertyName("DatabasePassword")]
    public string DatabasePassword { get; set; } = "";
    [JsonPropertyName("DatabaseName")]
    public string DatabaseName { get; set; } = "";
    [JsonPropertyName("Comment")]
    public string Comment { get; set; } = "Use SQLite or MySQL as Database Type. Usable team: 3 = both, 2 = only CTs, 1 = only TTs";
    [JsonPropertyName("DebugEnabled")] public bool DebugEnabled { get; set; } = true;
    [JsonPropertyName("ChatTag")] public string ChatTag { get; set; } = $" {ChatColors.Lime}[AgentsChooser]{ChatColors.Green} ";
}


[MinimumApiVersion(154)]
public class FranugAgentsChooser : BasePlugin, IPluginConfig<ConfigGen>
{
    public override string ModuleName => "Franug Agents Chooser";
    public override string ModuleAuthor => "Franc1sco Franug";
    public override string ModuleVersion => "0.0.6dev";
    public ConfigGen Config { get; set; } = null!;
    public void OnConfigParsed(ConfigGen config) { Config = config; }

    private SqliteConnection? connectionSQLITE = null;
    internal static MySqlConnection? connectionMySQL = null;
    internal static Dictionary<int, AgentsInfo> gAgentsInfo = new Dictionary<int, AgentsInfo>();

    internal static readonly Dictionary<string, string> agentsListCT = new()
    {
        {"characters\\models\\ctm_fbi\\ctm_fbi_varianta.vmdl", "CounterTerrorist | FBI"},
        {"characters\\models\\ctm_fbi\\ctm_fbi_variantd.vmdl", "CounterTerrorist v2 | FBI"},
        {"characters\\models\\ctm_fbi\\ctm_fbi_variantb.vmdl", "Special Agent Ava | FBI"},
        {"characters\\models\\ctm_fbi\\ctm_fbi_variantc.vmdl", "Markus Delrow | FBI HRT"},
		{"characters\\models\\ctm_fbi\\ctm_fbi_variantf.vmdl", "Markus Delrow v2| FBI HRT"},
        {"characters\\models\\ctm_fbi\\ctm_fbi_variantg.vmdl", "Operator | FBI SWAT"},
        {"characters\\models\\ctm_diver\\ctm_diver_varianta.vmdl", "Cmdr. Davida 'Goggles' Fernandez | SEAL Frogman"},
        {"characters\\models\\ctm_diver\\ctm_diver_variantb.vmdl", "Cmdr. Frank 'Wet Sox' Baroud | SEAL Frogman"},
        {"characters\\models\\ctm_diver\\ctm_diver_variantc.vmdl", "Lieutenant Rex Krikey | SEAL Frogman"},
        {"characters\\models\\ctm_gendarmerie\\ctm_gendarmerie_variantd.vmdl", "Chem-Haz Capitaine | Gendarmerie Nationale"},
        {"characters\\models\\ctm_gendarmerie\\ctm_gendarmerie_variante.vmdl", "Officer Jacques Beltram | Gendarmerie Nationale"},
        {"characters\\models\\ctm_gendarmerie\\ctm_gendarmerie_variantc.vmdl", "Chef d'Escadron Rouchard | Gendarmerie Nationale"},
        {"characters\\models\\ctm_gendarmerie\\ctm_gendarmerie_variantb.vmdl", "Chem-Haz Capitaine | Gendarmerie Nationale"},
        {"characters\\models\\ctm_gendarmerie\\ctm_gendarmerie_varianta.vmdl", "Sous-Lieutenant Medic | Gendarmerie Nationale"},
        {"characters\\models\\ctm_st6\\ctm_st6_variantg.vmdl", "'Blueberries' Buckshot | NSWC SEAL"},
		{"characters\\models\\ctm_st6\\ctm_st6_variantj.vmdl", "'Blueberries' Buckshot v2| NSWC SEAL"},
        {"characters\\models\\ctm_st6\\ctm_st6_varianti.vmdl", "'Lt. Commander Ricksaw | NSWC SEAL"},
        {"characters\\models\\ctm_st6\\ctm_st6_variantl.vmdl", "''Two Times' McCoy | USAF TACP"},
        {"characters\\models\\ctm_st6\\ctm_st6_variantm.vmdl", "''Two Times' McCoy v2| USAF TACP"},
        {"characters\\models\\ctm_st6\\ctm_st6_variantn.vmdl", "'Primeiro Tenente | Brazilian 1st Battalion"},
        {"characters\\models\\ctm_swat\\ctm_swat_variante.vmdl", "'Cmdr. Mae 'Dead Cold' Jamison | SWAT"},
        {"characters\\models\\ctm_swat\\ctm_swat_variantk.vmdl", "'Cmdr. Mae 'Dead Cold' Jamison v2 | SWAT"},
        {"characters\\models\\ctm_swat\\ctm_swat_variantf.vmdl", "'1st Lieutenant Farlow | SWAT"},
        {"characters\\models\\ctm_swat\\ctm_swat_variantg.vmdl", "John 'Van Healen' Kask | SWAT"},
        {"characters\\models\\ctm_swat\\ctm_swat_varianth.vmdl", "Bio-Haz Specialist | SWAT"},
        {"characters\\models\\ctm_swat\\ctm_swat_varianti.vmdl", "Sergeant Bombson | SWAT"},
        {"characters\\models\\ctm_swat\\ctm_swat_variantj.vmdl", "Chem-Haz Specialist | SWAT"}
    };

    internal static readonly Dictionary<string, string> agentsListTT = new()
    {
        {"characters\\models\\tm_balkan\\tm_balkan_variantg.vmdl", "Rezan the Redshirt | Sabre"},
        {"characters\\models\\tm_balkan\\tm_balkan_varianth.vmdl", "Rezan the Redshirt v2 | Sabre"},
        {"characters\\models\\tm_balkan\\tm_balkan_variantf.vmdl", "Dragomir | Sabre Footsoldier"},
        {"characters\\models\\tm_balkan\\tm_balkan_variantk.vmdl", "'The Doctor' Romanov | Sabre"},
        {"characters\\models\\tm_balkan\\tm_balkan_varianti.vmdl", "Maximus | Sabre"},
        {"characters\\models\\tm_balkan\\tm_balkan_variantj.vmdl", "Blackwolf | Sabre"},
        {"characters\\models\\tm_balkan\\tm_balkan_variantl.vmdl", "Dragomir v2 | Sabre Footsoldier"},
        {"characters\\models\\tm_jumpsuit\\tm_jumpsuit_varianta.vmdl", "Black | Yellow jacket"},
        {"characters\\models\\tm_jumpsuit\\tm_jumpsuit_variantb.vmdl", "Beard | Yellow jacket"},
        {"characters\\models\\tm_jumpsuit\\tm_jumpsuit_variantc.vmdl", "Bald | Yellow jacket"},
        {"characters\\models\\tm_jungle_raider\\tm_jungle_raider_variantb.vmdl", "'Medium Rare' Crasswater | Guerrilla Warfare"},
        {"characters\\models\\tm_jungle_raider\\tm_jungle_raider_variantb2.vmdl", "'Medium Rare' Crasswater v2 | Guerrilla Warfare"},
        {"characters\\models\\tm_jungle_raider\\tm_jungle_raider_variantc.vmdl", "Arno The Overgrown | Guerrilla Warfare"},
        {"characters\\models\\tm_jungle_raider\\tm_jungle_raider_variante.vmdl", "Vypa Sista of the Revolution | Guerrilla Warfare"},
        {"characters\\models\\tm_jungle_raider\\tm_jungle_raider_variantd.vmdl", "Col. Mangos Dabisi | Guerrilla Warfare"},
        {"characters\\models\\tm_jungle_raider\\tm_jungle_raider_variantf.vmdl", "Trapper Aggressor | Guerrilla Warfare"},
        {"characters\\models\\tm_jungle_raider\\tm_jungle_raider_variantf2.vmdl", "Trapper Aggressor v2 | Guerrilla Warfare"},
        {"characters\\models\\tm_leet\\tm_leet_varianta.vmdl", "Osiris | Elite Crew"},
        {"characters\\models\\tm_leet\\tm_leet_variantb.vmdl", "Jungle Rebel | Elite Crew"},
        {"characters\\models\\tm_leet\\tm_leet_variantj.vmdl", "Jungle Rebel v2 | Elite Crew"},
        {"characters\\models\\tm_leet\\tm_leet_variantc.vmdl", "Prof. Shahmat | Elite Crew"},
        {"characters\\models\\tm_leet\\tm_leet_varianti.vmdl", "Prof. Shahmat v2 | Elite Crew"},
        {"characters\\models\\tm_leet\\tm_leet_variantd.vmdl", "Ground Rebel | Elite Crew"},
        {"characters\\models\\tm_leet\\tm_leet_variante.vmdl", "Ground Rebel v2 | Elite Crew"},
        {"characters\\models\\tm_leet\\tm_leet_variantg.vmdl", "Ground Rebel v3 | Elite Crew"},
        {"characters\\models\\tm_leet\\tm_leet_varianth.vmdl", "Osiris | Elite Crew"},
        {"characters\\models\\tm_leet\\tm_leet_variantf.vmdl", "The Elite Mr. Muhlik | Elite Crew"},
        {"characters\\models\\tm_phoenix\\tm_phoenix_varianta.vmdl", "Terrorist | Phoenix"},
        {"characters\\models\\tm_phoenix\\tm_phoenix_variantb.vmdl", "Terrorist v2 | Phoenix"},
        {"characters\\models\\tm_phoenix\\tm_phoenix_variantc.vmdl", "Terrorist v3 | Phoenix"},
        {"characters\\models\\tm_phoenix\\tm_phoenix_variantd.vmdl", "Terrorist v4 | Phoenix"},
		{"characters\\models\\tm_phoenix\\tm_phoenix_varianti.vmdl", "Terrorist v5 | Phoenix"},
        {"characters\\models\\tm_phoenix\\tm_phoenix_variantf.vmdl", "Enforcer | Phoenix"},
        {"characters\\models\\tm_phoenix\\tm_phoenix_varianth.vmdl", "Soldier | Phoenix"},
        {"characters\\models\\tm_phoenix\\tm_phoenix_variantg.vmdl", "Slingshot | Phoenix"},
        {"characters\\models\\tm_phoenix_heavy\\tm_phoenix_heavy.vmdl", "Heavy Soldier | Phoenix"},
        {"characters\\models\\tm_professional\\tm_professional_varf.vmdl", "Sir Bloody Miami Darryl | The Professionals"},
        {"characters\\models\\tm_professional\\tm_professional_varf1.vmdl", "Sir Bloody Silent Darryl | The Professionals"},
        {"characters\\models\\tm_professional\\tm_professional_varf2.vmdl", "Sir Bloody Skullhead Darryl | The Professionals"},
        {"characters\\models\\tm_professional\\tm_professional_varf3.vmdl", "Sir Bloody Darryl Royale | The Professionals"},
        {"characters\\models\\tm_professional\\tm_professional_varf4.vmdl", "Sir Bloody Loudmouth Darryl | The Professionals"},
        {"characters\\models\\tm_professional\\tm_professional_varf5.vmdl", "Bloody Darryl The Strapped | The Professionals"},
        {"characters\\models\\tm_professional\\tm_professional_varg.vmdl", "Safecracker Voltzmann | The Professionals"},
        {"characters\\models\\tm_professional\\tm_professional_varh.vmdl", "Little Kev | The Professionals"},
        {"characters\\models\\tm_professional\\tm_professional_vari.vmdl", "Number K | The Professionals"},
        {"characters\\models\\tm_professional\\tm_professional_varj.vmdl", "Getaway Sally | The Professionals"}
    };

    public override void Load(bool hotReload)
    {
        createDB();
        if (hotReload)
        {
            Utilities.GetPlayers().ForEach(player =>
            {
                getPlayerData(player);
            });
        }

        RegisterEventHandler<EventPlayerConnectFull>((@event, info) =>
        {
            var player = @event.Userid;

            if (player.IsBot || !player.IsValid)
            {
                return HookResult.Continue;

            }
            else
            {
                getPlayerData(player);
                return HookResult.Continue;
            }
        });

        RegisterEventHandler<EventPlayerDisconnect>((@event, info) =>
        {
            var player = @event.Userid;

            if (player.IsBot || !player.IsValid)
            {
                return HookResult.Continue;

            }
            else
            {
                if (gAgentsInfo.ContainsKey((int)player.Index))
                {
                    gAgentsInfo.Remove((int)player.Index);
                }
                return HookResult.Continue;
            }
        });

        RegisterEventHandler<EventPlayerSpawn>(eventPlayerSpawn);
    }


    [ConsoleCommand("css_agents", "Select Agents model.")]
    [CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void setupMainMenu(CCSPlayerController? player, CommandInfo info)
    {
        if (!IsValidClient(player, false))
        {
            return;
        }

        if (Config.AccessFlag != "" && !AdminManager.PlayerHasPermissions(player, Config.AccessFlag))
        {
            player.PrintToChat(Config.ChatTag + $"You dont have access to this command.");
            return;
        }

        var menu = new ChatMenu("Agents Menu");
        if (Config.UsableTeam == 2 || Config.UsableTeam == 3)
        {
            menu.AddMenuOption("CT Agents", (player, option) => {
                setupCTMenu(player);
            });
        }

        if (Config.UsableTeam == 1 || Config.UsableTeam == 3) 
        {
            menu.AddMenuOption("TT Agents", (player, option) => {
                setupTTMenu(player);
            });
        }

        MenuManager.OpenChatMenu(player, menu);
    }

    private void setupCTMenu(CCSPlayerController player)
    {
        var menu = new ChatMenu("Agents Menu - CT");
        menu.AddMenuOption("No agent model", (player, option) => {

            gAgentsInfo[(int)player.Index].AgentCT = "none";
            player.PrintToChat(Config.ChatTag + $"Set agent model to {ChatColors.Lime}" + "none" + $" {ChatColors.Green}on your next spawn");
            updatePlayer(player);
        });
        foreach (var agent in agentsListCT)
        {
            menu.AddMenuOption(agent.Value, (player, option) => {

                gAgentsInfo[(int)player.Index].AgentCT = agent.Key;
                player.PrintToChat(Config.ChatTag + $"Set agent model to {ChatColors.Lime}" + agent.Value);
                if (IsValidClient(player) && (CsTeam)player.TeamNum == CsTeam.CounterTerrorist)
                {
                    player.PlayerPawn.Value.SetModel(gAgentsInfo[(int)player.Index].AgentCT);
                    playerRefresh(player);
                }
                updatePlayer(player);
            });
        }
        menu.PostSelectAction = PostSelectAction.Close;
        MenuManager.OpenChatMenu(player, menu);
    }

    private void setupTTMenu(CCSPlayerController player)
    {
        var menu = new ChatMenu("Agents Menu - TT");
        menu.AddMenuOption("No agent model", (player, option) => {

            gAgentsInfo[(int)player.Index].AgentTT = "none";
            player.PrintToChat(Config.ChatTag + $"Set agent model to {ChatColors.Lime}" + "none" + $" {ChatColors.Green}on your next spawn");
            updatePlayer(player);
        });
        foreach (var agent in agentsListTT)
        {
            menu.AddMenuOption(agent.Value, (player, option) => {

                gAgentsInfo[(int)player.Index].AgentTT = agent.Key;
                player.PrintToChat(Config.ChatTag + $"Set agent model to {ChatColors.Lime}" + agent.Value);
                if (IsValidClient(player) && (CsTeam)player.TeamNum == CsTeam.Terrorist)
                {
                    player.PlayerPawn.Value.SetModel(gAgentsInfo[(int)player.Index].AgentTT);
                    playerRefresh(player);
                }
                updatePlayer(player);
            });
        }
        menu.PostSelectAction = PostSelectAction.Close;
        MenuManager.OpenChatMenu(player, menu);
    }

    private HookResult eventPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;

        if (!IsValidClient(player))
        {
            return HookResult.Continue;

        }

        AddTimer(2.5f, () =>
        {
            if (!IsValidClient(player))
            {
                return;
            }

            if ((CsTeam)player.TeamNum == CsTeam.CounterTerrorist) {

                var agent_ct = gAgentsInfo[(int)player.Index].AgentCT;
                if (agent_ct != null && !agent_ct.Equals("none"))
                {
                    player.PlayerPawn.Value.SetModel(agent_ct);
                }
            }
            else if ((CsTeam)player.TeamNum == CsTeam.Terrorist)
            {

                var agent_tt = gAgentsInfo[(int)player.Index].AgentTT;
                if (agent_tt != null && !agent_tt.Equals("none"))
                {
                    player.PlayerPawn.Value.SetModel(agent_tt);
                }
            }
        });
        
        return HookResult.Continue;
    }

    private void createDB()
    {
        if (Config.DatabaseType != "MySQL")
        {
            CreateTableSQLite();
        }
        else
        {
            CreateTableMySQL();
        }
    }

    private void updatePlayer(CCSPlayerController player)
    {
        if (Config.DatabaseType != "MySQL")
        {
            if (RecordExists(player))
            {
                _ = UpdateQueryDataSQLite(player);
            }
            else
            {
                _ = InsertQueryDataSQLite(player);
            }
        }
        else
        {
            if (RecordExists(player))
            {
                _ = UpdateQueryDataMySQL(player);
            }
            else
            {
                _ = InsertQueryDataMySQL(player);
            }
        }
    }

    private void CreateTableSQLite()
    {
        string dbFilePath = Server.GameDirectory + Config.DatabaseFilePath;

        var connectionString = $"Data Source={dbFilePath};";

        connectionSQLITE = new SqliteConnection(connectionString);

        connectionSQLITE.Open();

        var query = "CREATE TABLE IF NOT EXISTS franug_agents (steamid varchar(64) NOT NULL, agent_ct varchar(64), agent_tt varchar(64));";

        if (Config.DebugEnabled) Console.WriteLine("QUERY: " + query);

        using (SqliteCommand command = new SqliteCommand(query, connectionSQLITE))
        {
            command.ExecuteNonQuery();
        }
        connectionSQLITE.Close();
    }

    private void CreateTableMySQL()
    {
        var connectionString = $"Server={Config.DatabaseHost};Database={Config.DatabaseName};User Id={Config.DatabaseUser};Password={Config.DatabasePassword};";

        connectionMySQL = new MySqlConnection(connectionString);
        connectionMySQL.Open();

        using (MySqlCommand command = new MySqlCommand("CREATE TABLE IF NOT EXISTS `franug_agents` (`steamid` varchar(64) NOT NULL, agent_ct varchar(64) NOT NULL, agent_tt varchar(64) NOT NULL) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COLLATE=utf8mb3_unicode_ci;",
            connectionMySQL))
        {
            command.ExecuteNonQuery();
        }

        connectionMySQL.Close();
    }

    private void getPlayerData(CCSPlayerController player)
    {
        if (Config.DatabaseType != "MySQL")
        {
            _ = GetUserDataSQLite(player);
        }
        else
        {
            _ = GetUserDataMySQL(player);
        }
    }

    private bool RecordExists(CCSPlayerController player)
    {
        if (Config.DebugEnabled)
        {
            Console.WriteLine(gAgentsInfo[(int)player.Index].AgentCT != null && gAgentsInfo[(int)player.Index].AgentTT != null);
            Console.WriteLine("ct is " + gAgentsInfo[(int)player.Index].AgentCT + " t is " + gAgentsInfo[(int)player.Index].AgentTT);
        }

        return gAgentsInfo[(int)player.Index].AgentCT != null && gAgentsInfo[(int)player.Index].AgentTT != null;
    }

    public async Task InsertQueryDataSQLite(CCSPlayerController player)
    {
        try
        {
            if (gAgentsInfo[(int)player.Index].AgentCT == null)
            {
                gAgentsInfo[(int)player.Index].AgentCT = "none";
            }

            if (gAgentsInfo[(int)player.Index].AgentTT == null)
            {
                gAgentsInfo[(int)player.Index].AgentTT = "none";
            }

            await connectionSQLITE.OpenAsync();

            var query = "INSERT INTO franug_agents (steamid, agent_ct, agent_tt) VALUES (@steamid, @agent_ct, @agent_tt);";
            var command = new SqliteCommand(query, connectionSQLITE);

            command.Parameters.AddWithValue("@steamid", player.SteamID);
            command.Parameters.AddWithValue("@agent_ct", gAgentsInfo[(int)player.Index].AgentCT);
            command.Parameters.AddWithValue("@agent_tt", gAgentsInfo[(int)player.Index].AgentTT);
            if (Config.DebugEnabled) Console.WriteLine("QUERY: " + query);
            await command.ExecuteNonQueryAsync();
            connectionSQLITE?.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Franug-AgentsChooser] InsertQueryDataSQLite ******* An error occurred: {ex.Message}");
        }
        finally
        {
            await connectionSQLITE?.CloseAsync();
        }
    }

    public async Task UpdateQueryDataSQLite(CCSPlayerController player)
    {
        try
        {
            if (gAgentsInfo[(int)player.Index].AgentCT == null)
            {
                gAgentsInfo[(int)player.Index].AgentCT = "none";
            }

            if (gAgentsInfo[(int)player.Index].AgentTT == null)
            {
                gAgentsInfo[(int)player.Index].AgentTT = "none";
            }

            await connectionSQLITE.OpenAsync();

            var query = "UPDATE franug_agents SET agent_ct = @agent_ct, agent_tt = @agent_tt WHERE steamid = @steamid;";
            var command = new SqliteCommand(query, connectionSQLITE);

            command.Parameters.AddWithValue("@steamid", player.SteamID);
            command.Parameters.AddWithValue("@agent_ct", gAgentsInfo[(int)player.Index].AgentCT);
            command.Parameters.AddWithValue("@agent_tt", gAgentsInfo[(int)player.Index].AgentTT);
            if (Config.DebugEnabled) Console.WriteLine("QUERY: " + query);
            await command.ExecuteNonQueryAsync();
            connectionSQLITE?.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Franug-AgentsChooser] UpdateQueryDataSQLite ******* An error occurred: {ex.Message}");
        }
        finally
        {
            await connectionSQLITE?.CloseAsync();
        }
    }

    public async Task InsertQueryDataMySQL(CCSPlayerController player)
    {
        try
        {
            if (gAgentsInfo[(int)player.Index].AgentCT == null)
            {
                gAgentsInfo[(int)player.Index].AgentCT = "none";
            }

            if (gAgentsInfo[(int)player.Index].AgentTT == null)
            {
                gAgentsInfo[(int)player.Index].AgentTT = "none";
            }

            await connectionMySQL.OpenAsync();

            var query = "INSERT INTO franug_agents (steamid, agent_ct, agent_tt) VALUES (@steamid, @agent_ct, @agent_tt);";
            var command = new MySqlCommand(query, connectionMySQL);

            command.Parameters.AddWithValue("@steamid", player.SteamID);
            command.Parameters.AddWithValue("@agent_ct", gAgentsInfo[(int)player.Index].AgentCT);
            command.Parameters.AddWithValue("@agent_tt", gAgentsInfo[(int)player.Index].AgentTT);
            if (Config.DebugEnabled) Console.WriteLine("QUERY: " + query);

            await command.ExecuteNonQueryAsync();
            connectionMySQL?.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Franug-AgentsChooser] InsertQueryDataMySQL ******* An error occurred: {ex.Message}");
        }
        finally
        {
            await connectionMySQL?.CloseAsync();
        }
    }

    public async Task UpdateQueryDataMySQL(CCSPlayerController player)
    {
        try
        {
            if (gAgentsInfo[(int)player.Index].AgentCT == null)
            {
                gAgentsInfo[(int)player.Index].AgentCT = "none";
            }

            if (gAgentsInfo[(int)player.Index].AgentTT == null)
            {
                gAgentsInfo[(int)player.Index].AgentTT = "none";
            }

            await connectionMySQL.OpenAsync();

            var query = "UPDATE franug_agents SET agent_ct = @agent_ct, agent_tt = @agent_tt WHERE steamid = @steamid;";
            var command = new MySqlCommand(query, connectionMySQL);

            command.Parameters.AddWithValue("@steamid", player.SteamID);
            command.Parameters.AddWithValue("@agent_ct", gAgentsInfo[(int)player.Index].AgentCT);
            command.Parameters.AddWithValue("@agent_tt", gAgentsInfo[(int)player.Index].AgentTT);
            if (Config.DebugEnabled) Console.WriteLine("QUERY: " + query);
            await command.ExecuteNonQueryAsync();
            connectionMySQL?.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Franug-AgentsChooser] UpdateQueryDataMySQL ******* An error occurred: {ex.Message}");
        }
        finally
        {
            await connectionMySQL?.CloseAsync();
        }
    }

    public async Task GetUserDataSQLite(CCSPlayerController player)
    {
        try
        {
            await connectionSQLITE.OpenAsync();

            var query = "SELECT * FROM franug_agents WHERE steamid = @steamid;";

            var command = new SqliteCommand(query, connectionSQLITE);
            command.Parameters.AddWithValue("@steamid", player.SteamID);
            if (Config.DebugEnabled) Console.WriteLine("QUERY: " + query);
            var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var agent_ct = Convert.ToString(reader["agent_ct"]);
                var agent_tt = Convert.ToString(reader["agent_tt"]);

                AgentsInfo agentsInfo = new AgentsInfo
                {
                    AgentCT = agent_ct,
                    AgentTT = agent_tt,
                };

                gAgentsInfo[(int)player.Index] = agentsInfo;
            } 
            else
            {
                AgentsInfo agentsInfo = new AgentsInfo
                {
                    AgentCT = null,
                    AgentTT = null,
                };

                gAgentsInfo[(int)player.Index] = agentsInfo;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Franug-AgentsChooser] GetUserDataSQLite ******* An error occurred: {ex.Message}");
        }
        finally
        {
            await connectionSQLITE?.CloseAsync();
        }
    }

    public async Task GetUserDataMySQL(CCSPlayerController player)
    {
        try
        {
            await connectionMySQL.OpenAsync();

            var query = "SELECT * FROM franug_agents WHERE steamid = @steamid;";

            var command = new MySqlCommand(query, connectionMySQL);
            command.Parameters.AddWithValue("@steamid", player.SteamID);
            if (Config.DebugEnabled) Console.WriteLine("QUERY: " + query);
            var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var agent_ct = Convert.ToString(reader["agent_ct"]);
                var agent_tt = Convert.ToString(reader["agent_tt"]);

                AgentsInfo agentsInfo = new AgentsInfo
                {
                    AgentCT = agent_ct,
                    AgentTT = agent_tt,
                };

                gAgentsInfo[(int)player.Index] = agentsInfo;
            }
            else
            {
                AgentsInfo agentsInfo = new AgentsInfo
                {
                    AgentCT = null,
                    AgentTT = null,
                };

                gAgentsInfo[(int)player.Index] = agentsInfo;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Franug-AgentsChooser] GetUserDataMySQL ******* An error occurred: {ex.Message}");
        }
        finally
        {
            await connectionMySQL?.CloseAsync();
        }
    }

    private void playerRefresh(CCSPlayerController? player)
    {
        if (!IsValidClient(player)) return;

        AddTimer(0.18f, () => {
            if (!IsValidClient(player)) return;
            NativeAPI.IssueClientCommand((int)player.Index - 1, "slot3");
        });
        AddTimer(0.25f, () => {
            if (!IsValidClient(player)) return;
            NativeAPI.IssueClientCommand((int)player.Index - 1, "slot2");
        });
        AddTimer(0.38f, () => {
            if (!IsValidClient(player)) return;
            NativeAPI.IssueClientCommand((int)player.Index - 1, "slot1");
        });
    }

    private bool IsValidClient(CCSPlayerController client, bool isAlive = true)
    {
        return client != null && client.IsValid && client.PlayerPawn != null && client.PlayerPawn.IsValid && (!isAlive || client.PawnIsAlive) && !client.IsBot;
    }
}

public class AgentsInfo
{
    public String? AgentCT { get; set; }
    public String? AgentTT { get; set; }
}

