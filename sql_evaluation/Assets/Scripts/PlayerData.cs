using SQLite4Unity3d;

// Ce script représente un joueur dans la base de données
// On donne à chaque joueur un ID auto-incrémenté, un nom, une classe, un niveau et de l'expérience

public class PlayerData
{
    [PrimaryKey, AutoIncrement]
    public int id { get; set; }

    public string name { get; set; }

    [Column("class")]
    public string playerClass { get; set; }

    public int level { get; set; }
    public int experience { get; set; }
}