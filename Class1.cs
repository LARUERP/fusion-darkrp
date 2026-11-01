
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(FusionDarkRP.MainMod), "Fusion DarkRP", "2.0.1", "LARUERP")]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]

namespace FusionDarkRP
{
    public class MainMod : MelonMod
    {
        private const string TEAM_KEY = "FusionDarkRP.Team";
        private const string GRADE_KEY = "FusionDarkRP.Grade";

        private static bool menuCree;
        private static bool demandeInitialeEnvoyee;
        private static DateTime prochaineTentative;

        private static readonly Dictionary<byte, object> pagesJoueurs =
            new Dictionary<byte, object>();

        private static readonly HashSet<byte> pagesGestionOuvertes =
            new HashSet<byte>();

        public override void OnInitializeMelon()
        {
            try
            {
                DarkRPNetwork.Register();
                MelonLogger.Msg("[Fusion DarkRP] Reseau initialise.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[Fusion DarkRP] Erreur reseau : " + ex);
            }
        }

        public override void OnUpdate()
        {
            if (DateTime.UtcNow < prochaineTentative)
                return;

            prochaineTentative = DateTime.UtcNow.AddSeconds(3);

            if (!demandeInitialeEnvoyee)
                InitialiserCitoyen();

            if (!menuCree)
                CreerMenu();
        }

        private static void InitialiserCitoyen()
        {
            try
            {
                object local = ObtenirJoueurLocal();

                if (local == null)
                    return;

                byte id = Convert.ToByte(
                    local.GetType().GetProperty("SmallID").GetValue(local)
                );

                object metadata = local.GetType()
                    .GetProperty("Metadata").GetValue(local);

                object data = metadata.GetType()
                    .GetProperty("Metadata").GetValue(metadata);

                string equipe = ReadMetadata(data, TEAM_KEY);
                string grade = ReadMetadata(data, GRADE_KEY);

                if (!string.IsNullOrEmpty(equipe) &&
                    !string.IsNullOrEmpty(grade))
                {
                    demandeInitialeEnvoyee = true;
                    return;
                }

                // Une seule demande initiale : ne pas spammer le serveur
                // si la reponse metadata tarde a arriver.
                DarkRPNetwork.Request(id, 0, 0);
                demandeInitialeEnvoyee = true;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[Fusion DarkRP] Initialisation citoyen : " + ex.Message
                );
            }
        }

        private static object ObtenirJoueurLocal()
        {
            try
            {
                Assembly fusion = TrouverAssembly("LabFusion");
                Type manager = fusion?.GetType(
                    "LabFusion.Player.PlayerIDManager"
                );

                if (manager == null)
                    return null;

                PropertyInfo property = manager.GetProperty(
                    "LocalID",
                    BindingFlags.Public | BindingFlags.Static
                );

                return property?.GetValue(null);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[Fusion DarkRP] Recuperation joueur local : " + ex.Message
                );

