using System;
using System.Linq;
using System.Collections.Generic;
using SQLite4Unity3d;
using UnityEngine;

// Ce script gère les quêtes dans la base de données
// Il permet d'assigner des quêtes aux joueurs, de mettre à jour leur progression, de récupérer les quêtes actives et l'historique des quêtes terminées
// Il gère aussi la génération de quêtes dynamiques et leur nettoyage

public class QuestRepository
{
    private readonly SQLiteConnection _db;

    public QuestRepository(SQLiteConnection db)
    {
        _db = db;
    }

    /// <summary>
    /// Assigne une quête à un joueur (1 seule active max)
    /// </summary>
    public void AssignQuest(int playerId, int questId)
    {
        int completed = _db.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM quest_history WHERE player_id = ?",
            playerId
        );

        if (completed >= 10)
            return;

        int active = _db.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM player_quests WHERE player_id = ?",
            playerId
        );

        if (active > 0)
            return;

        // ✅ INSERT propre avec noms de colonnes
        _db.Execute(
            "INSERT INTO player_quests (player_id, quest_id, status, progress) VALUES (?, ?, 'active', 0)",
            playerId, questId
        );
    }

    /// <summary>
    /// Met à jour la progression d’une quête
    /// </summary>
    public void UpdateQuestProgress(int playerId, int questId, int progress)
    {
        _db.RunInTransaction(() =>
        {
            _db.Execute(
                "UPDATE player_quests SET progress = progress + ? WHERE player_id = ? AND quest_id = ?",
                progress, playerId, questId
            );

            var quest = _db.Query<QuestData>(
                "SELECT * FROM quests WHERE quest_id = ?", questId
            ).FirstOrDefault();

            var pq = _db.Query<PlayerQuestData>(
                "SELECT * FROM player_quests WHERE player_id = ? AND quest_id = ?",
                playerId, questId
            ).FirstOrDefault();

            if (quest == null || pq == null) return;

            // 🔥 SI TERMINÉE
            if (pq.progress >= quest.target_progress)
            {
                // 1. Marquer complétée
                _db.Execute(
                    "UPDATE player_quests SET status = 'completed' WHERE player_id = ? AND quest_id = ?",
                    playerId, questId
                );

                // 2. Historique
                _db.Execute(
                    "INSERT INTO quest_history (player_id, quest_id, quest_name, completion_date, xp_gained) VALUES (?, ?, ?, ?, ?)",
                    playerId,
                    questId,
                    quest.name,
                    DateTime.Now.ToString(),
                    quest.reward_xp
                );

                // 3. XP (simple, cohérent avec ton système actuel)
                _db.Execute(
                    "UPDATE players SET experience = experience + ? WHERE id = ?",
                    quest.reward_xp,
                    playerId
                );

                // 4. 🔥 SUPPRESSION DE LA QUÊTE ACTIVE
                _db.Execute(
                    "DELETE FROM player_quests WHERE player_id = ? AND quest_id = ?",
                    playerId, questId
                );
            }
        });
    }

    /// <summary>
    /// Récupère les quêtes actives du joueur
    /// </summary>
    public List<PlayerQuestData> GetPlayerQuests(int playerId)
    {
        return _db.Query<PlayerQuestData>(
            "SELECT * FROM player_quests WHERE player_id = ?",
            playerId
        );
    }

    /// <summary>
    /// Historique des quêtes terminées
    /// </summary>
    public List<QuestHistoryData> GetQuestHistory(int playerId, int limit = 10)
    {
        return _db.Query<QuestHistoryData>(
            "SELECT * FROM quest_history WHERE player_id = ? ORDER BY completion_date DESC LIMIT ?",
            playerId, limit
        );
    }

    /// <summary>
    /// Toutes les quêtes disponibles
    /// </summary>
    public List<QuestData> GetAllQuests()
    {
        return _db.Query<QuestData>("SELECT * FROM quests");
    }

    /// <summary>
    /// Génère une quête dynamique (max 3 actives)
    /// </summary>
    public bool GenerateDynamicQuest(int playerId)
    {
        int count = _db.ExecuteScalar<int>(
            @"SELECT COUNT(*) FROM quests q
              JOIN player_quests pq ON q.quest_id = pq.quest_id
              WHERE q.is_dynamic = 1 AND pq.player_id = ?",
            playerId
        );

        if (count >= 3) return false;

        string questName = "Quête dynamique " + UnityEngine.Random.Range(100, 999);

        _db.Execute(
            "INSERT INTO quests (name, description, type, reward_xp, target_progress, is_dynamic) VALUES (?, ?, ?, ?, ?, 1)",
            questName,
            "Générée automatiquement",
            "dynamic",
            50,
            3
        );

        return true;
    }

    /// <summary>
    /// Nettoie les quêtes dynamiques non utilisées
    /// </summary>
    public void CleanDynamicQuests(int playerId)
    {
        _db.Execute(
            "DELETE FROM quests WHERE is_dynamic = 1 AND quest_id NOT IN (SELECT quest_id FROM player_quests)"
        );
    }
}