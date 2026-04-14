using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

// Ce script gère la logique principale du jeu : création de joueur, affichage des infos, gestion des quêtes, affichage du classement, etc.
// Il interagit avec les repositories pour manipuler les données dans la base de données et met à jour l'interface utilisateur en conséquence.
//C'est le cœur du jeu où tout se passe.

public class GameManager : MonoBehaviour
{
    [Header("Références UI")]
    // Références aux éléments UI pour afficher les infos du joueur, les quêtes, le leaderboard, etc.
    [SerializeField] private DatabaseManager dbManager;

    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private TMP_Dropdown classDropdown;
    [SerializeField] private TMP_Dropdown questDropdown;

    [SerializeField] private Button createButton;
    [SerializeField] private Button questButton;
    [SerializeField] private Button leaderboardButton;
    [SerializeField] private Button startQuestButton;
    [SerializeField] private Button progressQuestButton;
    [SerializeField] private Button generateQuestButton;

    [SerializeField] private TextMeshProUGUI playerInfoText;
    [SerializeField] private TextMeshProUGUI leaderboardText;
    [SerializeField] private TextMeshProUGUI questInfoText;

    // Données en mémoire
    private PlayerData currentPlayer;
    private List<int> questIds = new List<int>();

    // État de sélection de quête
    private bool questSelected = false;

    void Start()
    {
        // Ajouter les listeners aux boutons et au dropdown
        // Le bouton de création de joueur est toujours actif, les autres sont activés en fonction du contexte (ex : un joueur doit être créé pour faire des quêtes)
        createButton.onClick.AddListener(CreatePlayer);
        questButton.onClick.AddListener(DoQuest);
        leaderboardButton.onClick.AddListener(ShowLeaderboard);
        startQuestButton.onClick.AddListener(StartQuest);
        progressQuestButton.onClick.AddListener(ProgressQuest);
        generateQuestButton.onClick.AddListener(GenerateDynamicQuest);

        // Désactiver tous les boutons liés aux quêtes et au leaderboard au départ, car aucun joueur n'est encore créé
        // Seul le bouton de création de joueur est actif au départ
        questButton.interactable = false;
        startQuestButton.interactable = false;
        progressQuestButton.interactable = false;
        generateQuestButton.interactable = false;

        questDropdown.onValueChanged.AddListener(OnQuestSelected);

        // Désactiver boutons au départ
        startQuestButton.interactable = false;
        progressQuestButton.interactable = false;

        // Initialiser le dropdown des quêtes (sera mis à jour dynamiquement en fonction du joueur)
        PopulateQuestDropdown();
    }

    // Gérer la sélection d'une quête dans le dropdown
    void OnQuestSelected(int index)
    {
        // Si aucun joueur n'est sélectionné, ne rien faire
        if (currentPlayer == null) return;

        // Si l'option "Choisir une quête..." est sélectionnée, désactiver les boutons liés aux quêtes et ne pas considérer de quête comme sélectionnée
        // Sinon, activer les boutons liés aux quêtes et considérer la quête sélectionnée comme valide
        if (questIds[index] == -1)
        {
            questSelected = false;

            startQuestButton.interactable = false;
            questButton.interactable = false;

            return;
        }

        questSelected = true;

        var playerQuests = dbManager.QuestRepo.GetPlayerQuests(currentPlayer.id);

        startQuestButton.interactable = playerQuests.Count == 0;
        questButton.interactable = true;
    }

    // Gérer la création d'un joueur : vérifier si le nom est valide, créer le joueur dans la base de données s'il n'existe pas déjà,
    // récupérer ses données, mettre à jour l'interface utilisateur et activer les boutons liés aux quêtes
    void CreatePlayer()
    {
        // Récupérer le nom entré par l'utilisateur et le nettoyer (trim)
        string name = nameInput.text.Trim();

        // Vérifier que le nom n'est pas vide, sinon afficher un message d'erreur et ne pas continuer
        if (string.IsNullOrEmpty(name))
        {
            playerInfoText.text = "Nom invalide !";
            return;
        }

        currentPlayer = dbManager.PlayerRepo.GetPlayerByName(name);

        if (currentPlayer == null)
        {
            dbManager.PlayerRepo.CreatePlayer(name, classDropdown.options[classDropdown.value].text);
            currentPlayer = dbManager.PlayerRepo.GetPlayerByName(name);
        }

        UpdatePlayerUI();

        // Activer uniquement le bouton génération
        generateQuestButton.interactable = true;

        // Tout le reste reste bloqué
        startQuestButton.interactable = false;
        progressQuestButton.interactable = false;
        questButton.interactable = false;

        UpdateQuestInfo();
    }

