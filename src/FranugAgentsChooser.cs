using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using Microsoft.Data.Sqlite;

namespace FranugAgentsChooser;


[MinimumApiVersion(142)]
public class FranugAgentsChooser : BasePlugin
{
    public override string ModuleName => "Franug Agents Chooser";
    public override string ModuleAuthor => "Franc1sco Franug";
    public override string ModuleVersion => "0.0.1";

    private SqliteConnection? connectionSQLITE = null;
    internal static Dictionary<int, AgentsInfo> gAgentsInfo = new Dictionary<int, AgentsInfo>();


    public override void Load(bool hotReload)
    {
        CreateTableSQLite();
        if (hotReload)
        {
            Utilities.GetPlayers().ForEach(player =>
            {
                _ = GetUserDataSQLite(player);
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
                _ = GetUserDataSQLite(player);
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
    }

    private void updatePlayer(CCSPlayerController player)
    {
        if (RecordExistsSQLite(player))
        {
            _ = UpdateQueryDataSQLite(player);
        }
        else
        {
            _ = InsertQueryDataSQLite(player);
        }
    }

    private void CreateTableSQLite()
    {
        string dbFilePath = Server.GameDirectory + "/csgo/addons/counterstrikesharp/plugins/FranugAgentsChooser/franug-agentschooser-db.sqlite";

        var connectionString = $"Data Source={dbFilePath};";

        connectionSQLITE = new SqliteConnection(connectionString);

        connectionSQLITE.Open();

        var query = "CREATE TABLE IF NOT EXISTS franug_agents (steamid varchar(32) NOT NULL, agent_ct varchar(64), agent_tt varchar(64));";

        using (SqliteCommand command = new SqliteCommand(query, connectionSQLITE))
        {
            command.ExecuteNonQuery();
        }
        connectionSQLITE.Close();
    }

    private bool RecordExistsSQLite(CCSPlayerController player)
    {
        try
        {
            connectionSQLITE.Open();

            var query = "SELECT * FROM franug_agents WHERE steamid = @steamid;";
            var command = new SqliteCommand(query, connectionSQLITE);
            command.Parameters.AddWithValue("@steamid", player.SteamID);

            var reader = command.ExecuteReader();
            var exists = false;
            if (reader.Read())
            {
                exists = true;
            }

            connectionSQLITE.Close();
            return exists;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Franug-WeaponPaints] RecordExistsSQLite ******* An error occurred: {ex.Message}");

            return false;
        }
    }

    public async Task InsertQueryDataSQLite(CCSPlayerController player)
    {
        try
        {
            var agent_ct = gAgentsInfo[(int)player.Index].AgentCT;
            if (agent_ct == null)
            {
                agent_ct = "none";
            }

            var agent_tt = gAgentsInfo[(int)player.Index].AgentTT;
            if (agent_tt == null)
            {
                agent_tt = "none";
            }

            await connectionSQLITE.OpenAsync();

            var query = "INSERT INTO franug_agents (steamid, agent_ct, agent_tt) VALUES (@steamid, @agent_ct, @agent_tt);";
            var command = new SqliteCommand(query, connectionSQLITE);

            command.Parameters.AddWithValue("@steamid", player.SteamID);
            command.Parameters.AddWithValue("@agent_ct", agent_ct);
            command.Parameters.AddWithValue("@agent_tt", agent_tt);

            await command.ExecuteNonQueryAsync();
            connectionSQLITE?.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Franug-WeaponPaints] InsertQueryDataSQLite ******* An error occurred: {ex.Message}");
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
            var agent_ct = gAgentsInfo[(int)player.Index].AgentCT;
            if (agent_ct == null)
            {
                agent_ct = "none";
            }

            var agent_tt = gAgentsInfo[(int)player.Index].AgentTT;
            if (agent_tt == null)
            {
                agent_tt = "none";
            }

            await connectionSQLITE.OpenAsync();

            var query = "UPDATE franug_agents SET agent_ct = @agent_ct, agent_tt = @agent_tt WHERE steamid = @steamid;";
            var command = new SqliteCommand(query, connectionSQLITE);

            command.Parameters.AddWithValue("@steamid", player.SteamID);
            command.Parameters.AddWithValue("@agent_ct", agent_ct);
            command.Parameters.AddWithValue("@agent_tt", agent_tt);

            await command.ExecuteNonQueryAsync();
            connectionSQLITE?.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Franug-WeaponPaints] UpdateQueryDataSQLite ******* An error occurred: {ex.Message}");
        }
        finally
        {
            await connectionSQLITE?.CloseAsync();
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
            Console.WriteLine($"[Franug-WeaponPaints] GetUserDataSQLite ******* An error occurred: {ex.Message}");
        }
        finally
        {
            await connectionSQLITE?.CloseAsync();
        }
    }
}

public class AgentsInfo
{
    public String? AgentCT { get; set; }
    public String? AgentTT { get; set; }
}

