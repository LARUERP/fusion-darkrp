
using System;
using System.Reflection;
using MelonLoader;

[assembly: MelonInfo(typeof(FusionDarkRP.MainMod), "Fusion DarkRP", "1.3.0", "LARUERP")]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]

namespace FusionDarkRP
{
    public class MainMod : MelonMod
    {
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

        public static Equipe MonEquipe = Equipe.Citoyen;
        public static Grade MonGrade = Grade.Citoyen;

        private static bool _menuCree;
        private static DateTime _prochaineTentative = DateTime.MinValue;

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("[Fusion DarkRP] Version 1.3.0 chargee.");
            MelonLogger.Msg("[Fusion DarkRP] Equipes et hierarchies initialisees.");
        }

        public override void OnUpdate()
        {
            if (_menuCree || DateTime.UtcNow < _prochaineTentative)
                return;

            _prochaineTentative = DateTime.UtcNow.AddSeconds(3);
            CreerMenuBoneLib();
        }

        private static void CreerMenuBoneLib()
        {
            try
            {
                Assembly boneLib = null;

                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.GetName().Name == "BoneLib")
                    {
                        boneLib = asm;
                        break;
                    }
                }

                if (boneLib == null) return;

                Type pageType = boneLib.GetType("BoneLib.BoneMenu.Page");
                Type colorType = Type.GetType(
                    "UnityEngine.Color, UnityEngine.CoreModule"
                );

                if (pageType == null || colorType == null) return;

                PropertyInfo rootProperty = pageType.GetProperty(
                    "Root",
                    BindingFlags.Public | BindingFlags.Static
                );

                object root = rootProperty?.GetValue(null);
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

                object blanc = colorType.GetProperty("white").GetValue(null);
                object jaune = colorType.GetProperty("yellow").GetValue(null);
                object vert = colorType.GetProperty("green").GetValue(null);
                object bleu = colorType.GetProperty("blue").GetValue(null);
                object rouge = colorType.GetProperty("red").GetValue(null);
                object cyan = colorType.GetProperty("cyan").GetValue(null);
                object gris = colorType.GetProperty("gray").GetValue(null);

                object principal = Page(createPage, root, "Fusion DarkRP", jaune);

                object publics = Page(createPage, principal, "Jobs publics", blanc);
                object factions = Page(createPage, principal, "Factions", gris);
                object grades = Page(createPage, principal, "Grades et postes", blanc);
                object admin = Page(createPage, principal, "Administration hote", rouge);
                object statut = Page(createPage, principal, "Mon personnage", cyan);

                // JOBS PUBLICS
                Add(createFunction, publics, "Citoyen", Equipe.Citoyen, Grade.Citoyen, blanc);
                Add(createFunction, publics, "Eboueur", Equipe.Eboueurs, Grade.Eboueur, vert);
                Add(createFunction, publics, "Banquier", Equipe.Banque, Grade.Guichetier, jaune);
                Add(createFunction, publics, "Poste a la mairie", Equipe.Mairie, Grade.ConseillerMunicipal, cyan);
                Add(createFunction, publics, "Epicier", Equipe.Epicerie, Grade.Vendeur, vert);

                // FACTIONS
                Add(createFunction, factions, "Policier", Equipe.Police, Grade.Policier, bleu);
                Add(createFunction, factions, "Medecin", Equipe.Medecins, Grade.Infirmier, vert);

                object marseillais = Page(createPage, factions, "Les Marseillais", rouge);
                object parisiens = Page(createPage, factions, "Les Parisiens", bleu);
                object voyageurs = Page(createPage, factions, "Les Gens du Voyage", jaune);

                Add(createFunction, marseillais, "Rejoindre les Marseillais", Equipe.Marseillais, Grade.Membre, rouge);
                Add(createFunction, parisiens, "Rejoindre les Parisiens", Equipe.Parisiens, Grade.Membre, bleu);
                Add(createFunction, voyageurs, "Rejoindre les Gens du Voyage", Equipe.GensDuVoyage, Grade.Membre, jaune);