                return null;
            }
        }

        private static void CreerMenu()
        {
            try
            {
                Assembly bone = TrouverAssembly("BoneLib");

                Type pageType = bone?.GetType("BoneLib.BoneMenu.Page");
                Type colorType = Type.GetType(
                    "UnityEngine.Color, UnityEngine.CoreModule"
                );

                if (pageType == null || colorType == null)
                    return;

                object root = pageType.GetProperty(
                    "Root",
                    BindingFlags.Public | BindingFlags.Static
                )?.GetValue(null);

                if (root == null)
                    return;

                MethodInfo createPage = null;

                foreach (MethodInfo method in pageType.GetMethods())
                {
                    if (method.Name == "CreatePage" &&
                        method.GetParameters().Length == 4)
                    {
                        createPage = method;
                        break;
                    }
                }

                MethodInfo createFunction = pageType.GetMethod(
                    "CreateFunction",
                    new[] { typeof(string), colorType, typeof(Action) }
                );

                if (createPage == null || createFunction == null)
                    return;

                object white = ColorValue(colorType, "white");
                object yellow = ColorValue(colorType, "yellow");
                object green = ColorValue(colorType, "green");
                object blue = ColorValue(colorType, "blue");
                object red = ColorValue(colorType, "red");
                object cyan = ColorValue(colorType, "cyan");
                object grey = ColorValue(colorType, "gray");

                object main = NewPage(
                    createPage, root, "Fusion DarkRP", yellow
                );

                object jobs = NewPage(
                    createPage, main, "Jobs publics", white
                );

                object factions = NewPage(
                    createPage, main, "Factions", grey
                );

                object hierarchy = NewPage(
                    createPage, main, "Hierarchies", white
                );

                object players = NewPage(
                    createPage, main, "Joueurs connectes", cyan
                );

                object character = NewPage(
                    createPage, main, "Mon personnage", green
                );

                AddFunction(
                    createFunction, jobs, "Redevenir citoyen", white,
                    () => RequestSelf(0, 0)
                );

                AddFunction(
                    createFunction, jobs, "Devenir eboueur", green,
                    () => RequestSelf(1, 1)
                );

                AddInfo(
                    createFunction, factions,
                    "Banque : recrutement obligatoire", yellow
                );

                AddInfo(
                    createFunction, factions,
                    "Mairie : recrutement obligatoire", cyan
                );

                AddInfo(
                    createFunction, factions,
                    "Epicerie : recrutement obligatoire", green
                );

                AddInfo(
                    createFunction, factions,
                    "Police : recrutement obligatoire", blue
                );

                AddInfo(
                    createFunction, factions,
                    "Medecins : recrutement obligatoire", green
                );

                AddInfo(
                    createFunction, factions,
                    "Les Marseillais : recrutement obligatoire", red
                );

                AddInfo(
                    createFunction, factions,
                    "Les Parisiens : recrutement obligatoire", blue
                );

                AddInfo(
                    createFunction, factions,
                    "Les Gens du Voyage : recrutement obligatoire", yellow
                );

                AddHierarchy(
                    createPage, createFunction, hierarchy,
                    "Mairie", 3, yellow, white
                );

                AddHierarchy(
                    createPage, createFunction, hierarchy,
                    "Police", 5, blue, white
                );

                AddHierarchy(
                    createPage, createFunction, hierarchy,
                    "Medecins", 6, green, white
                );

                AddHierarchy(
                    createPage, createFunction, hierarchy,
                    "Eboueurs", 1, green, white
                );

                AddHierarchy(
                    createPage, createFunction, hierarchy,
                    "Banque", 2, yellow, white
                );

                AddHierarchy(
                    createPage, createFunction, hierarchy,
                    "Epicerie", 4, green, white
                );

                AddHierarchy(
                    createPage, createFunction, hierarchy,
                    "Marseillais", 7, red, white
                );

                AddHierarchy(
                    createPage, createFunction, hierarchy,
                    "Parisiens", 8, blue, white
                );

                AddHierarchy(
                    createPage, createFunction, hierarchy,
                    "Gens du Voyage", 9, yellow, white
                );

                AddFunction(
                    createFunction, players,
                    "Actualiser les joueurs", cyan,
                    () => RefreshPlayers(
                        createPage, createFunction, players,
                        white, yellow, green, blue, red, cyan
                    )
                );

                AddFunction(
                    createFunction, character,
                    "Consulter mon metier et mon grade", white,
                    () => ShowStatus(
                        createPage, createFunction, character, white, green
                    )
                );

                AddFunction(
                    createFunction, character,
                    "Demissionner", green,
                    () => RequestSelf(0, 0)
                );

                menuCree = true;

                MelonLogger.Msg("[Fusion DarkRP] Menu cree.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[Fusion DarkRP] Creation menu : " + ex
                );
            }
        }

        private static void RequestSelf(byte team, byte grade)
        {
            try
            {
                object local = ObtenirJoueurLocal();

                if (local == null)
                {
                    MelonLogger.Warning(
                        "[Fusion DarkRP] Joueur local introuvable."
                    );

                    return;
                }

                byte id = Convert.ToByte(
                    local.GetType().GetProperty("SmallID").GetValue(local)
                );

                DarkRPNetwork.Request(id, team, grade);

                MelonLogger.Msg(
                    "[Fusion DarkRP] Demande envoyee au serveur."
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[Fusion DarkRP] Demande : " + ex
                );
            }
        }

        private static void AddHierarchy(
            MethodInfo createPage,
            MethodInfo createFunction,
            object parent,
            string name,
            byte team,
            object color,
            object white)
        {
            object page = NewPage(
                createPage, parent, name, color
            );

            string[] ranks = Ranks(team);

            for (int i = 0; i < ranks.Length; i++)
            {
                string gradeName = ranks[i];
                string displayName = DisplayGrade(gradeName, team);

                AddInfo(
                    createFunction,
                    page,
                    (i + 1) + ". " + displayName,
                    white
                );
            }

            AddInfo(
                createFunction,
                page,
                "Pour recruter ou promouvoir : Joueurs connectes > joueur > gestion",
                white
            );
        }

        private static void RefreshPlayers(
            MethodInfo createPage,
            MethodInfo createFunction,
            object parent,
            object white,
            object yellow,
            object green,
            object blue,
            object red,
            object cyan)
        {
            try
            {
                MethodInfo removePage = parent.GetType().GetMethod(
                    "RemovePage",
                    new[] { parent.GetType() }
                );

                if (removePage != null)
                {
                    foreach (object oldPage in pagesJoueurs.Values)
                    {
                        if (oldPage != null)
                            removePage.Invoke(parent, new[] { oldPage });
                    }
                }

                pagesJoueurs.Clear();
                pagesGestionOuvertes.Clear();

                Assembly fusion = TrouverAssembly("LabFusion");

                Type manager = fusion?.GetType(
                    "LabFusion.Player.PlayerIDManager"
                );

                FieldInfo field = manager?.GetField(
                    "PlayerIDs",
                    BindingFlags.Public | BindingFlags.Static
                );

                IEnumerable list = field?.GetValue(null) as IEnumerable;

                if (list == null)
                {
                    MelonLogger.Warning(
                        "[Fusion DarkRP] Liste des joueurs introuvable."
                    );

                    return;
                }

                foreach (object player in list)
                {
                    if (player == null || !ReadBool(player, "IsValid"))
                        continue;

                    byte id = Convert.ToByte(
                        player.GetType().GetProperty("SmallID").GetValue(player)
                    );

                    if (pagesJoueurs.ContainsKey(id))
                        continue;

                    bool isMe = ReadBool(player, "IsMe");

                    string name = PlayerName(player, id);

                    object playerPage = NewPage(
                        createPage,
                        parent,
                        name + " [ID " + id + "]",
                        cyan
                    );

                    string currentTeam = PlayerTeam(player);
                    string currentGrade = PlayerGrade(player);

                    byte currentTeamId = TeamId(currentTeam);

                    AddInfo(
                        createFunction,
                        playerPage,
                        "METIER : " + TeamDisplay(currentTeamId),
                        yellow
                    );

                    AddInfo(
                        createFunction,
                        playerPage,
                        "GRADE : " + DisplayGrade(
                            currentGrade, currentTeamId
                        ),
                        white
                    );

                    if (isMe)
                    {
                        object personalPage = NewPage(
                            createPage,
                            playerPage,
                            "Mon metier",
                            white
                        );

                        AddFunction(
                            createFunction,
                            personalPage,
                            "Demissionner",
                            white,
                            () => RequestSelf(0, 0)
                        );

                        AddFunction(
                            createFunction,
                            personalPage,
                            "Rejoindre les eboueurs",
                            green,
                            () => RequestSelf(1, 1)
                        );
                    }

                    AddFunction(
                        createFunction,
                        playerPage,
                        "Ouvrir gestion / recrutement",
                        yellow,
                        () => OpenPlayerManagement(
                            createPage,
                            createFunction,
                            playerPage,
                            id,
                            white,
                            yellow,
                            green,
                            blue,
                            red,
                            cyan
                        )
                    );

                    pagesJoueurs[id] = playerPage;
                }

                MelonLogger.Msg(
                    "[Fusion DarkRP] Liste des joueurs actualisee."
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[Fusion DarkRP] Actualisation joueurs : " + ex
                );
            }
        }

        private static void OpenPlayerManagement(
            MethodInfo createPage,
            MethodInfo createFunction,
            object playerPage,
            byte target,
            object white,
            object yellow,
            object green,
            object blue,
            object red,
            object cyan)
        {
            try
            {
                bool host = IsHost();
                bool manager = CanManageOwnTeam();

                if (!host && !manager)
                {
                    MelonLogger.Warning(
                        "[Fusion DarkRP] Acces refuse : seuls l'hote et les chefs peuvent recruter."
                    );

                    return;
                }

                if (pagesGestionOuvertes.Contains(target))
                {
                    MelonLogger.Msg(
                        "[Fusion DarkRP] La page de gestion de ce joueur existe deja."
                    );

                    return;
                }

                object managementPage = NewPage(
                    createPage,
                    playerPage,
                    "Recrutement / gestion",
                    yellow
                );

                if (host)
                {
                    AddAllTeams(
                        createPage,
                        createFunction,
                        managementPage,
                        target,
                        white,
                        yellow,
                        green,
                        blue,
                        red,
                        cyan
                    );

                    AddInfo(
                        createFunction,
                        managementPage,
                        "Droits hote : toutes les equipes et tous les grades",
                        yellow
                    );
                }
                else
                {
                    AddManagedTeam(
                        createPage,
                        createFunction,
                        managementPage,
                        target,
                        white,
                        yellow,
                        green,
                        blue,
                        red,
                        cyan
                    );

                    AddInfo(
                        createFunction,
                        managementPage,
                        "Tu peux gerer uniquement ta propre equipe",
                        yellow
                    );
                }

                pagesGestionOuvertes.Add(target);

                MelonLogger.Msg(
                    "[Fusion DarkRP] Page de gestion ouverte pour ID " + target
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[Fusion DarkRP] Ouverture gestion : " + ex
                );
            }
        }

        private static void AddAllTeams(
            MethodInfo cp,
            MethodInfo cf,
            object parent,
            byte target,
            object white,
            object yellow,
            object green,
            object blue,
            object red,
            object cyan)
        {
            AddTargetTeam(cp, cf, parent, target, 1, green, true);
            AddTargetTeam(cp, cf, parent, target, 2, yellow, true);
            AddTargetTeam(cp, cf, parent, target, 3, cyan, true);
            AddTargetTeam(cp, cf, parent, target, 4, green, true);
            AddTargetTeam(cp, cf, parent, target, 5, blue, true);
            AddTargetTeam(cp, cf, parent, target, 6, green, true);
            AddTargetTeam(cp, cf, parent, target, 7, red, true);
            AddTargetTeam(cp, cf, parent, target, 8, blue, true);
            AddTargetTeam(cp, cf, parent, target, 9, yellow, true);

            AddFunction(
                cf,
                parent,
                "Renvoyer citoyen",
                white,
                () => DarkRPNetwork.Request(target, 0, 0)
            );
        }

        private static void AddManagedTeam(
            MethodInfo cp,
            MethodInfo cf,
            object parent,
            byte target,
            object white,
            object yellow,
            object green,
            object blue,
            object red,
            object cyan)
        {
            string team = MyTeam();
            byte id = TeamId(team);

            if (id == 0)
                return;

            object color = green;

            if (team == "Banque" || team == "Mairie")
                color = yellow;

            if (team == "Police")
                color = blue;

            if (team == "Marseillais")
                color = red;

            if (team == "Parisiens")
                color = blue;

            if (team == "GensDuVoyage")
                color = yellow;

            AddTargetTeam(
                cp, cf, parent, target, id, color, false
            );

            AddFunction(
                cf,
                parent,
                "Renvoyer citoyen",
                white,
                () => DarkRPNetwork.Request(target, 0, 0)
            );
        }

        private static void AddTargetTeam(
            MethodInfo cp,
            MethodInfo cf,
            object parent,
            byte target,
            byte team,
            object color,
            bool includeTopRank)
        {
            object page = NewPage(
                cp,
                parent,
                TeamDisplay(team),
                color
            );

            string[] ranks = Ranks(team);

            for (int i = 0; i < ranks.Length; i++)
            {
                if (!includeTopRank && i == ranks.Length - 1)
                    continue;

                byte grade = GradeId(ranks[i]);

                string label = DisplayGrade(ranks[i], team);

                AddFunction(
                    cf,
                    page,
                    label,
                    color,
                    () => DarkRPNetwork.Request(target, team, grade)
                );
            }
        }

        private static bool CanManageOwnTeam()
        {
            string team = MyTeam();
            string grade = MyGrade();

            if (string.IsNullOrEmpty(team) ||
                string.IsNullOrEmpty(grade))
                return false;

            string[] ranks = Ranks(TeamId(team));

            if (ranks.Length == 0)
                return false;

            return grade == ranks[ranks.Length - 1];
        }

        private static string MyTeam()
        {
            object local = ObtenirJoueurLocal();

            object metadata = local?.GetType()
                .GetProperty("Metadata")?.GetValue(local);

            object data = metadata?.GetType()
                .GetProperty("Metadata")?.GetValue(metadata);

            return ReadMetadata(data, TEAM_KEY);
        }

        private static string MyGrade()
        {
            object local = ObtenirJoueurLocal();

            object metadata = local?.GetType()
                .GetProperty("Metadata")?.GetValue(local);

            object data = metadata?.GetType()
                .GetProperty("Metadata")?.GetValue(metadata);

            return ReadMetadata(data, GRADE_KEY);
        }

        private static void ShowStatus(
            MethodInfo createPage,
            MethodInfo createFunction,
            object characterPage,
            object white,
            object green)
        {
            try
            {
                // Lecture au moment du clic pour afficher les dernieres
                // metadonnees recues du serveur.
                string team = MyTeam();
                string grade = MyGrade();
                byte teamId = TeamId(team);

                if (string.IsNullOrEmpty(team))
                    team = "Citoyen";
                if (string.IsNullOrEmpty(grade))
                    grade = "Citoyen";

                object statusPage = NewPage(
                    createPage,
                    characterPage,
                    "Mon statut actuel",
                    green
                );

                AddInfo(
                    createFunction,
                    statusPage,
                    "METIER : " + TeamDisplay(teamId),
                    white
                );

                AddInfo(
                    createFunction,
                    statusPage,
                    "GRADE : " + DisplayGrade(grade, teamId),
                    white
                );

                AddInfo(
                    createFunction,
                    statusPage,
                    "Equipe technique : " + team,
                    white
                );

                AddInfo(
                    createFunction,
                    statusPage,
                    "Grade technique : " + grade,
                    white
                );

                MelonLogger.Msg(
                    "[Fusion DarkRP] Statut affiche : " +
                    TeamDisplay(teamId) + " / " +
                    DisplayGrade(grade, teamId)
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[Fusion DarkRP] Affichage du statut : " + ex
                );
            }
        }

        private static void AddInfo(
            MethodInfo method,
            object page,
            string label,
            object color)
        {
            AddFunction(
                method,
                page,
                label,
                color,
                () => MelonLogger.Msg(
                    "[Fusion DarkRP] " + label
                )
            );
        }

        private static void AddFunction(
            MethodInfo method,
            object page,
            string label,
            object color,
            Action action)
        {
            if (page != null)
            {
                method.Invoke(
                    page,
                    new object[] { label, color, action }
                );
            }
        }

        private static object NewPage(
            MethodInfo method,
            object parent,
            string name,
            object color)
        {
            return method.Invoke(
                parent,
                new object[] { name, color, 0, true }
            );
        }

        private static object ColorValue(Type type, string name)
        {
            return type.GetProperty(
                name,
                BindingFlags.Public | BindingFlags.Static
            )?.GetValue(null);
        }

        private static string[] Ranks(byte team)
        {
            switch (team)
            {
                case 1:
                    return new[] {
                        "Eboueur",
                        "ResponsableCollecte",
                        "ChefEboueurs",
                        "EboueurResponsableSecteur",
                        "EboueurDirecteurProprete"
                    };

                case 2:
                    return new[] {
                        "Guichetier",
                        "ConseillerBancaire",
                        "DirecteurAdjointBanque",
                        "DirecteurBanque",
                        "DirecteurAgence"
                    };

                case 3:
                    return new[] {
                        "ConseillerMunicipal",
                        "SecretaireMairie",
                        "AdjointMaire",
                        "PremierAdjointMairie",
                        "Maire"
                    };

                case 4:
                    return new[] {
                        "Vendeur",
                        "ResponsableRayon",
                        "GerantAdjoint",
                        "GerantEpicerie",
                        "GerantEpicerieTop"
                    };

                case 5:
                    return new[] {
                        "Policier",
                        "Sergent",
                        "LieutenantPolice",
                        "CommissaireAdjoint",
                        "ChefPolice"
                    };

                case 6:
                    return new[] {
                        "MedecinAmbulancier",
                        "Infirmier",
                        "MedecinAdjoint",
                        "Chirurgien",
                        "MedecinChef"
                    };

                case 7:
                case 8:
                case 9:
                    return new[] {
                        "Membre",
                        "Associe",
                        "LieutenantGang",
                        "BrasDroit",
                        "Patron"
                    };

                default:
                    return new[] { "Citoyen" };
            }
        }

        private static byte GradeId(string grade)
        {
            string[] names = {
                "Citoyen", "Eboueur", "ResponsableCollecte", "ChefEboueurs",
                "Guichetier", "ConseillerBancaire", "DirecteurAdjointBanque", "DirecteurBanque",
                "ConseillerMunicipal", "SecretaireMairie", "AdjointMaire", "Maire",
                "Vendeur", "ResponsableRayon", "GerantAdjoint", "GerantEpicerie",
                "Policier", "Sergent", "LieutenantPolice", "CommissaireAdjoint", "ChefPolice",
                "Infirmier", "Chirurgien", "MedecinAdjoint", "MedecinChef",
                "Membre", "Associe", "LieutenantGang", "BrasDroit", "Patron",
                "EboueurResponsableSecteur",
                "EboueurDirecteurProprete",
                "DirecteurAgence",
                "PremierAdjointMairie",
                "GerantEpicerieTop",
                "MedecinAmbulancier"
            };

            for (byte i = 0; i < names.Length; i++)
            {
                if (names[i] == grade)
                    return i;
            }

            return 0;
        }

        private static byte TeamId(string team)
        {
            string[] names = {
                "Citoyen",
                "Eboueurs",
                "Banque",
                "Mairie",
                "Epicerie",
                "Police",
                "Medecins",
                "Marseillais",
                "Parisiens",
                "GensDuVoyage"
            };

            for (byte i = 0; i < names.Length; i++)
            {
                if (names[i] == team)
                    return i;
            }

            return 0;
        }

        private static string TeamDisplay(byte id)
        {
            string[] names = {
                "Citoyen",
                "Eboueurs",
                "Banque",
                "Mairie",
                "Epicerie",
                "Police",
                "Medecins",
                "Les Marseillais",
                "Les Parisiens",
                "Les Gens du Voyage"
            };

            return id < names.Length ? names[id] : "Citoyen";
        }

        private static string DisplayGrade(string grade, byte team)
        {
            if (string.IsNullOrEmpty(grade))
                return "Citoyen";

            switch (team)
            {
                case 1:
                    switch (grade)
                    {
                        case "Eboueur": return "Agent de collecte";
                        case "ResponsableCollecte": return "Eboueur confirme";
                        case "ChefEboueurs": return "Chef d'equipe";
                        case "EboueurResponsableSecteur": return "Responsable de secteur";
                        case "EboueurDirecteurProprete": return "Directeur de la proprete";
                    }
                    break;

                case 2:
                    switch (grade)
                    {
                        case "Guichetier": return "Agent d'accueil";
                        case "ConseillerBancaire": return "Conseiller bancaire";
                        case "DirecteurAdjointBanque": return "Gestionnaire de comptes";
                        case "DirecteurBanque": return "Directeur adjoint";
                        case "DirecteurAgence": return "Directeur d'agence";
                    }
                    break;

                case 3:
                    switch (grade)
                    {
                        case "ConseillerMunicipal": return "Conseiller municipal";
                        case "SecretaireMairie": return "Secretaire general";
                        case "AdjointMaire": return "Adjoint au maire";
                        case "PremierAdjointMairie": return "Premier adjoint";
                        case "Maire": return "Maire";
                    }
                    break;

                case 4:
                    switch (grade)
                    {
                        case "Vendeur": return "Employe polyvalent";
                        case "ResponsableRayon": return "Vendeur";
                        case "GerantAdjoint": return "Responsable des rayons";
                        case "GerantEpicerie": return "Gerant adjoint";
                        case "GerantEpicerieTop": return "Gerant";
                    }
                    break;

                case 5:
                    switch (grade)
                    {
                        case "Policier": return "Gardien de la paix";
                        case "Sergent": return "Brigadier";
                        case "LieutenantPolice": return "Lieutenant";
                        case "CommissaireAdjoint": return "Commissaire";
                        case "ChefPolice": return "Commissaire divisionnaire";
                    }
                    break;

                case 6:
                    switch (grade)
                    {
                        case "MedecinAmbulancier": return "Ambulancier";
                        case "Infirmier": return "Infirmier";
                        case "MedecinAdjoint": return "Medecin";
                        case "Chirurgien": return "Chirurgien";
                        case "MedecinChef": return "Chef de service";
                    }
                    break;

                case 7:
                    switch (grade)
                    {
                        case "Membre": return "Cagole";
                        case "Associe": return "Guetteur";
                        case "LieutenantGang": return "Vendeur";
                        case "BrasDroit": return "Shooter";
                        case "Patron": return "Chef de reseau";
                    }
                    break;

                case 8:
                    switch (grade)
                    {
                        case "Membre": return "Petit nouveau";
                        case "Associe": return "Guetteur";
                        case "LieutenantGang": return "Charbonneur";
                        case "BrasDroit": return "Bras droit";
                        case "Patron": return "Baron du quartier";
                    }
                    break;

                case 9:
                    switch (grade)
                    {
                        case "Membre": return "Nouveau de la famille";
                        case "Associe": return "Voyageur";
                        case "LieutenantGang": return "Ancien";
                        case "BrasDroit": return "Chef de caravane";
                        case "Patron": return "Patriarche";
                    }
                    break;
            }

            return grade;
        }

        private static string PlayerName(object player, byte id)
        {
            try
            {
                Assembly fusion = TrouverAssembly("LabFusion");

                Type networkPlayerType = fusion?.GetType(
                    "LabFusion.Player.NetworkPlayer"
                );

                PropertyInfo playersProperty = networkPlayerType?.GetProperty(
                    "Players",
                    BindingFlags.Public | BindingFlags.Static
                );

                FieldInfo playersField = networkPlayerType?.GetField(
                    "Players",
                    BindingFlags.Public | BindingFlags.Static
                );

                IEnumerable networkPlayers =
                    playersProperty?.GetValue(null) as IEnumerable ??
                    playersField?.GetValue(null) as IEnumerable;

                if (networkPlayers != null)
                {
                    foreach (object networkPlayer in networkPlayers)
                    {
                        if (networkPlayer == null)
                            continue;

                        object networkId = networkPlayer.GetType()
                            .GetProperty("PlayerID")?.GetValue(networkPlayer);

                        if (networkId == null)
                            continue;

                        byte networkSmallId = Convert.ToByte(
                            networkId.GetType()
                                .GetProperty("SmallID")
                                ?.GetValue(networkId)
                        );

                        if (networkSmallId != id)
                            continue;

                        string username = networkPlayer.GetType()
                            .GetProperty("Username")
                            ?.GetValue(networkPlayer) as string;

                        if (!string.IsNullOrWhiteSpace(username))
                            return username;
                    }
                }

                MethodInfo displayName = player.GetType().GetMethod(
                    "TryGetDisplayName",
                    new[] { typeof(string).MakeByRefType() }
                );

                if (displayName != null)
                {
                    object[] args = { null };

                    bool success = (bool)displayName.Invoke(player, args);

                    if (success && args[0] is string display &&
                        !string.IsNullOrWhiteSpace(display))
                    {
                        return display;
                    }
                }

                object metadata = player.GetType()
                    .GetProperty("Metadata")?.GetValue(player);

                if (metadata != null)
                {
                    string username = ReadPlayerNameProperty(
                        metadata, "Username"
                    );

                    if (!string.IsNullOrWhiteSpace(username))
                        return username;

                    string nickname = ReadPlayerNameProperty(
                        metadata, "Nickname"
                    );

                    if (!string.IsNullOrWhiteSpace(nickname))
                        return nickname;
                }

                return "Joueur ID " + id;
            }
            catch
            {
                return "Joueur ID " + id;
            }
        }

        private static string ReadPlayerNameProperty(
            object metadata,
            string propertyName)
        {
            try
            {
                object value = metadata.GetType()
                    .GetProperty(propertyName)?.GetValue(metadata);

                if (value == null)
                    return null;

                MethodInfo getValue = value.GetType().GetMethod("GetValue");

                if (getValue == null)
                    return value as string;

                return getValue.Invoke(value, null) as string;
            }
            catch
            {
                return null;
            }
        }

        private static string PlayerTeam(object player)
        {
            try
            {
                object metadata = player.GetType()
                    .GetProperty("Metadata")?.GetValue(player);

                object data = metadata?.GetType()
                    .GetProperty("Metadata")?.GetValue(metadata);

                return ReadMetadata(data, TEAM_KEY) ?? "Citoyen";
            }
            catch
            {
                return "Citoyen";
            }
        }

        private static string PlayerGrade(object player)
        {
            try
            {
                object metadata = player.GetType()
                    .GetProperty("Metadata")?.GetValue(player);

                object data = metadata?.GetType()
                    .GetProperty("Metadata")?.GetValue(metadata);

                return ReadMetadata(data, GRADE_KEY) ?? "Citoyen";
            }
            catch
            {
                return "Citoyen";
            }
        }

        private static string ReadMetadata(object metadata, string key)
        {
            if (metadata == null)
                return null;

            MethodInfo method = metadata.GetType().GetMethod(
                "GetMetadata",
                new[] { typeof(string) }
            );

            return method?.Invoke(
                metadata,
                new object[] { key }
            ) as string;
        }

        private static bool ReadBool(object obj, string name)
        {
            try
            {
                object value = obj.GetType()
                    .GetProperty(name)?.GetValue(obj);

                return value is bool && (bool)value;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsHost()
        {
            try
            {
                Assembly fusion = TrouverAssembly("LabFusion");

                Type type = fusion?.GetType(
                    "LabFusion.Network.NetworkInfo"
                );

                return (bool)type?.GetProperty(
                    "IsHost",
                    BindingFlags.Public | BindingFlags.Static
                )?.GetValue(null);
            }
            catch
            {
                return false;
            }
        }

        private static Assembly TrouverAssembly(string name)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name == name)
                    return assembly;
            }

            return null;
        }
    }
}
