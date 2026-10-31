
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MelonLoader;

[assembly: MelonInfo(typeof(FusionDarkRP.MainMod), "Fusion DarkRP", "1.5.0", "LARUERP")]
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

        private static bool _menuCree;
        private static DateTime _prochaineTentative = DateTime.MinValue;
        private static readonly HashSet<byte> PagesJoueurs = new HashSet<byte>();

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("[Fusion DarkRP] Version 1.5.0 initialisee.");
        }

        public override void OnUpdate()
        {
            if (_menuCree || DateTime.UtcNow < _prochaineTentative)
                return;

            _prochaineTentative = DateTime.UtcNow.AddSeconds(3);
            CreerMenu();
        }

        private static void CreerMenu()
        {
            try
            {
                Assembly boneLib = TrouverAssembly("BoneLib");
                if (boneLib == null) return;

                Type pageType = boneLib.GetType("BoneLib.BoneMenu.Page");
                Type colorType = Type.GetType("UnityEngine.Color, UnityEngine.CoreModule");

                if (pageType == null || colorType == null) return;

                object root = pageType.GetProperty(
                    "Root", BindingFlags.Public | BindingFlags.Static
                )?.GetValue(null);

                if (root == null) return;

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

                if (createPage == null || createFunction == null) return;

                object blanc = Couleur(colorType, "white");
                object jaune = Couleur(colorType, "yellow");
                object vert = Couleur(colorType, "green");
                object bleu = Couleur(colorType, "blue");
                object rouge = Couleur(colorType, "red");
                object cyan = Couleur(colorType, "cyan");
                object gris = Couleur(colorType, "gray");

                object principal = Page(createPage, root, "Fusion DarkRP", jaune);
                object publics = Page(createPage, principal, "Jobs publics", blanc);
                object factions = Page(createPage, principal, "Factions", gris);
                object grades = Page(createPage, principal, "Hierarchies", blanc);
                object joueurs = Page(createPage, principal, "Joueurs connectes", cyan);
                object administration = Page(createPage, principal, "Administration hote", rouge);
                object personnage = Page(createPage, principal, "Mon personnage", vert);

                // METIERS PUBLICS : accessibles a tous

                AjouterPublic(createFunction, publics, "Citoyen",
                    Equipe.Citoyen, Grade.Citoyen, blanc);

                AjouterPublic(createFunction, publics, "Eboueur",
                    Equipe.Eboueurs, Grade.Eboueur, vert);

                AjouterPublic(createFunction, publics, "Banquier",
                    Equipe.Banque, Grade.Guichetier, jaune);

                AjouterPublic(createFunction, publics, "Poste a la mairie",
                    Equipe.Mairie, Grade.ConseillerMunicipal, cyan);

                AjouterPublic(createFunction, publics, "Epicier",
                    Equipe.Epicerie, Grade.Vendeur, vert);

                // FACTIONS : recrutement par l'administration

                AjouterInfo(createFunction, factions, "Police - recrutement par l'hote", bleu);
                AjouterInfo(createFunction, factions, "Medecins - recrutement par l'hote", vert);
                AjouterInfo(createFunction, factions, "Les Marseillais - recrutement par l'hote", rouge);
                AjouterInfo(createFunction, factions, "Les Parisiens - recrutement par l'hote", bleu);
                AjouterInfo(createFunction, factions, "Les Gens du Voyage - recrutement par l'hote", jaune);

                // HIERARCHIES

                AjouterGrades(createPage, createFunction, grades,
                    "Mairie", Equipe.Mairie, jaune, blanc,
                    new Grade[] {
                        Grade.ConseillerMunicipal, Grade.SecretaireMairie,
                        Grade.AdjointMaire, Grade.Maire
                    });

                AjouterGrades(createPage, createFunction, grades,
                    "Police", Equipe.Police, bleu, blanc,
                    new Grade[] {
                        Grade.Policier, Grade.Sergent, Grade.LieutenantPolice,
                        Grade.CommissaireAdjoint, Grade.ChefPolice
                    });

                AjouterGrades(createPage, createFunction, grades,
                    "Medecins", Equipe.Medecins, vert, blanc,
                    new Grade[] {
                        Grade.Infirmier, Grade.Chirurgien,
                        Grade.MedecinAdjoint, Grade.MedecinChef
                    });

                AjouterGrades(createPage, createFunction, grades,
                    "Eboueurs", Equipe.Eboueurs, vert, blanc,
                    new Grade[] {
                        Grade.Eboueur, Grade.ResponsableCollecte, Grade.ChefEboueurs
                    });

                AjouterGrades(createPage, createFunction, grades,
                    "Banque", Equipe.Banque, jaune, blanc,
                    new Grade[] {
                        Grade.Guichetier, Grade.ConseillerBancaire,
                        Grade.DirecteurAdjointBanque, Grade.DirecteurBanque
                    });

                AjouterGrades(createPage, createFunction, grades,
                    "Epicerie", Equipe.Epicerie, vert, blanc,
                    new Grade[] {
                        Grade.Vendeur, Grade.ResponsableRayon,
                        Grade.GerantAdjoint, Grade.GerantEpicerie
                    });

                AjouterGrades(createPage, createFunction, grades,
                    "Marseillais", Equipe.Marseillais, rouge, blanc,
                    new Grade[] {
                        Grade.Membre, Grade.Associe, Grade.LieutenantGang,
                        Grade.BrasDroit, Grade.Patron
                    });

                AjouterGrades(createPage, createFunction, grades,
                    "Parisiens", Equipe.Parisiens, bleu, blanc,
                    new Grade[] {
                        Grade.Membre, Grade.Associe, Grade.LieutenantGang,
                        Grade.BrasDroit, Grade.Patron
                    });

                AjouterGrades(createPage, createFunction, grades,
                    "Gens du Voyage", Equipe.GensDuVoyage, jaune, blanc,
                    new Grade[] {
                        Grade.Membre, Grade.Associe, Grade.LieutenantGang,
                        Grade.BrasDroit, Grade.Patron
                    });

                // ADMINISTRATION

                AjouterInfo(createFunction, administration,
                    "Selectionner un joueur dans Joueurs connectes", jaune);

                AjouterInfo(createFunction, administration,
                    "Seul l'hote peut attribuer les grades de chef et patron", rouge);

                // JOUEURS CONNECTES

                Fonction(createFunction, joueurs, "Actualiser la liste",
                    cyan, () => ActualiserJoueurs(
                        createPage, createFunction, joueurs,
                        blanc, jaune, vert, bleu, rouge, cyan
                    ));

                // PERSONNAGE

                Fonction(createFunction, personnage, "Afficher mon equipe",
                    blanc, AfficherMonStatut);

                Fonction(createFunction, personnage, "Afficher mon grade",
                    blanc, AfficherMonStatut);

                _menuCree = true;
                MelonLogger.Msg("[Fusion DarkRP] Menu cree.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[Fusion DarkRP] Erreur menu : " + ex);
            }
        }

        private static void AjouterPublic(
            MethodInfo method, object page, string label,
            Equipe equipe, Grade grade, object couleur)
        {
            Fonction(method, page, label, couleur, () =>
            {
                if (!EstPostePublic(equipe))
                {
                    MelonLogger.Warning("[Fusion DarkRP] Poste non public.");
                    return;
                }

                ChangerMonPoste(equipe, grade);
            });
        }

        private static bool EstPostePublic(Equipe equipe)
        {
            return equipe == Equipe.Citoyen ||
                   equipe == Equipe.Eboueurs ||
                   equipe == Equipe.Banque ||
                   equipe == Equipe.Mairie ||
                   equipe == Equipe.Epicerie;
        }

        private static void AjouterInfo(
            MethodInfo method, object page, string label, object couleur)
        {
            Fonction(method, page, label, couleur, () =>
                MelonLogger.Msg("[Fusion DarkRP] " + label));
        }

        private static void AjouterGrades(
            MethodInfo createPage, MethodInfo createFunction,
            object parent, string nom, Equipe equipe,
            object couleur, object blanc, Grade[] liste)
        {
            object page = Page(createPage, parent, nom, couleur);

            foreach (Grade grade in liste)
            {
                Fonction(createFunction, page, NomGrade(grade), blanc, () =>
                {
                    MelonLogger.Msg(
                        "[Fusion DarkRP] Grade " + NomGrade(grade) +
                        " : attribution par un administrateur depuis Joueurs connectes."
                    );
                });
            }
        }

        // LISTE DES JOUEURS

        private static void ActualiserJoueurs(
            MethodInfo createPage, MethodInfo createFunction,
            object parent, object blanc, object jaune, object vert,
            object bleu, object rouge, object cyan)
        {
            try
            {
                Assembly fusion = TrouverAssembly("LabFusion");
                Type manager = fusion?.GetType("LabFusion.Player.PlayerIDManager");

                PropertyInfo idsProperty = manager?.GetProperty(
                    "PlayerIDs", BindingFlags.Public | BindingFlags.Static
                );

                IEnumerable ids = idsProperty?.GetValue(null) as IEnumerable;

                if (ids == null)
                {
                    MelonLogger.Warning("[Fusion DarkRP] Liste LabFusion indisponible.");
                    return;
                }

                int compteur = 0;

                foreach (object player in ids)
                {
                    if (player == null) continue;

                    Type type = player.GetType();

                    PropertyInfo validProperty = type.GetProperty("IsValid");
                    if (validProperty != null &&
                        !(bool)validProperty.GetValue(player))
                        continue;

                    PropertyInfo idProperty = type.GetProperty("SmallID");
                    if (idProperty == null) continue;

                    byte id = Convert.ToByte(idProperty.GetValue(player));

                    if (PagesJoueurs.Contains(id)) continue;

                    bool estMoi = LireBool(player, "IsMe");
                    bool estHote = LireBool(player, "IsHost");
                    string nom = LireNomJoueur(player);

                    string etiquette = nom + " [ID " + id + "]";
                    if (estMoi) etiquette += " (Moi)";
                    if (estHote) etiquette += " (Hote)";

                    object page = Page(createPage, parent, etiquette, cyan);

                    if (EstHote())
                    {
                        object equipePage = Page(
                            createPage, page, "Choisir une equipe", blanc
                        );

                        AjouterEquipeCible(createPage, createFunction,
                            equipePage, player, Equipe.Citoyen, blanc);

                        AjouterEquipeCible(createPage, createFunction,
                            equipePage, player, Equipe.Eboueurs, vert);

                        AjouterEquipeCible(createPage, createFunction,
                            equipePage, player, Equipe.Banque, jaune);

                        AjouterEquipeCible(createPage, createFunction,
                            equipePage, player, Equipe.Mairie, cyan);

                        AjouterEquipeCible(createPage, createFunction,
                            equipePage, player, Equipe.Epicerie, vert);

                        AjouterEquipeCible(createPage, createFunction,
                            equipePage, player, Equipe.Police, bleu);

                        AjouterEquipeCible(createPage, createFunction,
                            equipePage, player, Equipe.Medecins, vert);

                        AjouterEquipeCible(createPage, createFunction,
                            equipePage, player, Equipe.Marseillais, rouge);

                        AjouterEquipeCible(createPage, createFunction,
                            equipePage, player, Equipe.Parisiens, bleu);

                        AjouterEquipeCible(createPage, createFunction,
                            equipePage, player, Equipe.GensDuVoyage, jaune);
                    }

                    PagesJoueurs.Add(id);
                    compteur++;
                }

                MelonLogger.Msg(
                    "[Fusion DarkRP] Nouveaux joueurs affiches : " + compteur
                );

                if (compteur == 0)
                    MelonLogger.Msg("[Fusion DarkRP] Aucun nouveau joueur a afficher.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[Fusion DarkRP] Erreur joueurs : " + ex);
            }
        }

        private static void AjouterEquipeCible(
            MethodInfo createPage, MethodInfo createFunction,
            object parent, object joueur, Equipe equipe, object couleur)
        {
            object page = Page(
                createPage, parent, NomEquipe(equipe), couleur
            );

            foreach (Grade grade in GradesDequipe(equipe))
            {
                Fonction(createFunction, page, NomGrade(grade), couleur, () =>
                {
                    if (!EstHote())
                    {
                        MelonLogger.Warning("[Fusion DarkRP] Action reservee a l'hote.");
                        return;
                    }

                    if (!EstGradeAutorisePourEquipe(equipe, grade))
                    {
                        MelonLogger.Warning("[Fusion DarkRP] Grade invalide pour cette equipe.");
                        return;
                    }

                    if (EcrireMetadataJoueur(joueur, TEAM_KEY, equipe.ToString()) &&
                        EcrireMetadataJoueur(joueur, GRADE_KEY, grade.ToString()))
                    {
                        MelonLogger.Msg(
                            "[Fusion DarkRP] Joueur affecte : " +
                            NomEquipe(equipe) + " / " + NomGrade(grade)
                        );
                    }
                    else
                    {
                        MelonLogger.Warning(
                            "[Fusion DarkRP] Echec de l'attribution. Verifie LabFusion."
                        );
                    }
                });
            }
        }

        private static Grade[] GradesDequipe(Equipe equipe)
        {
            switch (equipe)
            {
                case Equipe.Citoyen:
                    return new[] { Grade.Citoyen };
                case Equipe.Eboueurs:
                    return new[] { Grade.Eboueur, Grade.ResponsableCollecte, Grade.ChefEboueurs };
                case Equipe.Banque:
                    return new[] { Grade.Guichetier, Grade.ConseillerBancaire, Grade.DirecteurAdjointBanque, Grade.DirecteurBanque };
                case Equipe.Mairie:
                    return new[] { Grade.ConseillerMunicipal, Grade.SecretaireMairie, Grade.AdjointMaire, Grade.Maire };
                case Equipe.Epicerie:
                    return new[] { Grade.Vendeur, Grade.ResponsableRayon, Grade.GerantAdjoint, Grade.GerantEpicerie };
                case Equipe.Police:
                    return new[] { Grade.Policier, Grade.Sergent, Grade.LieutenantPolice, Grade.CommissaireAdjoint, Grade.ChefPolice };
                case Equipe.Medecins:
                    return new[] { Grade.Infirmier, Grade.Chirurgien, Grade.MedecinAdjoint, Grade.MedecinChef };
                case Equipe.Marseillais:
                case Equipe.Parisiens:
                case Equipe.GensDuVoyage:
                    return new[] { Grade.Membre, Grade.Associe, Grade.LieutenantGang, Grade.BrasDroit, Grade.Patron };
                default:
                    return new[] { Grade.Citoyen };
            }
        }

        private static bool EstGradeAutorisePourEquipe(Equipe equipe, Grade grade)
        {
            foreach (Grade valide in GradesDequipe(equipe))
                if (valide == grade) return true;

            return false;
        }

        // METADONNEES LABFUSION

        private static bool EcrireMetadataJoueur(
            object joueur, string cle, string valeur)
        {
            try
            {
                object playerMetadata = joueur.GetType()
                    .GetProperty("Metadata")?.GetValue(joueur);

                object networkMetadata = playerMetadata?.GetType()
                    .GetProperty("Metadata")?.GetValue(playerMetadata);

                return AppelerTrySet(networkMetadata, cle, valeur);
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[Fusion DarkRP] Metadata joueur : " + ex.Message);
                return false;
            }
        }

        private static bool EcrireMetadataLocale(string cle, string valeur)
        {
            try
            {
                Assembly fusion = TrouverAssembly("LabFusion");
                Type localPlayer = fusion?.GetType("LabFusion.Player.LocalPlayer");

                object metadata = localPlayer?.GetProperty(
                    "Metadata", BindingFlags.Public | BindingFlags.Static
                )?.GetValue(null);

                object networkMetadata = metadata?.GetType()
                    .GetProperty("Metadata")?.GetValue(metadata);

                return AppelerTrySet(networkMetadata, cle, valeur);
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[Fusion DarkRP] Metadata locale : " + ex.Message);
                return false;
            }
        }

        private static bool AppelerTrySet(
            object metadata, string cle, string valeur)
        {
            if (metadata == null) return false;

            MethodInfo method = metadata.GetType().GetMethod(
                "TrySetMetadata",
                new[] { typeof(string), typeof(string) }
            );

            if (method == null) return false;

            object resultat = method.Invoke(
                metadata, new object[] { cle, valeur }
            );

            return resultat is bool && (bool)resultat;
        }

        private static void ChangerMonPoste(Equipe equipe, Grade grade)
        {
            if (!EcrireMetadataLocale(TEAM_KEY, equipe.ToString()))
            {
                MelonLogger.Warning("[Fusion DarkRP] Impossible de changer l'equipe.");
                return;
            }

            if (!EcrireMetadataLocale(GRADE_KEY, grade.ToString()))
            {
                MelonLogger.Warning("[Fusion DarkRP] Impossible de changer le grade.");
                return;
            }

            MelonLogger.Msg(
                "[Fusion DarkRP] Poste mis a jour : " +
                NomEquipe(equipe) + " / " + NomGrade(grade)
            );
        }

        // STATUT DU JOUEUR

        private static void AfficherMonStatut()
        {
            try
            {
                Assembly fusion = TrouverAssembly("LabFusion");
                Type localPlayer = fusion?.GetType("LabFusion.Player.LocalPlayer");

                object metadata = localPlayer?.GetProperty(
                    "Metadata", BindingFlags.Public | BindingFlags.Static
                )?.GetValue(null);

                object networkMetadata = metadata?.GetType()
                    .GetProperty("Metadata")?.GetValue(metadata);

                string equipe = LireMetadata(networkMetadata, TEAM_KEY);
                string grade = LireMetadata(networkMetadata, GRADE_KEY);

                MelonLogger.Msg("[Fusion DarkRP] Equipe : " +
                    (string.IsNullOrEmpty(equipe) ? "Citoyen" : equipe));

                MelonLogger.Msg("[Fusion DarkRP] Grade : " +
                    (string.IsNullOrEmpty(grade) ? "Citoyen" : grade));
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[Fusion DarkRP] Lecture statut : " + ex.Message);
            }
        }

        private static string LireMetadata(object metadata, string cle)
        {
            if (metadata == null) return null;

            MethodInfo method = metadata.GetType().GetMethod(
                "GetMetadata", new[] { typeof(string) }
            );

            return method?.Invoke(metadata, new object[] { cle }) as string;
        }

        // OUTILS

        private static Assembly TrouverAssembly(string nom)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                if (assembly.GetName().Name == nom)
                    return assembly;

            return null;
        }

        private static object Couleur(Type type, string nom)
        {
            return type.GetProperty(
                nom, BindingFlags.Public | BindingFlags.Static
            )?.GetValue(null);
        }

        private static object Page(
            MethodInfo method, object parent, string nom, object couleur)
        {
            return method.Invoke(
                parent, new object[] { nom, couleur, 0, true }
            );
        }

        private static void Fonction(
            MethodInfo method, object page, string label,
            object couleur, Action action)
        {
            if (page == null) return;

            method.Invoke(page, new object[] { label, couleur, action });
        }

        private static bool EstHote()
        {
            try
            {
                Assembly fusion = TrouverAssembly("LabFusion");
                Type network = fusion?.GetType("LabFusion.Network.NetworkInfo");

                PropertyInfo property = network?.GetProperty(
                    "IsHost", BindingFlags.Public | BindingFlags.Static
                );

                return property != null && (bool)property.GetValue(null);
            }
            catch
            {
                return false;
            }
        }

        private static bool LireBool(object objet, string nom)
        {
            try
            {
                object valeur = objet.GetType().GetProperty(nom)?.GetValue(objet);
                return valeur is bool && (bool)valeur;
            }
            catch
            {
                return false;
            }
        }

        private static string LireNomJoueur(object joueur)
        {
            try
            {
                object metadata = joueur.GetType()
                    .GetProperty("Metadata")?.GetValue(joueur);

                object username = metadata?.GetType()
                    .GetProperty("Username")?.GetValue(metadata);

                string nom = username?.GetType()
                    .GetMethod("GetValue")?.Invoke(username, null) as string;

                if (!string.IsNullOrEmpty(nom)) return nom;

                return "Joueur";
            }
            catch
            {
                return "Joueur";
            }
        }

        private static string NomEquipe(Equipe equipe)
        {
            switch (equipe)
            {
                case Equipe.Citoyen: return "Citoyen";
                case Equipe.Eboueurs: return "Eboueurs";
                case Equipe.Banque: return "Banque";
                case Equipe.Mairie: return "Mairie";
                case Equipe.Epicerie: return "Epicerie";
                case Equipe.Police: return "Police";
                case Equipe.Medecins: return "Medecins";
                case Equipe.Marseillais: return "Les Marseillais";
                case Equipe.Parisiens: return "Les Parisiens";
                case Equipe.GensDuVoyage: return "Les Gens du Voyage";
                default: return equipe.ToString();
            }
        }

        private static string NomGrade(Grade grade)
        {
            switch (grade)
            {
                case Grade.ResponsableCollecte: return "Responsable collecte";
                case Grade.ChefEboueurs: return "Chef des eboueurs";
                case Grade.ConseillerBancaire: return "Conseiller bancaire";
                case Grade.DirecteurAdjointBanque: return "Directeur adjoint";
                case Grade.DirecteurBanque: return "Directeur de banque";
                case Grade.ConseillerMunicipal: return "Conseiller municipal";
                case Grade.SecretaireMairie: return "Secretaire mairie";
                case Grade.AdjointMaire: return "Adjoint au maire";
                case Grade.Maire: return "Maire";
                case Grade.ResponsableRayon: return "Responsable rayon";
                case Grade.GerantAdjoint: return "Gerant adjoint";
                case Grade.GerantEpicerie: return "Gerant epicerie";
                case Grade.LieutenantPolice: return "Lieutenant";
                case Grade.CommissaireAdjoint: return "Commissaire adjoint";
                case Grade.ChefPolice: return "Chef de police";
                case Grade.MedecinAdjoint: return "Medecin adjoint";
                case Grade.MedecinChef: return "Medecin-chef";
                case Grade.LieutenantGang: return "Lieutenant";
                case Grade.BrasDroit: return "Bras droit";
                case Grade.Patron: return "Patron";
                default: return grade.ToString();
            }
        }
    }
}