                // GRADES MAIRIE
                object mairie = Page(createPage, grades, "Mairie", jaune);
                Add(createFunction, mairie, "Conseiller municipal", Equipe.Mairie, Grade.ConseillerMunicipal, blanc);
                Add(createFunction, mairie, "Secretaire de mairie", Equipe.Mairie, Grade.SecretaireMairie, blanc);
                Add(createFunction, mairie, "Adjoint au maire", Equipe.Mairie, Grade.AdjointMaire, cyan);

                // GRADES POLICE
                object police = Page(createPage, grades, "Police", bleu);
                Add(createFunction, police, "Policier", Equipe.Police, Grade.Policier, blanc);
                Add(createFunction, police, "Sergent", Equipe.Police, Grade.Sergent, cyan);
                Add(createFunction, police, "Lieutenant", Equipe.Police, Grade.LieutenantPolice, cyan);
                Add(createFunction, police, "Commissaire adjoint", Equipe.Police, Grade.CommissaireAdjoint, jaune);

                // GRADES MEDECINS
                object medecins = Page(createPage, grades, "Medecins", vert);
                Add(createFunction, medecins, "Infirmier", Equipe.Medecins, Grade.Infirmier, blanc);
                Add(createFunction, medecins, "Chirurgien", Equipe.Medecins, Grade.Chirurgien, cyan);
                Add(createFunction, medecins, "Medecin adjoint", Equipe.Medecins, Grade.MedecinAdjoint, jaune);

                // GRADES EBOUEURS
                object eboueurs = Page(createPage, grades, "Eboueurs", vert);
                Add(createFunction, eboueurs, "Eboueur", Equipe.Eboueurs, Grade.Eboueur, blanc);
                Add(createFunction, eboueurs, "Responsable de collecte", Equipe.Eboueurs, Grade.ResponsableCollecte, cyan);

                // GRADES BANQUE
                object banque = Page(createPage, grades, "Banque", jaune);
                Add(createFunction, banque, "Guichetier", Equipe.Banque, Grade.Guichetier, blanc);
                Add(createFunction, banque, "Conseiller bancaire", Equipe.Banque, Grade.ConseillerBancaire, cyan);
                Add(createFunction, banque, "Directeur adjoint", Equipe.Banque, Grade.DirecteurAdjointBanque, jaune);

                // GRADES EPICERIE
                object epicerie = Page(createPage, grades, "Epicerie", vert);
                Add(createFunction, epicerie, "Vendeur", Equipe.Epicerie, Grade.Vendeur, blanc);
                Add(createFunction, epicerie, "Responsable de rayon", Equipe.Epicerie, Grade.ResponsableRayon, cyan);
                Add(createFunction, epicerie, "Gerant adjoint", Equipe.Epicerie, Grade.GerantAdjoint, jaune);

                // HIERARCHIE DES TROIS GANGS
                AjouterGradesGang(createPage, createFunction, grades,
                    "Grades Marseillais", Equipe.Marseillais, rouge,
                    blanc, cyan, jaune);

                AjouterGradesGang(createPage, createFunction, grades,
                    "Grades Parisiens", Equipe.Parisiens, bleu,
                    blanc, cyan, jaune);

                AjouterGradesGang(createPage, createFunction, grades,
                    "Grades Gens du Voyage", Equipe.GensDuVoyage, jaune,
                    blanc, cyan, jaune);

