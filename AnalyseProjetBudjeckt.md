Cahier des charges : Budjeckt (Version Bureau)

1. Objectif du projet
Développer une application de bureau Windows autonome permettant à un utilisateur de consigner et de suivre ses dépenses quotidiennes de manière locale, sans base de données lourde ni connexion internet.

2. Périmètre fonctionnel

    Ajout d'une dépense : L'utilisateur doit pouvoir saisir une dépense via un formulaire comportant :

        Le montant (valeur numérique).

        La catégorie (liste déroulante : Alimentation, Transports, Loisirs, Autre).

        La date (sélecteur de date, par défaut à la date du jour).

    Affichage de l'historique : Visualisation de toutes les dépenses enregistrées sous forme de tableau ou de liste, triées de la plus récente à la plus ancienne.

    Indicateur financier : Affichage dynamique du montant total des dépenses actuellement listées.

    Suppression : Possibilité de sélectionner une dépense dans la liste et de la supprimer définitivement.

    Filtrage : Menu déroulant permettant de n'afficher que les dépenses d'une catégorie spécifique.

3. Contraintes techniques

    Technologie : C# avec le framework .NET (version 6 ou supérieure).

    Interface Graphique : WPF (Windows Presentation Foundation) avec utilisation de XAML.

    Persistance des données : Sérialisation et désérialisation dans un fichier texte local au format JSON (ex: depenses.json).

    Mécanique d'interface : Utilisation du mécanisme de DataBinding propre à WPF (idéalement en respectant le motif d'architecture MVVM).

4. Interface Utilisateur (IHM)

    Application à fenêtre unique (Single Window).

    Interface divisée en trois zones claires :

        Zone de saisie (en haut ou sur le côté).

        Zone d'affichage des données (au centre).

        Zone de synthèse/total (en bas).

5. Critères d'acceptation et règles de gestion

    Le bouton d'ajout (ou la validation) doit être bloqué ou renvoyer une erreur visible si le champ montant est vide, contient du texte non numérique, ou si la valeur est inférieure ou égale à 0.

    Les données saisies doivent être sauvegardées dans le fichier JSON pour ne pas être perdues lors de la fermeture de l'application.

    Au lancement de l'application, les données du fichier JSON doivent être lues et chargées automatiquement dans la liste.

    Le montant total affiché doit se recalculer automatiquement à chaque ajout ou suppression d'une ligne, sans nécessiter de rafraîchissement manuel de l'interface.



Conception

BackEnd

Classe Budjeckt lv0 -> La classe principale

    attribue:
        string qui sera le nom de l'année (2024 ou 2026 ect) avec valeur par défaut l'année récupérer par le programme (sysdate)

        tableau taille 12 de MonthBudjet qui sera crée  avec chaque mois différents de l'année en tant que paramètre de constructeur -> yearsBudject (ou un nom plus cohérent)


    méthode:
        un get pour le nom de l'année
        en public : get et set pour la variable yearsBudject (ou le nom plus cohérent) 

        charger json -> méthode à décomposer proprement en sous méthode pour tout récupérer sur le json et charger correctement toutes les données -> vérifier que le json est correct (type, fichier ect)

        sauvegarder l'année -> même chose mais pour sauvegarder



Classe MonthBudjet     lv1 Elle permettra de contenir les catégories de dépense et le budjet de ce mois

    Constructeur: 
        constructeur qui peux remplir tout les attribues d'un coup nottament si jamais je veux charger un json

        et un autre
            nom du mois passé en paramètre str
            Le tableau de dépense Catégorie seront instancier par des valeur par défaut avec Loyer, Eau, Electricite, Chauffage, Alimentation, Transports, Loisirs
            toute les autre valeur seront mise à 0 par défaut 


    attribue:
        string -> nom du mois -> month
        flottant -> qui représentera le budjet du mois -> revenue (à anglaisisé)
        tableau de Tuple (int, string, float) -> id (auto incrémentation), nom, dépense faite -> tabDepenseCategorie  (à anglaisisé)  
        flottant -> total des dépenses (à anglaisisé)
        flottant -> qui représente ce qu'il reste dans le budjet (à anglaisisé)

    méthode:
        get pour chacun d'entre eux
        set pour chacun d'entre eux en privée

        Un méthode ajouter Catégorie de dépense avec: vérification que le nom est unique, il mettra la dépense faite à 0 et augmentera l'id de 1 par rapport au précédent

        une méthode ajouter facture Catégorie (par id), la facture dois être en entier positive

        une méthode pour calculer et modifier le total des dépenses

        idem pour le reste du budget


Classe outils pour vérifier que les conditions sont respecter lv1 (évidemment renvoyer une erreur avec le message si ça ne va pas)



Le front end tu le fait beau comme tu veux mais il doit respecter le cahier des charges
Et je te dirais après quoi changer ajouter

Le front end récuperera directeur ce dont il a besoin avec les get des classes, pour les afficher.


Test

Les jeux de test se font dès que tu as finit une méthode et une classe sur celle ci pour vérifier. TU créera et codera les jeux de test
