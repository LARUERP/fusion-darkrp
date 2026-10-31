using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(FusionDarkRP.MainMod), "Fusion DarkRP", "2.0.0", "LARUERP")]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]

namespace FusionDarkRP
{
    public class MainMod : MelonMod
    {
        private const string TEAM_KEY = "FusionDarkRP.Team";
        private const string GRADE_KEY = "FusionDarkRP.Grade";

        public enum Equipe
        {
            Citoyen, Eboueurs, Banque, Mairie, Epicerie,
            Police, Medecins, Marseillais, Parisiens, GensDuVoyage
        }

        public enum Grade
        {
            Citoyen,
            Eboueur, ResponsableCollecte, ChefEboueurs,
            Guichetier, ConseillerBancaire, DirecteurAdjointBanque, DirecteurBanque,
            ConseillerMunicipal, SecretaireMairie, AdjointMaire, Maire,
            Vendeur, ResponsableRayon, GerantAdjoint, GerantEpicerie,
            Policier, Sergent, LieutenantPolice, CommissaireAdjoint, ChefPolice,
            Infirmier, Chirurgien, MedecinAdjoint, MedecinChef,
            Membre, Associe, LieutenantGang, BrasDroit, Patron
        }

        private static bool menuCree;
        private static bool demandeInitialeEnvoyee;
        private static DateTime prochaineTentative;
        private static readonly HashSet<byte> pagesJoueurs = new HashSet<byte>();

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
                if (local == null) return;

                byte id = Convert.ToByte(
                    local.GetType().GetProperty("SmallID").GetValue(local)
                );

                object metadata = local.GetType().GetProperty("Metadata").GetValue(local);
                object data = metadata.GetType().GetProperty("Metadata").GetValue(metadata);

                string equipe = LireMetadata(data, TEAM_KEY);
                string grade = LireMetadata(data, GRADE_KEY);

                if (string.IsNullOrEmpty(equipe) || string.IsNullOrEmpty(grade))
                    DarkRPNetwork.Request(id, 0, 0);

