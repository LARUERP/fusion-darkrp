
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
        private static DateTime debutAttenteMetadonnees = DateTime.MinValue;
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

            if (!DarkRPNetwork.HasServer)
            {
                // Une nouvelle session Fusion doit pouvoir refaire
                // l'initialisation, meme apres une deconnexion.
                demandeInitialeEnvoyee = false;
                debutAttenteMetadonnees = DateTime.MinValue;
            }
            else if (!demandeInitialeEnvoyee)
            {
                InitialiserCitoyen();
            }

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
                    debutAttenteMetadonnees = DateTime.MinValue;
                    return;
                }

                // Ne jamais ecraser une affectation existante si les deux
                // metadonnees arrivent avec un leger decalage.
                if (!string.IsNullOrEmpty(equipe) ||
                    !string.IsNullOrEmpty(grade))
                    return;

                // Laisser a Fusion le temps de synchroniser les metadonnees
                // avant d'initialiser un nouveau joueur en citoyen.
                if (debutAttenteMetadonnees == DateTime.MinValue)
                    debutAttenteMetadonnees = DateTime.UtcNow;

                if (DateTime.UtcNow - debutAttenteMetadonnees <
                    TimeSpan.FromSeconds(8))
                    return;

                // Ne valider l'initialisation que si la demande a bien ete
                // envoyee a une session Fusion connectee.
                if (DarkRPNetwork.Request(id, 0, 0))
                {
                    demandeInitialeEnvoyee = true;
                    debutAttenteMetadonnees = DateTime.MinValue;
                }
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
                byte gradeId = DarkRPJobs.GradeId(gradeName);
                string displayName = DarkRPJobs.DisplayGrade(team, gradeId);
                if (string.IsNullOrEmpty(displayName))
                    displayName = gradeName;

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
                if (!DarkRPJobs.IsValidAssignment(team, grade))
                    continue;

                string label = DarkRPJobs.DisplayGrade(team, grade);

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

            byte teamId = TeamId(team);
            byte gradeId = DarkRPJobs.GradeId(grade);

            if (!DarkRPJobs.IsValidAssignment(teamId, gradeId))
                return false;

            DarkRPJob job = DarkRPJobs.GetJob(teamId);
            return job != null &&
                   DarkRPJobs.RankIndex(teamId, gradeId) == job.Grades.Length - 1;
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
                // Relire les metadonnees au moment du clic afin d'utiliser
                // les dernieres informations synchronisees par le serveur.
                string team = MyTeam();
                string grade = MyGrade();

                byte teamId = DarkRPJobs.TeamId(team);
                byte gradeId = DarkRPJobs.GradeId(grade);

                if (teamId == 255 || gradeId == 255 ||
                    !DarkRPJobs.IsValidAssignment(teamId, gradeId))
                {
                    teamId = 0;
                    gradeId = 0;
                }

                DarkRPNetwork.ShowNotification(teamId, gradeId);

                MelonLogger.Msg(
                    "[Fusion DarkRP] Statut notifie : " +
                    DarkRPJobs.TeamDisplay(teamId) + " / " +
                    DarkRPJobs.DisplayGrade(teamId, gradeId)
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
            DarkRPJob job = DarkRPJobs.GetJob(team);
            return job == null ? new string[0] : job.Grades;
        }

        private static byte GradeId(string grade)
        {
            return DarkRPJobs.GradeId(grade);
        }

        private static byte TeamId(string team)
        {
            DarkRPJob job = DarkRPJobs.GetJob(team);
            return job == null ? (byte)0 : job.Id;
        }

        private static string TeamDisplay(byte id)
        {
            return DarkRPJobs.TeamDisplay(id);
        }

        private static string DisplayGrade(string grade, byte team)
        {
            if (string.IsNullOrEmpty(grade))
                return "Citoyen";

            byte gradeId = DarkRPJobs.GradeId(grade);
            if (gradeId == 255)
                return grade;

            string display = DarkRPJobs.DisplayGrade(team, gradeId);
            return string.IsNullOrEmpty(display) ? grade : display;
        }

        private static string PlayerName(object player, byte id)
        {
            try
            {
                Assembly fusion = TrouverAssembly("LabFusion");

                Type networkPlayerType = fusion?.GetType(
                    "LabFusion.Entities.NetworkPlayer"
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

                        if (IsUsablePlayerName(username))
                            return username;
                    }
                }

                string fusionDisplayName = TryGetFusionDisplayName(
                    fusion, player
                );

                if (IsUsablePlayerName(fusionDisplayName))
                    return fusionDisplayName;

                object metadata = player.GetType()
                    .GetProperty("Metadata")?.GetValue(player);

                if (metadata != null)
                {
                    string username = ReadPlayerNameProperty(
                        metadata, "Username"
                    );

                    if (IsUsablePlayerName(username))
                        return username;

                    string nickname = ReadPlayerNameProperty(
                        metadata, "Nickname"
                    );

                    if (IsUsablePlayerName(nickname))
                        return nickname;
                }

                return "Joueur ID " + id;
            }
            catch
            {
                return "Joueur ID " + id;
            }
        }

        private static string TryGetFusionDisplayName(
            Assembly fusion,
            object player)
        {
            try
            {
                Type helperType = fusion?.GetType(
                    "LabFusion.Network.MetadataHelper"
                );

                if (helperType == null || player == null)
                    return null;

                foreach (MethodInfo method in helperType.GetMethods(
                    BindingFlags.Public | BindingFlags.Static))
                {
                    if (method.Name != "TryGetDisplayName")
                        continue;

                    ParameterInfo[] parameters = method.GetParameters();

                    if (parameters.Length != 2 ||
                        !parameters[0].ParameterType.IsInstanceOfType(player) ||
                        !parameters[1].ParameterType.IsByRef ||
                        parameters[1].ParameterType.GetElementType() != typeof(string))
                        continue;

                    object[] args = { player, null };
                    object result = method.Invoke(null, args);

                    if (result is bool success && success)
                        return args[1] as string;
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[Fusion DarkRP] Lecture du nom Fusion : " + ex.Message
                );
            }

            return null;
        }

        private static bool IsUsablePlayerName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            string value = name.Trim();

            return !string.Equals(value, "No Name",
                       StringComparison.OrdinalIgnoreCase) &&
                   !string.Equals(value, "Unknown",
                       StringComparison.OrdinalIgnoreCase) &&
                   !string.Equals(value, "Unknown Player",
                       StringComparison.OrdinalIgnoreCase);
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
