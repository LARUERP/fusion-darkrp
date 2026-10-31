
using System;
using System.Collections;
using System.Reflection;
using MelonLoader;

[assembly: MelonInfo(typeof(FusionDarkRP.MainMod), "Fusion DarkRP", "1.4.0", "LARUERP")]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]

namespace FusionDarkRP
{
    public class MainMod : MelonMod
    {
        private const string TEAM_KEY = "FusionDarkRP.Team";
        private const string GRADE_KEY = "FusionDarkRP.Grade";

        public enum Equipe
        {
            Citoyen,
            Eboueurs,
            Banque,
            Mairie,
            Epicerie,
            Police,
            Medecins,
            Marseillais,
            Parisiens,
            GensDuVoyage
        }

        public enum Grade
        {
            Citoyen,

            Eboueur,
            ResponsableCollecte,
            ChefEboueurs,

            Guichetier,
            ConseillerBancaire,
            DirecteurAdjointBanque,
            DirecteurBanque,

            ConseillerMunicipal,
            SecretaireMairie,
            AdjointMaire,
            Maire,

            Vendeur,
            ResponsableRayon,
            GerantAdjoint,
            GerantEpicerie,

            Policier,
            Sergent,
            LieutenantPolice,
            CommissaireAdjoint,
            ChefPolice,

            Infirmier,
            Chirurgien,
            MedecinAdjoint,
            MedecinChef,

            Membre,
            Associe,
            LieutenantGang,
            BrasDroit,
            Patron
        }

        private static bool _menuCree;
        private static DateTime _prochaineTentative = DateTime.MinValue;

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("[Fusion DarkRP] Version 1.4.0 chargee.");
            MelonLogger.Msg("[Fusion DarkRP] Systeme de postes initialise.");
        }

        public override void OnUpdate()
        {
            if (_menuCree || DateTime.UtcNow < _prochaineTentative)
                return;

            _prochaineTentative = DateTime.UtcNow.AddSeconds(3);
            CreerMenu();
        }

        // --------------------------------------------------
        // INITIALISATION DU BONEMENU
        // --------------------------------------------------