    // Mettre à jour l'affichage des infos du joueur (nom, niveau, XP) et afficher un message optionnel (ex : gain d'XP après une quête)
    void UpdatePlayerUI(string extraMessage = "")
    {
        // Si aucun joueur n'est sélectionné, ne rien faire
        if (currentPlayer == null) return;

        // Afficher les infos du joueur dans le format : "Joueur : [nom] | Niv [niveau] | XP [expérience]"
        // Si un message supplémentaire est fourni (ex : gain d'XP après une quête), l'afficher au-dessus des infos du joueur
        playerInfoText.text =
            $"{extraMessage}\nJoueur : {currentPlayer.name} | Niv {currentPlayer.level} | XP {currentPlayer.experience}";
    }

    // Gérer la logique de faire une quête : vérifier si une quête active existe, sinon utiliser la quête sélectionnée dans le dropdown,
    // compléter la quête, vérifier les réussites débloquées, mettre à jour les infos du joueur, mettre à jour les quêtes disponibles et leur affichage
    void DoQuest()
    {
        // Si aucun joueur n'est sélectionné, ne rien faire
        if (currentPlayer == null) return;

        // Vérifier si une quête active existe pour le joueur, sinon utiliser la quête sélectionnée dans le dropdown
        int questId = -1;

        // Récupérer les quêtes actives du joueur
        var playerQuests = dbManager.QuestRepo.GetPlayerQuests(currentPlayer.id);

        // Si une quête active existe, la compléter, sinon vérifier si une quête est sélectionnée dans le dropdown et l'assigner avant de la compléter
        if (playerQuests.Count > 0)
        {
            questId = playerQuests.First().quest_id;
        }
        else
        {
            // Si aucune quête active n'existe, vérifier si une quête est sélectionnée dans le dropdown, sinon afficher un message d'erreur et ne pas continuer
            if (!questSelected)
            {
                questInfoText.text = "Choisissez une quête !";
                return;
            }

            questId = questIds[questDropdown.value];

            // On assigne la quête sélectionnée au joueur avant de la compléter
            dbManager.QuestRepo.AssignQuest(currentPlayer.id, questId);
        }

        var quest = dbManager.QuestRepo.GetAllQuests()
            .FirstOrDefault(q => q.quest_id == questId);

        // On met à jour la progression de la quête avec une valeur très élevée pour la compléter directement (dans un vrai jeu, ce serait basé sur des actions du joueur)
        dbManager.QuestRepo.UpdateQuestProgress(
            currentPlayer.id,
            questId,
            999
        );

        dbManager.PlayerRepo.CheckAndUnlockAchievements(currentPlayer.id);

        // Rafraîchir les données du joueur après la quête pour afficher les infos à jour (niveau, XP, etc.)
        currentPlayer = dbManager.PlayerRepo.GetPlayerById(currentPlayer.id);

        if (quest != null)
        {
            UpdatePlayerUI($"Quête {questId} terminée ! +{quest.reward_xp} XP");
        }
        else
        {
            UpdatePlayerUI($"Quête {questId} terminée !");
        }

        UpdateQuestInfo();

        // ⚠️ tu peux garder ou enlever selon ton choix
        // dbManager.QuestRepo.CleanDynamicQuests(currentPlayer.id);

        PopulateQuestDropdown();

        questDropdown.value = 0;
        questDropdown.RefreshShownValue();
        questSelected = false;
    }

    // Gérer la logique d'affichage des quêtes disponibles dans le dropdown : si aucun joueur n'est sélectionné, afficher toutes les quêtes,
    // sinon afficher uniquement les quêtes non complétées par le joueur
    void PopulateQuestDropdown()
    {
        // Si aucun joueur n'est sélectionné, afficher toutes les quêtes, sinon afficher uniquement les quêtes non complétées par le joueur
        questDropdown.ClearOptions();
        questIds.Clear();

        // Ajouter une option par défaut pour inviter à choisir une quête, avec un ID de -1 pour indiquer qu'aucune quête n'est sélectionnée
        questDropdown.options.Add(new TMP_Dropdown.OptionData("Choisir une quête..."));
        questIds.Add(-1);

        var allQuests = dbManager.QuestRepo.GetAllQuests();

        // Si aucun joueur n'est sélectionné, afficher toutes les quêtes, sinon afficher uniquement les quêtes non complétées par le joueur
        // Récupérer l'historique des quêtes complétées par le joueur pour filtrer les quêtes disponibles
        if (currentPlayer == null)
        {
            foreach (var q in allQuests)
            {
                questDropdown.options.Add(new TMP_Dropdown.OptionData($"{q.quest_id} - {q.name}"));
                questIds.Add(q.quest_id);
            }
        }
        else
        {
            var completed = dbManager.QuestRepo.GetQuestHistory(currentPlayer.id);

            var availableQuests = allQuests
                .Where(q => !completed.Any(c => c.quest_id == q.quest_id))
                .ToList();

            foreach (var q in availableQuests)
            {
                questDropdown.options.Add(new TMP_Dropdown.OptionData($"{q.quest_id} - {q.name}"));
                questIds.Add(q.quest_id);
            }
        }

        questDropdown.value = 0;
        questDropdown.RefreshShownValue();

        questSelected = false;
    }