                demandeInitialeEnvoyee = true;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[Fusion DarkRP] Initialisation citoyen : " + ex.Message);
            }
        }

        private static object ObtenirJoueurLocal()
        {
            Assembly fusion = TrouverAssembly("LabFusion");
            Type localType = fusion?.GetType("LabFusion.Player.LocalPlayer");
            return localType?.GetProperty("PlayerID",
                BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
        }

        private static void CreerMenu()
        {
            try
            {
                Assembly bone = TrouverAssembly("BoneLib");
                Type pageType = bone?.GetType("BoneLib.BoneMenu.Page");
                Type colorType = Type.GetType("UnityEngine.Color, UnityEngine.CoreModule");

                if (pageType == null || colorType == null) return;

                object root = pageType.GetProperty("Root",
                    BindingFlags.Public | BindingFlags.Static)?.GetValue(null);

                if (root == null) return;

                MethodInfo createPage = null;
                foreach (MethodInfo m in pageType.GetMethods())
                {
                    if (m.Name == "CreatePage" && m.GetParameters().Length == 4)
                    {
                        createPage = m;
                        break;
                    }
                }

                MethodInfo createFunction = pageType.GetMethod(
                    "CreateFunction",
                    new[] { typeof(string), colorType, typeof(Action) }
                );

                if (createPage == null || createFunction == null) return;

                object white = ColorValue(colorType, "white");
                object yellow = ColorValue(colorType, "yellow");
                object green = ColorValue(colorType, "green");
                object blue = ColorValue(colorType, "blue");
                object red = ColorValue(colorType, "red");
                object cyan = ColorValue(colorType, "cyan");
                object grey = ColorValue(colorType, "gray");

                object main = NewPage(createPage, root, "Fusion DarkRP", yellow);
                object jobs = NewPage(createPage, main, "Jobs publics", white);
                object factions = NewPage(createPage, main, "Factions", grey);
                object hierarchy = NewPage(createPage, main, "Hierarchies", white);
                object players = NewPage(createPage, main, "Joueurs connectes", cyan);
                object character = NewPage(createPage, main, "Mon personnage", green);

                AddFunction(createFunction, jobs, "Redevenir citoyen", white,
                    () => RequestSelf(0, 0));

                AddFunction(createFunction, jobs, "Devenir eboueur", green,
                    () => RequestSelf(1, 1));

                AddInfo(createFunction, factions, "Banque : recrutement obligatoire", yellow);
                AddInfo(createFunction, factions, "Mairie : recrutement obligatoire", cyan);
                AddInfo(createFunction, factions, "Epicerie : recrutement obligatoire", green);
                AddInfo(createFunction, factions, "Police : recrutement obligatoire", blue);
                AddInfo(createFunction, factions, "Medecins : recrutement obligatoire", green);
                AddInfo(createFunction, factions, "Les Marseillais : recrutement obligatoire", red);
                AddInfo(createFunction, factions, "Les Parisiens : recrutement obligatoire", blue);
                AddInfo(createFunction, factions, "Les Gens du Voyage : recrutement obligatoire", yellow);

                AddHierarchy(createPage, createFunction, hierarchy, "Mairie", 3, yellow, white);
                AddHierarchy(createPage, createFunction, hierarchy, "Police", 5, blue, white);
                AddHierarchy(createPage, createFunction, hierarchy, "Medecins", 6, green, white);
                AddHierarchy(createPage, createFunction, hierarchy, "Eboueurs", 1, green, white);
                AddHierarchy(createPage, createFunction, hierarchy, "Banque", 2, yellow, white);
                AddHierarchy(createPage, createFunction, hierarchy, "Epicerie", 4, green, white);
                AddHierarchy(createPage, createFunction, hierarchy, "Marseillais", 7, red, white);
                AddHierarchy(createPage, createFunction, hierarchy, "Parisiens", 8, blue, white);
                AddHierarchy(createPage, createFunction, hierarchy, "Gens du Voyage", 9, yellow, white);

                AddFunction(createFunction, players, "Actualiser les joueurs", cyan,
                    () => RefreshPlayers(createPage, createFunction, players,
                        white, yellow, green, blue, red, cyan));

                AddFunction(createFunction, character, "Afficher mon statut", white,
                    ShowStatus);

                AddFunction(createFunction, character, "Redevenir citoyen", green,
                    () => RequestSelf(0, 0));

                menuCree = true;
                MelonLogger.Msg("[Fusion DarkRP] Menu cree.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[Fusion DarkRP] Creation menu : " + ex);
            }
        }

        private static void RequestSelf(byte team, byte grade)
        {
            try
            {
                object local = ObtenirJoueurLocal();
                if (local == null)
                {
                    MelonLogger.Warning("[Fusion DarkRP] Joueur local introuvable.");
                    return;
                }

                byte id = Convert.ToByte(local.GetType().GetProperty("SmallID").GetValue(local));
                DarkRPNetwork.Request(id, team, grade);

                MelonLogger.Msg("[Fusion DarkRP] Demande envoyee au serveur.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[Fusion DarkRP] Demande : " + ex);
            }
        }

        private static void AddHierarchy(
            MethodInfo createPage, MethodInfo createFunction,
            object parent, string name, byte team, object color, object white)
        {
            object page = NewPage(createPage, parent, name, color);

            string[] ranks = Ranks(team);
            for (int i = 0; i < ranks.Length; i++)
            {
                string gradeName = ranks[i];
                AddFunction(createFunction, page, DisplayGrade(gradeName), white,
                    () => MelonLogger.Msg("[Fusion DarkRP] Grade : " + DisplayGrade(gradeName)));
            }
        }

        private static void RefreshPlayers(
            MethodInfo createPage, MethodInfo createFunction,
            object parent, object white, object yellow, object green,
            object blue, object red, object cyan)
        {
            try
            {
                Assembly fusion = TrouverAssembly("LabFusion");
                Type manager = fusion?.GetType("LabFusion.Player.PlayerIDManager");
                FieldInfo field = manager?.GetField("PlayerIDs",
                    BindingFlags.Public | BindingFlags.Static);

                IEnumerable list = field?.GetValue(null) as IEnumerable;
                if (list == null)
                {
                    MelonLogger.Warning("[Fusion DarkRP] Liste des joueurs introuvable.");
                    return;
                }

                foreach (object player in list)
                {
                    if (player == null || !ReadBool(player, "IsValid")) continue;

                    byte id = Convert.ToByte(player.GetType()
                        .GetProperty("SmallID").GetValue(player));

                    if (pagesJoueurs.Contains(id)) continue;

                    bool isMe = ReadBool(player, "IsMe");
                    string name = PlayerName(player);

                    object playerPage = NewPage(
                        createPage, parent, name + " [ID " + id + "]", cyan);

                    object teamPage = NewPage(
                        createPage, playerPage, "Gestion des metiers", white);

                    if (isMe)
                    {
                        AddFunction(createFunction, teamPage, "Redevenir citoyen", white,
                            () => DarkRPNetwork.Request(id, 0, 0));

                        AddFunction(createFunction, teamPage, "Rejoindre les eboueurs", green,
                            () => DarkRPNetwork.Request(id, 1, 1));
                    }

                    if (IsHost())
                    {
                        AddAllTeams(createPage, createFunction, teamPage,
                            id, white, yellow, green, blue, red, cyan);
                    }
                    else if (CanManageOwnTeam())
                    {
                        AddManagedTeam(createPage, createFunction, teamPage,
                            id, white, yellow, green, blue, red, cyan);
                    }

                    pagesJoueurs.Add(id);
                }

                MelonLogger.Msg("[Fusion DarkRP] Liste des joueurs actualisee.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[Fusion DarkRP] Actualisation joueurs : " + ex);
            }
        }

        private static void AddAllTeams(
            MethodInfo cp, MethodInfo cf, object parent, byte target,
            object white, object yellow, object green, object blue,
            object red, object cyan)
        {
            AddTargetTeam(cp, cf, parent, target, 1, green);
            AddTargetTeam(cp, cf, parent, target, 2, yellow);
            AddTargetTeam(cp, cf, parent, target, 3, cyan);
            AddTargetTeam(cp, cf, parent, target, 4, green);
            AddTargetTeam(cp, cf, parent, target, 5, blue);
            AddTargetTeam(cp, cf, parent, target, 6, green);
            AddTargetTeam(cp, cf, parent, target, 7, red);
            AddTargetTeam(cp, cf, parent, target, 8, blue);
            AddTargetTeam(cp, cf, parent, target, 9, yellow);
            AddFunction(cf, parent, "Renvoyer citoyen", white,
                () => DarkRPNetwork.Request(target, 0, 0));
        }

        private static void AddManagedTeam(
            MethodInfo cp, MethodInfo cf, object parent, byte target,
            object white, object yellow, object green, object blue,
            object red, object cyan)
        {
            string team = MyTeam();
            byte id = TeamId(team);
            if (id == 0) return;

            object color = green;
            if (team == "Banque" || team == "Mairie") color = yellow;
            if (team == "Police") color = blue;
            if (team == "Marseillais") color = red;
            if (team == "Parisiens") color = blue;
            if (team == "GensDuVoyage") color = yellow;

            AddTargetTeam(cp, cf, parent, target, id, color);
            AddFunction(cf, parent, "Renvoyer citoyen", white,
                () => DarkRPNetwork.Request(target, 0, 0));
        }

        private static void AddTargetTeam(
            MethodInfo cp, MethodInfo cf, object parent,
            byte target, byte team, object color)
        {
            object page = NewPage(cp, parent, TeamDisplay(team), color);
            string[] ranks = Ranks(team);

            for (int i = 0; i < ranks.Length; i++)
            {
                byte grade = GradeId(ranks[i]);
                string label = DisplayGrade(ranks[i]);

                AddFunction(cf, page, label, color,
                    () => DarkRPNetwork.Request(target, team, grade));
            }
        }

        private static bool CanManageOwnTeam()
        {
            string team = MyTeam();
            string grade = MyGrade();
            if (string.IsNullOrEmpty(team) || string.IsNullOrEmpty(grade)) return false;
            return Rank(team, grade) > 0;
        }

        private static string MyTeam()
        {
            object local = ObtenirJoueurLocal();
            object metadata = local?.GetType().GetProperty("Metadata")?.GetValue(local);
            object data = metadata?.GetType().GetProperty("Metadata")?.GetValue(metadata);
            return ReadMetadata(data, TEAM_KEY);
        }

        private static string MyGrade()
        {
            object local = ObtenirJoueurLocal();
            object metadata = local?.GetType().GetProperty("Metadata")?.GetValue(local);
            object data = metadata?.GetType().GetProperty("Metadata")?.GetValue(metadata);
            return ReadMetadata(data, GRADE_KEY);
        }

        private static void ShowStatus()
        {
            MelonLogger.Msg("[Fusion DarkRP] " + TeamDisplay(TeamId(MyTeam())) +
                " - " + DisplayGrade(MyGrade()));
        }

        public override void OnGUI()
        {
            try
            {
                string team = MyTeam();
                string grade = MyGrade();

                if (string.IsNullOrEmpty(team)) team = "Citoyen";
                if (string.IsNullOrEmpty(grade)) grade = "Citoyen";

                GUI.Box(new Rect(12, Screen.height - 72, 310, 56), "");
                GUI.Label(new Rect(22, Screen.height - 64, 290, 22),
                    "METIER : " + TeamDisplay(TeamId(team)));
                GUI.Label(new Rect(22, Screen.height - 40, 290, 22),
                    "GRADE : " + DisplayGrade(grade));
            }
            catch { }
        }

        private static void AddInfo(MethodInfo method, object page, string label, object color)
        {
            AddFunction(method, page, label, color,
                () => MelonLogger.Msg("[Fusion DarkRP] " + label));
        }

        private static void AddFunction(
            MethodInfo method, object page, string label, object color, Action action)
        {
            if (page != null)
                method.Invoke(page, new object[] { label, color, action });
        }

        private static object NewPage(
            MethodInfo method, object parent, string name, object color)
        {
            return method.Invoke(parent, new object[] { name, color, 0, true });
        }

        private static object ColorValue(Type type, string name)
        {
            return type.GetProperty(name,
                BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
        }

        private static string[] Ranks(byte team)
        {
            switch (team)
            {
                case 1: return new[] { "Eboueur", "ResponsableCollecte", "ChefEboueurs" };
                case 2: return new[] { "Guichetier", "ConseillerBancaire", "DirecteurAdjointBanque", "DirecteurBanque" };
                case 3: return new[] { "ConseillerMunicipal", "SecretaireMairie", "AdjointMaire", "Maire" };
                case 4: return new[] { "Vendeur", "ResponsableRayon", "GerantAdjoint", "GerantEpicerie" };
                case 5: return new[] { "Policier", "Sergent", "LieutenantPolice", "CommissaireAdjoint", "ChefPolice" };
                case 6: return new[] { "Infirmier", "Chirurgien", "MedecinAdjoint", "MedecinChef" };
                case 7:
                case 8:
                case 9: return new[] { "Membre", "Associe", "LieutenantGang", "BrasDroit", "Patron" };
                default: return new[] { "Citoyen" };
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
                "Membre", "Associe", "LieutenantGang", "BrasDroit", "Patron"
            };

            for (byte i = 0; i < names.Length; i++)
                if (names[i] == grade) return i;
            return 0;
        }

        private static byte TeamId(string team)
        {
            string[] names = {
                "Citoyen", "Eboueurs", "Banque", "Mairie", "Epicerie",
                "Police", "Medecins", "Marseillais", "Parisiens", "GensDuVoyage"
            };

            for (byte i = 0; i < names.Length; i++)
                if (names[i] == team) return i;
            return 0;
        }

        private static int Rank(string team, string grade)
        {
            byte id = TeamId(team);
            string[] ranks = Ranks(id);
            for (int i = 0; i < ranks.Length; i++)
                if (ranks[i] == grade) return i;
            return -1;
        }

        private static string TeamDisplay(byte id)
        {
            string[] names = {
                "Citoyen", "Eboueurs", "Banque", "Mairie", "Epicerie",
                "Police", "Medecins", "Les Marseillais", "Les Parisiens",
                "Les Gens du Voyage"
            };
            return id < names.Length ? names[id] : "Citoyen";
        }

        private static string DisplayGrade(string grade)
        {
            if (string.IsNullOrEmpty(grade)) return "Citoyen";
            switch (grade)
            {
                case "ResponsableCollecte": return "Responsable collecte";
                case "ChefEboueurs": return "Chef des eboueurs";
                case "ConseillerBancaire": return "Conseiller bancaire";
                case "DirecteurAdjointBanque": return "Directeur adjoint";
                case "DirecteurBanque": return "Directeur banque";
                case "ConseillerMunicipal": return "Conseiller municipal";
                case "SecretaireMairie": return "Secretaire mairie";
                case "AdjointMaire": return "Adjoint au maire";
                case "GerantAdjoint": return "Gerant adjoint";
                case "GerantEpicerie": return "Gerant epicerie";
                case "LieutenantPolice": return "Lieutenant";
                case "CommissaireAdjoint": return "Commissaire adjoint";
                case "ChefPolice": return "Chef police";
                case "MedecinAdjoint": return "Medecin adjoint";
                case "MedecinChef": return "Medecin-chef";
                case "LieutenantGang": return "Lieutenant";
                case "BrasDroit": return "Bras droit";
                default: return grade;
            }
        }

        private static string PlayerName(object player)
        {
            try
            {
                object metadata = player.GetType().GetProperty("Metadata").GetValue(player);
                object username = metadata.GetType().GetProperty("Username").GetValue(metadata);
                return username.GetType().GetMethod("GetValue").Invoke(username, null) as string
                    ?? "Joueur";
            }
            catch { return "Joueur"; }
        }

        private static string ReadMetadata(object metadata, string key)
        {
            if (metadata == null) return null;
            MethodInfo method = metadata.GetType().GetMethod(
                "GetMetadata", new[] { typeof(string) });
            return method?.Invoke(metadata, new object[] { key }) as string;
        }

        private static bool ReadBool(object obj, string name)
        {
            try
            {
                object value = obj.GetType().GetProperty(name)?.GetValue(obj);
                return value is bool && (bool)value;
            }
            catch { return false; }
        }

        private static bool IsHost()
        {
            try
            {
                Assembly fusion = TrouverAssembly("LabFusion");
                Type type = fusion?.GetType("LabFusion.Network.NetworkInfo");
                return (bool)type?.GetProperty("IsHost",
                    BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            }
            catch { return false; }
        }

        private static Assembly TrouverAssembly(string name)
        {
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
                if (a.GetName().Name == name) return a;
            return null;
        }
    }
}
