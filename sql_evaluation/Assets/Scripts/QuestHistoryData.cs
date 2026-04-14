// Ce script représente l'historique des quêtes d'un joueur
// Chaque entrée contient l'ID du joueur, l'ID de la quête, le nom de la quête, la date de complétion et l'expérience gagnée

public class QuestHistoryData
{
    public int id { get; set; }
    public int player_id { get; set; }
    public int quest_id { get; set; }
    public string quest_name { get; set; }
    public string completion_date { get; set; }
    public int xp_gained { get; set; }
}