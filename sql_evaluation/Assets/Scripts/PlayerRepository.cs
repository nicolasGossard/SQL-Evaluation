using System;
using System.Linq;
using SQLite4Unity3d;

// Ce script gère les joueurs dans la base de données
// Il permet de créer un joueur, de récupérer un joueur par son nom ou son ID, de mettre à jour son expérience et son niveau, de récupérer le classement des joueurs (leaderboard)
// et de vérifier les conditions d'obtention des réussites (achievements) pour les débloquer et récompenser le joueur en conséquence

public class PlayerRepository
{
    private readonly SQLiteConnection _db;
    private const string TABLE_PLAYERS = "players";

    public PlayerRepository(SQLiteConnection db)
    {
        _db = db;
    }

    public void CreatePlayer(string name, string playerClass)
    {
        _db.Execute(
            $"INSERT INTO {TABLE_PLAYERS} (name, class, level, experience) VALUES (?, ?, 1, 0)",
            name, playerClass
        );
    }

    public PlayerData GetPlayerByName(string name)
    {
        return _db.Query<PlayerData>(
            $"SELECT * FROM {TABLE_PLAYERS} WHERE name = ?",
            name
        ).FirstOrDefault();
    }

    public PlayerData GetPlayerById(int id)
    {
        return _db.Query<PlayerData>(
            $"SELECT * FROM {TABLE_PLAYERS} WHERE id = ?",
            id
        ).FirstOrDefault();
    }

    public void UpdatePlayerExperience(int playerId, int xpGain)
    {
        _db.RunInTransaction(() =>
        {
            var player = GetPlayerById(playerId);
            if (player == null) return;

            int xp = player.experience + xpGain;
            int level = player.level;

            while (xp >= 100)
            {
                xp -= 100;
                level++;
            }

            _db.Execute(
                $"UPDATE {TABLE_PLAYERS} SET experience = ?, level = ? WHERE id = ?",
                xp, level, playerId
            );
        });
    }

    public PlayerStats[] GetLeaderboard()
    {
        return _db.Query<PlayerStats>(
            "SELECT * FROM vw_PlayerStats ORDER BY total_xp DESC, quests_completed DESC LIMIT 5"
        ).ToArray();
    }

    public void CheckAndUnlockAchievements(int playerId)
    {
        var player = GetPlayerById(playerId);
        if (player == null) return;

        int totalXp = player.level * 100 + player.experience;
        var achievements = _db.Query<Achievement>("SELECT * FROM achievements");

        foreach (var a in achievements)
        {
            bool unlocked = false;

            if (a.condition_type == "level" && player.level >= a.condition_value)
                unlocked = true;

            if (a.condition_type == "xp_total" && totalXp >= a.condition_value)
                unlocked = true;

            if (a.condition_type == "quests_completed")
            {
                int count = _db.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM player_quests WHERE player_id = ? AND status = 'completed'",
                    playerId
                );

                if (count >= a.condition_value)
                    unlocked = true;
            }

            var exists = _db.Query<PlayerAchievement>(
                "SELECT * FROM player_achievements WHERE player_id = ? AND achievement_id = ?",
                playerId, a.id
            );

            if (unlocked && exists.Count == 0)
            {
                _db.Execute(
                    "INSERT INTO player_achievements VALUES (?, ?, ?)",
                    playerId, a.id, DateTime.Now.ToString()
                );

                UpdatePlayerExperience(playerId, a.reward_xp);
            }
        }
    }

    public void DeletePlayer(int playerId)
    {
        _db.RunInTransaction(() =>
        {
            _db.Execute("DELETE FROM players WHERE id = ?", playerId);
        });
    }
}