        private static void CreerMenu()
        {
            try
            {
                Assembly boneLib = TrouverAssembly("BoneLib");

                if (boneLib == null)
                    return;

                Type pageType = boneLib.GetType("BoneLib.BoneMenu.Page");
                Type colorType = Type.GetType(
                    "UnityEngine.Color, UnityEngine.CoreModule"
                );

                if (pageType == null || colorType == null)
                    return;

                PropertyInfo rootProperty = pageType.GetProperty(
                    "Root",
                    BindingFlags.Public | BindingFlags.Static
                );

                object root = rootProperty?.GetValue(null);

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
                    new Type[] { typeof(string), colorType, typeof(Action) }
                );

                if (createPage == null || createFunction == null)
                    return;

                object blanc = Couleur(colorType, "white");
                object jaune = Couleur(colorType, "yellow");
                object vert = Couleur(colorType, "green");
                object bleu = Couleur(colorType, "blue");
                object rouge = Couleur(colorType, "red");
                object cyan = Couleur(colorType, "cyan");
                object gris = Couleur(colorType, "gray");

                object principal = Page(
                    createPage, root, "Fusion DarkRP", jaune
                );

                object publics = Page(
                    createPage, principal, "Jobs publics", blanc
                );

                object factions = Page(
                    createPage, principal, "Factions", gris
                );

                object grades = Page(
                    createPage, principal, "Hierarchies", blanc
                );

                object joueurs = Page(
                    createPage, principal, "Joueurs connectes", cyan
                );

                object administration = Page(
                    createPage, principal, "Administration hote", rouge
                );

                object personnage = Page(
                    createPage, principal, "Mon personnage", vert
                );

                // JOBS PUBLICS

                Ajouter(createFunction, publics, "Citoyen",
                    Equipe.Citoyen, Grade.Citoyen, blanc);

                Ajouter(createFunction, publics, "Eboueur",
                    Equipe.Eboueurs, Grade.Eboueur, vert);

                Ajouter(createFunction, publics, "Banquier",
                    Equipe.Banque, Grade.Guichetier, jaune);

                Ajouter(createFunction, publics, "Poste a la mairie",
                    Equipe.Mairie, Grade.ConseillerMunicipal, cyan);

                Ajouter(createFunction, publics, "Epicier",
                    Equipe.Epicerie, Grade.Vendeur, vert);

                // FACTIONS

                Ajouter(createFunction, factions, "Police",
                    Equipe.Police, Grade.Policier, bleu);

                Ajouter(createFunction, factions, "Medecins",
                    Equipe.Medecins, Grade.Infirmier, vert);

                Ajouter(createFunction, factions, "Les Marseillais",
                    Equipe.Marseillais, Grade.Membre, rouge);

                Ajouter(createFunction, factions, "Les Parisiens",
                    Equipe.Parisiens, Grade.Membre, bleu);

                Ajouter(createFunction, factions, "Les Gens du Voyage",
                    Equipe.GensDuVoyage, Grade.Membre, jaune);

                // HIERARCHIES

                AjouterGrades(createPage, createFunction, grades,
                    "Mairie", Equipe.Mairie, jaune,
                    blanc, cyan, jaune,
                    new Grade[] {
                        Grade.ConseillerMunicipal,
                        Grade.SecretaireMairie,
                        Grade.AdjointMaire,
                        Grade.Maire
                    });

                AjouterGrades(createPage, createFunction, grades,
                    "Police", Equipe.Police, bleu,
                    blanc, cyan, jaune,
                    new Grade[] {
                        Grade.Policier,
                        Grade.Sergent,
                        Grade.LieutenantPolice,
                        Grade.CommissaireAdjoint,
                        Grade.ChefPolice
                    });

                AjouterGrades(createPage, createFunction, grades,
                    "Medecins", Equipe.Medecins, vert,
                    blanc, cyan, jaune,
                    new Grade[] {
                        Grade.Infirmier,
                        Grade.Chirurgien,
                        Grade.MedecinAdjoint,
                        Grade.MedecinChef
                    });

                AjouterGrades(createPage, createFunction, grades,
                    "Eboueurs", Equipe.Eboueurs, vert,
                    blanc, cyan, jaune,
                    new Grade[] {
                        Grade.Eboueur,
                        Grade.ResponsableCollecte,
                        Grade.ChefEboueurs
                    });

                AjouterGrades(createPage, createFunction, grades,
                    "Banque", Equipe.Banque, jaune,
                    blanc, cyan, jaune,
                    new Grade[] {
                        Grade.Guichetier,
                        Grade.ConseillerBancaire,
                        Grade.DirecteurAdjointBanque,
                        Grade.DirecteurBanque
                    });

                AjouterGrades(createPage, createFunction, grades,
                    "Epicerie", Equipe.Epicerie, vert,
                    blanc, cyan, jaune,
                    new Grade[] {
                        Grade.Vendeur,
                        Grade.ResponsableRayon,
                        Grade.GerantAdjoint,
                        Grade.GerantEpicerie
                    });

                AjouterGradesGang(createPage, createFunction, grades,
                    "Marseillais", Equipe.Marseillais, rouge,
                    blanc, cyan, jaune);

                AjouterGradesGang(createPage, createFunction, grades,
                    "Parisiens", Equipe.Parisiens, bleu,
                    blanc, cyan, jaune);

                AjouterGradesGang(createPage, createFunction, grades,
                    "Gens du Voyage", Equipe.GensDuVoyage, jaune,
                    blanc, cyan, jaune);

                // ADMINISTRATION : NOMINATIONS HOTE

                AjouterNomination(createFunction, administration,
                    "Nommer Maire", Equipe.Mairie, Grade.Maire, jaune);

                AjouterNomination(createFunction, administration,
                    "Nommer Chef de police", Equipe.Police, Grade.ChefPolice, bleu);

                AjouterNomination(createFunction, administration,
                    "Nommer Medecin-chef", Equipe.Medecins, Grade.MedecinChef, vert);

                AjouterNomination(createFunction, administration,
                    "Nommer Chef des eboueurs", Equipe.Eboueurs, Grade.ChefEboueurs, vert);

                AjouterNomination(createFunction, administration,
                    "Nommer Directeur de banque", Equipe.Banque, Grade.DirecteurBanque, jaune);

                AjouterNomination(createFunction, administration,
                    "Nommer Gerant epicerie", Equipe.Epicerie, Grade.GerantEpicerie, vert);

                AjouterNomination(createFunction, administration,
                    "Nommer Patron Marseillais", Equipe.Marseillais, Grade.Patron, rouge);

                AjouterNomination(createFunction, administration,
                    "Nommer Patron Parisien", Equipe.Parisiens, Grade.Patron, bleu);

                AjouterNomination(createFunction, administration,
                    "Nommer Patron Gens du Voyage", Equipe.GensDuVoyage, Grade.Patron, jaune);

                // JOUEURS CONNECTES

                Action actualiser = () =>
                    ActualiserJoueurs(
                        createPage, createFunction, joueurs,
                        blanc, jaune, vert, bleu, rouge, cyan
                    );

                Fonction(createFunction, joueurs,
                    "Actualiser la liste", cyan, actualiser);

                // PERSONNAGE

                Fonction(createFunction, personnage,
                    "Afficher mon equipe", blanc,
                    () => AfficherMonStatut());

                Fonction(createFunction, personnage,
                    "Afficher mon grade", blanc,
                    () => AfficherMonStatut());

                _menuCree = true;

                MelonLogger.Msg("[Fusion DarkRP] Menu cree.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[Fusion DarkRP] Erreur menu : " + ex
                );
            }
        }

        // --------------------------------------------------
        // CREATION DES HIERARCHIES
        // --------------------------------------------------

        private static void AjouterGradesGang(
            MethodInfo createPage,
            MethodInfo createFunction,
            object parent,
            string nom,
            Equipe equipe,
            object couleur,
            object blanc,
            object cyan,
            object jaune)
        {
            AjouterGrades(createPage, createFunction, parent,
                nom, equipe, couleur, blanc, cyan, jaune,
                new Grade[] {
                    Grade.Membre,
                    Grade.Associe,
                    Grade.LieutenantGang,
                    Grade.BrasDroit,
                    Grade.Patron
                });
        }

        private static void AjouterGrades(
            MethodInfo createPage,
            MethodInfo createFunction,
            object parent,
            string nom,
            Equipe equipe,
            object couleur,
            object blanc,
            object cyan,
            object jaune,
            Grade[] liste)
        {
            object page = Page(createPage, parent, nom, couleur);

            foreach (Grade grade in liste)
            {
                object c = blanc;

                if (grade == Grade.BrasDroit ||
                    grade == Grade.AdjointMaire ||
                    grade == Grade.LieutenantGang ||
                    grade == Grade.ResponsableCollecte ||
                    grade == Grade.Sergent ||
                    grade == Grade.LieutenantPolice ||
                    grade == Grade.Chirurgien ||
                    grade == Grade.ConseillerBancaire ||
                    grade == Grade.ResponsableRayon)
                    c = cyan;

                if (grade == Grade.Patron ||
                    grade == Grade.Maire ||
                    grade == Grade.ChefPolice ||
                    grade == Grade.MedecinChef ||
                    grade == Grade.ChefEboueurs ||
                    grade == Grade.DirecteurBanque ||
                    grade == Grade.GerantEpicerie)
                    c = jaune;

                Ajouter(createFunction, page,
                    NomGrade(grade), equipe, grade, c);
            }
        }

        // --------------------------------------------------
        // LISTE DES JOUEURS LABFUSION
        // --------------------------------------------------

        private static void ActualiserJoueurs(
            MethodInfo createPage,
            MethodInfo createFunction,
            object parent,
            object blanc,
            object jaune,
            object vert,
            object bleu,
            object rouge,
            object cyan)
        {
            try
            {
                Assembly fusion = TrouverAssembly("LabFusion");

                if (fusion == null)
                {
                    MelonLogger.Warning("[Fusion DarkRP] LabFusion introuvable.");
                    return;
                }

                Type manager = fusion.GetType(
                    "LabFusion.Player.PlayerIDManager"
                );

                PropertyInfo idsProperty = manager?.GetProperty(
                    "PlayerIDs",
                    BindingFlags.Public | BindingFlags.Static
                );

                if (idsProperty == null)
                {
                    MelonLogger.Warning("[Fusion DarkRP] PlayerIDs introuvable.");
                    return;
                }

                IEnumerable ids = idsProperty.GetValue(null) as IEnumerable;

                if (ids == null)
                    return;

                int compteur = 0;

                foreach (object player in ids)
                {
                    if (player == null)
                        continue;

                    PropertyInfo validProperty = player.GetType().GetProperty("IsValid");

                    if (validProperty != null &&
                        !(bool)validProperty.GetValue(player))
                        continue;

                    PropertyInfo smallIdProperty = player.GetType().GetProperty("SmallID");
                    PropertyInfo meProperty = player.GetType().GetProperty("IsMe");
                    PropertyInfo hostProperty = player.GetType().GetProperty("IsHost");

                    object id = smallIdProperty?.GetValue(player);
                    bool estMoi = meProperty != null &&
                        (bool)meProperty.GetValue(player);

                    bool estHote = hostProperty != null &&
                        (bool)hostProperty.GetValue(player);

                    string nom = LireNomJoueur(player);

                    string etiquette = nom + " [ID " + id + "]";

                    if (estMoi)
                        etiquette += " (Moi)";

                    if (estHote)
                        etiquette += " (Hote)";

                    object page = Page(
                        createPage, parent, etiquette, cyan
                    );

                    // Un joueur ne peut etre affecte par ce menu
                    // que par l'hote. Les joueurs peuvent toujours
                    // choisir leur propre poste public.

                    if (EstHote())
                    {
                        object postes = Page(
                            createPage, page, "Affecter un poste", blanc
                        );

                        AjouterCible(createFunction, postes, player,
                            "Citoyen", Equipe.Citoyen, Grade.Citoyen, blanc);

                        AjouterCible(createFunction, postes, player,
                            "Eboueur", Equipe.Eboueurs, Grade.Eboueur, vert);

                        AjouterCible(createFunction, postes, player,
                            "Banquier", Equipe.Banque, Grade.Guichetier, jaune);

                        AjouterCible(createFunction, postes, player,
                            "Mairie", Equipe.Mairie, Grade.ConseillerMunicipal, cyan);

                        AjouterCible(createFunction, postes, player,
                            "Epicier", Equipe.Epicerie, Grade.Vendeur, vert);

                        AjouterCible(createFunction, postes, player,
                            "Police", Equipe.Police, Grade.Policier, bleu);

                        AjouterCible(createFunction, postes, player,
                            "Medecin", Equipe.Medecins, Grade.Infirmier, vert);

                        AjouterCible(createFunction, postes, player,
                            "Marseillais", Equipe.Marseillais, Grade.Membre, rouge);

                        AjouterCible(createFunction, postes, player,
                            "Parisiens", Equipe.Parisiens, Grade.Membre, bleu);

                        AjouterCible(createFunction, postes, player,
                            "Gens du Voyage", Equipe.GensDuVoyage, Grade.Membre, jaune);
                    }

                    compteur++;
                }

                MelonLogger.Msg(
                    "[Fusion DarkRP] Joueurs trouves : " + compteur
                );

                if (compteur == 0)
                    MelonLogger.Msg("[Fusion DarkRP] Aucun joueur connecte.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[Fusion DarkRP] Erreur liste joueurs : " + ex
                );
            }
        }

        private static void AjouterCible(
            MethodInfo method,
            object page,
            object joueur,
            string label,
            Equipe equipe,
            Grade grade,
            object couleur)
        {
            Fonction(method, page, label, couleur, () =>
            {
                if (!EstHote())
                {
                    MelonLogger.Warning(
                        "[Fusion DarkRP] Seul l'hote peut affecter un autre joueur."
                    );
                    return;
                }

                if (EcrireMetadataJoueur(joueur, TEAM_KEY, equipe.ToString()) &&
                    EcrireMetadataJoueur(joueur, GRADE_KEY, grade.ToString()))
                {
                    MelonLogger.Msg(
                        "[Fusion DarkRP] Affectation envoyee : " +
                        label + " / " + NomGrade(grade)
                    );
                }
            });
        }

        // --------------------------------------------------
        // METADONNEES ET SYNCHRONISATION LABFUSION
        // --------------------------------------------------

        private static bool EcrireMetadataJoueur(
            object joueur,
            string cle,
            string valeur)
        {
            try
            {
                PropertyInfo metadataProperty =
                    joueur.GetType().GetProperty("Metadata");

                object playerMetadata = metadataProperty?.GetValue(joueur);

                object networkMetadata = playerMetadata?
                    .GetType().GetProperty("Metadata")?
                    .GetValue(playerMetadata);

                return AppelerTrySet(networkMetadata, cle, valeur);
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[Fusion DarkRP] Ecriture metadata impossible : " + ex.Message
                );
                return false;
            }
        }

        private static bool EcrireMetadataLocale(
            string cle,
            string valeur)
        {
            try
            {
                Assembly fusion = TrouverAssembly("LabFusion");

                Type localPlayer = fusion?.GetType(
                    "LabFusion.Player.LocalPlayer"
                );

                object metadata = localPlayer?
                    .GetProperty("Metadata",
                        BindingFlags.Public | BindingFlags.Static)?
                    .GetValue(null);

                object networkMetadata = metadata?
                    .GetType().GetProperty("Metadata")?
                    .GetValue(metadata);

                return AppelerTrySet(networkMetadata, cle, valeur);
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[Fusion DarkRP] Metadata locale impossible : " + ex.Message
                );
                return false;
            }
        }

        private static bool AppelerTrySet(
            object metadata,
            string cle,
            string valeur)
        {
            if (metadata == null)
                return false;

            MethodInfo method = metadata.GetType().GetMethod(
                "TrySetMetadata",
                new Type[] { typeof(string), typeof(string) }
            );

            if (method == null)
            {
                MelonLogger.Warning(
                    "[Fusion DarkRP] TrySetMetadata introuvable."
                );
                return false;
            }

            object result = method.Invoke(
                metadata, new object[] { cle, valeur }
            );

            return result is bool && (bool)result;
        }

        // --------------------------------------------------
        // CHANGEMENT DE POSTE
        // --------------------------------------------------

        private static void Ajouter(
            MethodInfo method,
            object page,
            string label,
            Equipe equipe,
            Grade grade,
            object couleur)
        {
            Fonction(method, page, label, couleur, () =>
            {
                if (grade == Grade.Maire ||
                    grade == Grade.ChefPolice ||
                    grade == Grade.MedecinChef ||
                    grade == Grade.ChefEboueurs ||
                    grade == Grade.DirecteurBanque ||
                    grade == Grade.GerantEpicerie ||
                    grade == Grade.Patron)
                {
                    MelonLogger.Warning(
                        "[Fusion DarkRP] Ce grade doit etre attribue par l'hote."
                    );
                    return;
                }

                ChangerMonPoste(equipe, grade);
            });
        }

        private static void AjouterNomination(
            MethodInfo method,
            object page,
            string label,
            Equipe equipe,
            Grade grade,
            object couleur)
        {
            Fonction(method, page, label, couleur, () =>
            {
                if (!EstHote())
                {
                    MelonLogger.Warning(
                        "[Fusion DarkRP] Nomination reservee a l'hote."
                    );
                    return;
                }

                ChangerMonPoste(equipe, grade);
            });
        }

        private static void ChangerMonPoste(
            Equipe equipe,
            Grade grade)
        {
            if (!EcrireMetadataLocale(TEAM_KEY, equipe.ToString()))
                return;

            if (!EcrireMetadataLocale(GRADE_KEY, grade.ToString()))
                return;

            MelonLogger.Msg(
                "[Fusion DarkRP] Poste synchronise : " +
                equipe + " / " + grade
            );
        }

        // --------------------------------------------------
        // AFFICHAGE DU STATUT
        // --------------------------------------------------

        private static void AfficherMonStatut()
        {
            try
            {
                Assembly fusion = TrouverAssembly("LabFusion");

                Type localPlayer = fusion?.GetType(
                    "LabFusion.Player.LocalPlayer"
                );

                object metadata = localPlayer?
                    .GetProperty("Metadata",
                        BindingFlags.Public | BindingFlags.Static)?
                    .GetValue(null);

                object networkMetadata = metadata?
                    .GetType().GetProperty("Metadata")?
                    .GetValue(metadata);

                string equipe = LireMetadata(networkMetadata, TEAM_KEY);
                string grade = LireMetadata(networkMetadata, GRADE_KEY);

                MelonLogger.Msg(
                    "[Fusion DarkRP] Mon equipe : " +
                    (string.IsNullOrEmpty(equipe) ? "Citoyen" : equipe)
                );

                MelonLogger.Msg(
                    "[Fusion DarkRP] Mon grade : " +
                    (string.IsNullOrEmpty(grade) ? "Citoyen" : grade)
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[Fusion DarkRP] Lecture statut impossible : " + ex.Message
                );
            }
        }

        private static string LireMetadata(
            object metadata,
            string cle)
        {
            if (metadata == null)
                return null;

            MethodInfo method = metadata.GetType().GetMethod(
                "GetMetadata",
                new Type[] { typeof(string) }
            );

            return method?.Invoke(metadata, new object[] { cle }) as string;
        }

        private static string LireNomJoueur(object joueur)
        {
            try
            {
                object metadata = joueur.GetType()
                    .GetProperty("Metadata")?.GetValue(joueur);

                object nickname = metadata?.GetType()
                    .GetProperty("Nickname")?.GetValue(metadata);

                string nom = nickname?.GetType()
                    .GetMethod("GetValueOrEmpty")?
                    .Invoke(nickname, null) as string;

                if (!string.IsNullOrEmpty(nom))
                    return nom;

                object username = metadata?.GetType()
                    .GetProperty("Username")?.GetValue(metadata);

                nom = username?.GetType()
                    .GetMethod("GetValueOrEmpty")?
                    .Invoke(username, null) as string;

                return string.IsNullOrEmpty(nom) ? "Joueur" : nom;
            }
            catch
            {
                return "Joueur";
            }
        }

        // --------------------------------------------------
        // OUTILS
        // --------------------------------------------------

        private static Assembly TrouverAssembly(string nom)
        {
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name == nom)
                    return asm;
            }

            return null;
        }

        private static object Couleur(Type type, string nom)
        {
            return type.GetProperty(
                nom,
                BindingFlags.Public | BindingFlags.Static
            )?.GetValue(null);
        }

        private static object Page(
            MethodInfo method,
            object parent,
            string nom,
            object couleur)
        {
            return method.Invoke(
                parent,
                new object[] { nom, couleur, 0, true }
            );
        }

        private static void Fonction(
            MethodInfo method,
            object page,
            string label,
            object couleur,
            Action action)
        {
            if (page == null)
                return;

            method.Invoke(
                page,
                new object[] { label, couleur, action }
            );
        }

        private static bool EstHote()
        {
            try
            {
                Assembly fusion = TrouverAssembly("LabFusion");

                Type network = fusion?.GetType(
                    "LabFusion.Network.NetworkInfo"
                );

                PropertyInfo property = network?.GetProperty(
                    "IsHost",
                    BindingFlags.Public | BindingFlags.Static
                );

                return property != null &&
                    (bool)property.GetValue(null);
            }
            catch
            {
                return false;
            }
        }

        private static string NomGrade(Grade grade)
        {
            return grade.ToString();
        }
    }
}
