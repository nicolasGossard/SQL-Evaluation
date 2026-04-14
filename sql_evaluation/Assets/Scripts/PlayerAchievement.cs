// Ce script représente la relation entre un joueur et une réussite (achievement) dans la base de données
// Un joueur peut avoir plusieurs réussites, et une réussite peut être obtenue par plusieurs joueurs

public class PlayerAchievement
{
    public int player_id { get; set; }
    public int achievement_id { get; set; }
    public string unlocked_date { get; set; }
}