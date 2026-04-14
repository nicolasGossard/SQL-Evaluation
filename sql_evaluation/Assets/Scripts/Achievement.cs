// Ce script représente une réussite (achievement) dans la base de données
// Chaque réussite a un ID, un nom, une description, un type de condition, une valeur de condition et une récompense en XP

public class Achievement
{
    public int id { get; set; }
    public string name { get; set; }
    public string description { get; set; }
    public string condition_type { get; set; }
    public int condition_value { get; set; }
    public int reward_xp { get; set; }
}