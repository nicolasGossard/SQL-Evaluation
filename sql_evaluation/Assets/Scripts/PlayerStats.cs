// Ce script représente les statistiques d'un joueur pour l'affichage du leaderboard
// Il correspond à la vue vw_PlayerStats dans la base de données, qui agrège les données des joueurs,
//de leurs quêtes et de leurs réussites pour calculer le total d'XP et le nombre de quêtes complétées

public class PlayerStats
{
    public int id;
    public string name;
    public string playerClass;
    public int level;
    public int experience;

    public int total_xp;
    public int quests_completed;
}