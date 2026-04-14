// Ce script représente les statistiques d'un joueur pour l'affichage du leaderboard
// Il correspond à la vue vw_PlayerStats dans la base de données, qui agrège les données des joueurs,
//de leurs quêtes et de leurs réussites pour calculer le total d'XP et le nombre de quêtes complétées

using SQLite4Unity3d;

public class PlayerStats
{
    public int id { get; set; }
    public string name { get; set; }

    [Column("class")]
    public string playerClass { get; set; }

    public int level { get; set; }
    public int experience { get; set; }

    public int total_xp { get; set; }
    public int quests_completed { get; set; }
    public int achievements_unlocked { get; set; }
}