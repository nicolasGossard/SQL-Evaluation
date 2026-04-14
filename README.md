# HERO QUEST – SQL Evaluation

## Description

Ce projet est un jeu Unity intégrant une base de données SQLite.
Il permet de créer un joueur, gérer des quêtes, gagner de l’expérience, débloquer des achievements et afficher un classement.

L’objectif principal est de mettre en place une architecture propre avec un **Data Access Layer (Repository Pattern)** et d’utiliser des fonctionnalités SQL avancées.

## Architecture

Le projet suit une séparation claire des responsabilités :

* **DatabaseManager**

  * Initialise la base de données
  * Instancie les repositories
  * Ne contient aucune requête SQL métier

* **PlayerRepository**

  * Gestion des joueurs (création, XP, classement)
  * Gestion des achievements

* **QuestRepository**

  * Gestion des quêtes
  * Assignation, progression et historique

## Fonctionnalités implémentées

### Achievements

* Déblocage automatique selon :

  * niveau
  * XP total
  * nombre de quêtes terminées
* Ajout d’XP bonus lors du déblocage

### Historique des quêtes

* Chaque quête terminée est enregistrée
* Les quêtes complétées ne réapparaissent plus dans le jeu

### Système de quêtes

* Sélection via dropdown
* Une seule quête active à la fois
* Deux modes :

  * progression
  * complétion instantanée

---

### Classement

* Les joueurs sont classés selon leur xp total.
* Plus un joueur fait de quêtes, plus il sera haut dans le classement

### 2. Suppression logique des quêtes jouées

* Les quêtes terminées disparaissent de l’UI
* Mais restent stockées dans la base (historique)

---

## Comment Jouer ?

Quand le joueur lance le jeu, il ne peut pas réaliser une quête tout de suite. Il doit d'abord créé son joueur en lui donnant un nom
et une classe (Warrior, Mage ou Rogue), puis générer au moins une quête. Cela fait, il doit choisir une quête parmi les quêtes générées,
et peut décider de FAIRE LA QUÊTE, donc la terminer immédiatement, ou de COMMENCER LA QUÊTE, pour ensuite faire des PROGRESSION DE QUÊTE,
donc terminer la quête à son rythme.

Une fois la quête en cours terminée, elle disparait visuellement du jeu et est enregistrée dans la base de donnée comme "accomplie", le
joueur ne peut donc plus la refaire, il doit choisir une nouvelle quête à réaliser.

Chaque quête raporte 50 xp au joueur, et 100 xp lui font augmenter d'un niveau. Plus le joueur a d'xp (donc de niveaux), plus il sera en
haut du classement dans lequel se trouvent les autres joueurs ayant chaucun leur données aussi.

---