    // Gérer la logique de démarrage d'une quête : vérifier si une quête est sélectionnée, sinon afficher un message d'erreur,
    void StartQuest()
    {
        if (!questSelected)
        {
            questInfoText.text = "Choisissez une quête d'abord !";
            return;
        }

        if (currentPlayer == null || questIds.Count == 0) return;

        int questId = questIds[questDropdown.value];

        dbManager.QuestRepo.AssignQuest(currentPlayer.id, questId);
        UpdateQuestInfo();

        questDropdown.value = 0;
        questDropdown.RefreshShownValue();

        questSelected = false;
    }

    // Gérer la logique de progression d'une quête : vérifier si une quête active existe, sinon afficher un message d'erreur,
    // mettre à jour la progression de la quête, vérifier les réussites débloquées, mettre à jour les infos du joueur,
    // vérifier si la quête est terminée pour afficher un message de succès ou de progression, mettre à jour les quêtes disponibles et leur affichage
    void ProgressQuest()
    {
        if (currentPlayer == null) return;

        var playerQuests = dbManager.QuestRepo.GetPlayerQuests(currentPlayer.id);

        if (playerQuests.Count == 0)
        {
            questInfoText.text = "Aucune quête active !";
            return;
        }

        var activeQuest = playerQuests.First();
        int questId = activeQuest.quest_id;

        dbManager.QuestRepo.UpdateQuestProgress(currentPlayer.id, questId, 1);

        dbManager.PlayerRepo.CheckAndUnlockAchievements(currentPlayer.id);
        currentPlayer = dbManager.PlayerRepo.GetPlayerById(currentPlayer.id);

        var stillExists = dbManager.QuestRepo.GetPlayerQuests(currentPlayer.id)
    .Any(q => q.quest_id == questId);

        if (!stillExists)
        {
            UpdatePlayerUI($"Quête {questId} terminée avec succès !");
        }
        else
        {
            UpdatePlayerUI();
        }

        UpdateQuestInfo();
        PopulateQuestDropdown();
    }

    // Gérer la logique de génération d'une quête dynamique : vérifier si un joueur est sélectionné, générer une quête dynamique pour le joueur,
    // mettre à jour les quêtes disponibles et leur affichage, et afficher un message de succès si une quête a été générée ou un message
    // d'erreur sinon (ex : limite de quêtes complétées atteinte)
    void GenerateDynamicQuest()
    {
        if (currentPlayer == null) return;

        if (dbManager.QuestRepo.GenerateDynamicQuest(currentPlayer.id))
        {
            PopulateQuestDropdown();
        }
    }

    // Gérer la logique d'affichage du classement : récupérer les statistiques des joueurs pour le leaderboard, trier
    // les joueurs par XP total décroissant et nombre de quêtes complétées décroissant, afficher les 5 meilleurs joueurs
    // dans un format lisible (ex : "1. [nom] - Niv [niveau] - XP [expérience] - Quêtes complétées [nombre]") et afficher
    // un message si aucun joueur n'est disponible
    void ShowLeaderboard()
    {
        var leaderboard = dbManager.PlayerRepo.GetLeaderboard();

        string text = "CLASSEMENT :\n";

        for (int i = 0; i < leaderboard.Length; i++)
        {
            var p = leaderboard[i];
            text += $"{i + 1}. {p.name} - Niv {p.level} - XP {p.total_xp} - Quêtes {p.quests_completed}\n";
        }

        leaderboardText.text = text;
    }

    // Gérer la logique de mise à jour des infos des quêtes affichées : récupérer les quêtes actives du joueur, leur progression et leur statut,
    // récupérer l'historique des quêtes complétées pour calculer le nombre de quêtes complétées, activer ou désactiver les boutons liés aux quêtes
    // en fonction de la situation (ex : si une quête est active, désactiver le bouton de démarrage de quête, etc.) et afficher les infos des quêtes dans un format lisible
    void UpdateQuestInfo()
    {
        if (currentPlayer == null) return;

        var playerQuests = dbManager.QuestRepo.GetPlayerQuests(currentPlayer.id);
        var allQuests = dbManager.QuestRepo.GetAllQuests();

        int completed = dbManager.QuestRepo.GetQuestHistory(currentPlayer.id).Count;
        generateQuestButton.interactable = completed < 10;

        progressQuestButton.interactable = playerQuests.Count > 0;
        startQuestButton.interactable = playerQuests.Count == 0 && completed < 10;

        string text = "Quêtes :\n";

        foreach (var pq in playerQuests)
        {
            var quest = allQuests.FirstOrDefault(q => q.quest_id == pq.quest_id);

            if (quest != null)
            {
                text += $"{quest.name} | {pq.status} | {pq.progress}/{quest.target_progress} | +{quest.reward_xp} XP\n";
            }
        }

        questInfoText.text = text;
    }
}