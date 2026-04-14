using UnityEngine;
using SQLite4Unity3d;

// Ce script gère la connexion à la base de données SQLite, l'initialisation des tables et des vues, et fournit des accès centralisés aux repositories pour les joueurs et les quêtes

public class DatabaseManager : MonoBehaviour
{
    // Singleton pour accéder facilement à la base de données depuis n'importe quel script du projet
    private static DatabaseManager instance;
    private SQLiteConnection connection;

    // Propriétés pour accéder aux repositories de joueurs et de quêtes, ce qui permet de centraliser la
    // logique d'accès à la base de données et de faciliter la maintenance du code
    public PlayerRepository PlayerRepo { get; private set; }
    public QuestRepository QuestRepo { get; private set; }

    // Initialisation de la base de données et des repositories dans la méthode Awake, ce qui garantit que la base de données est prête à être utilisée dès le début du jeu
    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        string dbPath = Application.persistentDataPath + "/HeroQuest.db";

        InitializeDatabase(dbPath);

        // Initialisation des repositories avec la connexion à la base de données, ce qui permet d'accéder facilement
        // aux méthodes de gestion des joueurs et des quêtes depuis les autres scripts du projet
        PlayerRepo = new PlayerRepository(connection);
        QuestRepo = new QuestRepository(connection);
    }

    // Initialisation de la base de données : création des tables avec des contraintes d'intégrité, insertion de données pré-définies,
    // création d'une vue pour les statistiques des joueurs et ajout d'index pour optimiser les requêtes
    private void InitializeDatabase(string dbPath)
    {
        // Connexion à la base de données
        connection = new SQLiteConnection(dbPath);

        // Création des tables avec des contraintes d'intégrité et des index pour optimiser les requêtes
        connection.Execute(@"
        CREATE TABLE IF NOT EXISTS players (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT UNIQUE,
            class TEXT,
            level INTEGER CHECK(level >= 1),
            experience INTEGER CHECK(experience >= 0)
        )");

        // On ajoute une table de quêtes avec un champ pour indiquer si la quête est dynamique ou non,
        // ce qui permettra de différencier les quêtes pré-définies des quêtes générées dynamiquement
        connection.Execute(@"
        CREATE TABLE IF NOT EXISTS quests (
            quest_id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT,
            description TEXT,
            type TEXT,
            reward_xp INTEGER,
            target_progress INTEGER,
            is_dynamic INTEGER DEFAULT 0
        )");

        // Table d'association entre les joueurs et leurs quêtes actives, avec un statut pour indiquer si la quête est
        // active ou complétée, et un champ de progression pour suivre l'avancement du joueur dans la quête
        connection.Execute(@"
        CREATE TABLE IF NOT EXISTS player_quests (
            player_id INTEGER,
            quest_id INTEGER,
            status TEXT,
            progress INTEGER,
            PRIMARY KEY (player_id, quest_id),
            FOREIGN KEY (player_id) REFERENCES players(id) ON DELETE CASCADE,
            FOREIGN KEY (quest_id) REFERENCES quests(quest_id) ON DELETE CASCADE
        )");

        // Table pour stocker l'historique des quêtes complétées par les joueurs, avec la date de complétion et l'XP gagnée,
        // ce qui permettra de calculer le nombre de quêtes complétées et l'XP total pour les réussites et le leaderboard
        connection.Execute(@"
        CREATE TABLE IF NOT EXISTS achievements (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT,
            description TEXT,
            condition_type TEXT,
            condition_value INTEGER,
            reward_xp INTEGER
        )");

        // On insère des réussites pré-définies pour récompenser les joueurs en fonction de leurs niveaux, du nombre de quêtes complétées et de l'XP totale
        connection.Execute(@"
        INSERT OR IGNORE INTO achievements (id, name, description, condition_type, condition_value, reward_xp)
        VALUES 
        (1, 'Premier pas', 'Atteindre niveau 5', 'level', 5, 50),
        (2, 'Chasseur', '5 quêtes terminées', 'quests_completed', 5, 100),
        (3, 'Légende', '1000 XP', 'xp_total', 1000, 200)
        ");

        // Table d'association entre les joueurs et leurs réussites débloquées, avec la date de déblocage,
        // ce qui permettra de suivre les réussites obtenues par chaque joueur et de récompenser en conséquence
        connection.Execute(@"
        CREATE TABLE IF NOT EXISTS player_achievements (
            player_id INTEGER,
            achievement_id INTEGER,
            unlocked_date TEXT,
            PRIMARY KEY (player_id, achievement_id),
            FOREIGN KEY (player_id) REFERENCES players(id) ON DELETE CASCADE,
            FOREIGN KEY (achievement_id) REFERENCES achievements(id) ON DELETE CASCADE
        )");

        // Table pour stocker l'historique des quêtes complétées par les joueurs, avec la date de complétion et l'XP gagnée,
        // ce qui permettra de calculer le nombre de quêtes complétées et l'XP total pour les réussites et le leaderboard
        connection.Execute(@"
        CREATE TABLE IF NOT EXISTS quest_history (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            player_id INTEGER,
            quest_id INTEGER,
            quest_name TEXT,
            completion_date TEXT,
            xp_gained INTEGER
        )");

        // Création d'une vue pour agréger les statistiques des joueurs, incluant le total d'XP (calculé à partir du niveau et de l'expérience)
        // et le nombre de quêtes complétées, ce qui permettra d'afficher un leaderboard plus complet et de récompenser les joueurs en fonction
        // de leur progression globale
        connection.Execute("DROP VIEW IF EXISTS vw_PlayerStats");

        // La vue vw_PlayerStats agrège les données des joueurs, de leurs quêtes et de leurs réussites pour calculer le total d'XP et le nombre de quêtes complétées,
        // ce qui permettra d'afficher un leaderboard plus complet et de récompenser les joueurs en fonction de leur progression globale
        connection.Execute(@"
        CREATE VIEW vw_PlayerStats AS
        SELECT 
            p.id,
            p.name,
            p.class,
            p.level,
            p.experience,
            (p.level * 100 + p.experience) AS total_xp,

            COUNT(DISTINCT qh.quest_id) AS quests_completed,

            COUNT(DISTINCT pa.achievement_id) AS achievements_unlocked

        FROM players p

        LEFT JOIN quest_history qh 
            ON p.id = qh.player_id

        LEFT JOIN player_achievements pa 
            ON p.id = pa.player_id

        GROUP BY p.id
        ");

        // Index pour optimiser les recherches de joueurs par nom, ce qui est une opération fréquente dans le jeu pour la sélection du joueur et l'affichage du leaderboard
        connection.Execute("CREATE INDEX IF NOT EXISTS idx_player_name ON players(name)");

        Debug.Log("Base de données initialisée !");
    }

    // Fermer la connexion à la base de données lorsque le manager est détruit pour libérer les ressources
    void OnDestroy()
    {
        connection?.Close();
    }
}