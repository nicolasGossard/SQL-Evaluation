using SQLite4Unity3d;

// Ce script représente une quête dans la base de données
// Chaque quête a un ID auto-incrémenté, un nom, une description, un type, une récompense en XP, un objectif de progression et un indicateur de quête dynamique
// Les quêtes dynamiques sont générées aléatoirement et n'ont pas de description fixe, juste un numéro

public class QuestData
{
    [PrimaryKey, AutoIncrement]
    public int quest_id { get; set; }

    public string name { get; set; }
    public string description { get; set; }
    public string type { get; set; }

    public int reward_xp { get; set; }
    public int target_progress { get; set; }

    public int is_dynamic { get; set; }
}