                // NOMINATIONS RESERVEES A L'HOTE
                AddHost(createFunction, admin, "Nommer Maire", Equipe.Mairie, Grade.Maire, jaune);
                AddHost(createFunction, admin, "Nommer Chef de police", Equipe.Police, Grade.ChefPolice, bleu);
                AddHost(createFunction, admin, "Nommer Medecin-chef", Equipe.Medecins, Grade.MedecinChef, vert);
                AddHost(createFunction, admin, "Nommer Chef des eboueurs", Equipe.Eboueurs, Grade.ChefEboueurs, vert);
                AddHost(createFunction, admin, "Nommer Directeur de banque", Equipe.Banque, Grade.DirecteurBanque, jaune);
                AddHost(createFunction, admin, "Nommer Gerant epicerie", Equipe.Epicerie, Grade.GerantEpicerie, vert);

                AddHost(createFunction, admin, "Nommer Patron Marseillais", Equipe.Marseillais, Grade.Patron, rouge);
                AddHost(createFunction, admin, "Nommer Patron Parisien", Equipe.Parisiens, Grade.Patron, bleu);
                AddHost(createFunction, admin, "Nommer Patron Gens du Voyage", Equipe.GensDuVoyage, Grade.Patron, jaune);

                // STATUT PERSONNEL
                createFunction.Invoke(statut, new object[] {
                    "Afficher mon equipe",
                    cyan,
                    (Action)(() => AfficherStatut())
                });

                createFunction.Invoke(statut, new object[] {
                    "Afficher mon grade",
                    cyan,
                    (Action)(() => AfficherStatut())
                });

                _menuCree = true;
                MelonLogger.Msg("[Fusion DarkRP] Menu 1.3.0 cree avec succes.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[Fusion DarkRP] Erreur BoneMenu : " + ex);
            }
        }

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
            object gang = Page(createPage, parent, nom, couleur);

            Add(createFunction, gang, "Membre", equipe, Grade.Membre, blanc);
            Add(createFunction, gang, "Associe", equipe, Grade.Associe, blanc);
            Add(createFunction, gang, "Lieutenant", equipe, Grade.LieutenantGang, cyan);
            Add(createFunction, gang, "Bras droit", equipe, Grade.BrasDroit, jaune);
        }

        private static object Page(
            MethodInfo method,
            object parent,
            string nom,
            object couleur)
        {
            return method.Invoke(parent, new object[] { nom, couleur, 0, true });
        }

        private static void Add(
            MethodInfo method,
            object page,
            string label,
            Equipe equipe,
            Grade grade,
            object couleur)
        {
            if (page == null) return;

            method.Invoke(page, new object[] {
                label,
                couleur,
                (Action)(() => ChangerPosteLocal(equipe, grade))
            });
        }

        private static void AddHost(
            MethodInfo method,
            object page,
            string label,
            Equipe equipe,
            Grade grade,
            object couleur)
        {
            if (page == null) return;

            method.Invoke(page, new object[] {
                label,
                couleur,
                (Action)(() => {
                    if (!EstHote())
                    {
                        MelonLogger.Warning("[Fusion DarkRP] Action reservee a l'hote.");
                        return;
                    }

                    ChangerPosteLocal(equipe, grade);
                })
            });
        }

        private static bool EstHote()
        {
            try
            {
                Assembly fusion = null;

                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.GetName().Name == "LabFusion")
                    {
                        fusion = asm;
                        break;
                    }
                }

                Type network = fusion?.GetType("LabFusion.Network.NetworkInfo");

                PropertyInfo prop = network?.GetProperty(
                    "IsServer",
                    BindingFlags.Public | BindingFlags.Static
                );

                return prop != null && (bool)prop.GetValue(null);
            }
            catch
            {
                return false;
            }
        }

        private static void ChangerPosteLocal(Equipe equipe, Grade grade)
        {
            MonEquipe = equipe;
            MonGrade = grade;

            MelonLogger.Msg(
                "[Fusion DarkRP] Poste local : " + equipe + " / " + grade
            );
        }

        private static void AfficherStatut()
        {
            MelonLogger.Msg("[Fusion DarkRP] Equipe : " + MonEquipe);
            MelonLogger.Msg("[Fusion DarkRP] Grade : " + MonGrade);
        }
    }
}
