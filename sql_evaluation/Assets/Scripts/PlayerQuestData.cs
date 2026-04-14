using SQLite4Unity3d;

// Ce script représente la relation entre un joueur et une quête dans la base de données
// Un joueur peut avoir une seule quête active à la fois, mais peut en avoir plusieurs dans son historique (quêtes terminées)

public class PlayerQuestData
{
    public int player_id { get; set; }
    public int quest_id { get; set; }

    public string status { get; set; }

    public int progress { get; set; }
}