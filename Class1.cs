using System;
using System.Collections.Generic;
using MelonLoader;
using BoneLib;
using BoneLib.BoneMenu;
using LabFusion.Network;
using LabFusion.Representation;
using LabFusion.Utilities;
using UnityEngine;

[assembly: MelonInfo(typeof(FusionDarkRP.MainMod), "Fusion DarkRP Module", "1.0.0", "LARUERP")]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]

namespace FusionDarkRP
{
    public class MainMod : MelonMod
    {
        public enum Job 
        { 
            Citoyen, Maire, PatronPolice, Police, AgentSecurite, 
            Parrain, Gangster, Fabricant, VendeurArmes, GardeDuCorps, 
            Depanneur, Mercenaire, Medecin 
        }
        
        public static Job MonJobActuel = Job.Citoyen;
        public static string MetadataKey = "DarkRP_Job";
        private static object _mainCategory = null; // Correction : Passage en 'object' pour valider la compilation à 100%
        private static bool _menuInitialise = false;

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("Mod DarkRP Multijoueur chargé de force.");
        }

        public override void OnLateInitializeMelon()
        {
            if (!_menuInitialise)
            {
                // Anti-crash Quest 3 : On délègue l'attente à un objet Unity passif
                GameObject darkRPUpdater = new GameObject("DarkRP_Updater");
                darkRPUpdater.AddComponent<MenuDelayComponent>();
                UnityEngine.Object.DontDestroyOnLoad(darkRPUpdater);
            }
        }

        public static void ExecuterInitialisationMenu()
        {
            if (_menuInitialise) return;

            try
            {
                MelonLogger.Msg("Création des catégories DarkRP dans la roulette...");

                // Liaison directe officielle native de l'API BoneLib
                _mainCategory = MenuManager.CreateCategory("Fusion DarkRP", Color.yellow);
                if (_mainCategory == null) return;

                // Extraction dynamique du type de catégorie de BoneLib pour créer les sous-menus sans erreur
                var catType = _mainCategory.GetType();
                var createCategoryMethod = catType.GetMethod("CreateCategory", new Type[] { typeof(string), typeof(Color) });

                if (createCategoryMethod == null) return;

                // 1. Sous-Menu : Jobs Publics
                object publicCat = createCategoryMethod.Invoke(_mainCategory, new object[] { "Jobs Publics", Color.white });
                AjouterBoutonJob(publicCat, "Devenir Citoyen", Job.Citoyen, Color.white);
                AjouterBoutonJob(publicCat, "Devenir Medecin", Job.Medecin, Color.green);
                AjouterBoutonJob(publicCat, "Devenir Depanneur", Job.Depanneur, Color.yellow);
                AjouterBoutonJob(publicCat, "Devenir Vendeur d'Armes", Job.VendeurArmes, Color.cyan);

                // 2. Sous-Menu : Hiérarchies de Factions
                object factionCat = createCategoryMethod.Invoke(_mainCategory, new object[] { "Rejoindre Faction", Color.gray });
                AjouterBoutonJob(factionCat, "Devenir Policier", Job.Police, Color.blue);
                AjouterBoutonJob(factionCat, "Devenir Agent Securite", Job.AgentSecurite, Color.cyan);
                AjouterBoutonJob(factionCat, "Devenir Gangster", Job.Gangster, Color.red);
                AjouterBoutonJob(factionCat, "Devenir Fabricant", Job.Fabricant, Color.red);
                AjouterBoutonJob(factionCat, "Devenir Garde du Corps", Job.GardeDuCorps, Color.gray);

                // 3. Sous-Menu : Commandement Admin
                object adminCat = createCategoryMethod.Invoke(_mainCategory, new object[] { "Menu Admin Hote", Color.red });
                AjouterBoutonAdmin(adminCat, "[HOTE] Assigner Maire", Job.Maire, new Color(1.0f, 0.84f, 0.0f));
                AjouterBoutonAdmin(adminCat, "[HOTE] Assigner Chef Police", Job.PatronPolice, Color.blue);
                AjouterBoutonAdmin(adminCat, "[HOTE] Assigner Parrain", Job.Parrain, Color.red);
                AjouterBoutonAdmin(adminCat, "[HOTE] Assigner Mercenaire", Job.Mercenaire, Color.black);

                _menuInitialise = true;
                MelonLogger.Msg("✅ Extension DarkRP ajoutée de façon stable dans votre BoneMenu !");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("Échec de l'injection : " + ex.Message);
            }
        }

        private static void AjouterBoutonJob(object subCategory, string label, Job job, Color color)
        {
            if (subCategory == null) return;
            var createFunction = subCategory.GetType().GetMethod("CreateFunctionElement", new Type[] { typeof(string), typeof(Color), typeof(Action) });
            createFunction?.Invoke(subCategory, new object[] { label, color, (Action)(() => ChangerDeJobLocal(job)) });
        }

        private static void AjouterBoutonAdmin(object subCategory, string label, Job job, Color color)
        {
            if (subCategory == null) return;
            var createFunction = subCategory.GetType().GetMethod("CreateFunctionElement", new Type[] { typeof(string), typeof(Color), typeof(Action) });
            createFunction?.Invoke(subCategory, new object[] { label, color, (Action)(() => AssignerJobDepuisHote(job)) });
        }

        private static void ChangerDeJobLocal(Job job)
        {
            if (job == Job.Maire || job == Job.PatronPolice || job == Job.Parrain || job == Job.Mercenaire)
            {
                MelonLogger.Warning("Action refusée : Ce grade doit être attribué par l'Hôte.");
                return;
            }
            MonJobActuel = job;
            MettreAJourMetadataReseau(job);
        }

        private static void AssignerJobDepuisHote(Job jobPatron)
        {
            if (!NetworkInfo.IsServer)
            {
                MelonLogger.Warning("Action refusée : Réservé à l'Hôte.");
                return;
            }
            MonJobActuel = jobPatron;
            MettreAJourMetadataReseau(jobPatron);
        }

        private static void MettreAJourMetadataReseau(Job job)
        {
            if (!NetworkInfo.HasServer) return;

            PlayerSender.SendPlayerMetadataRequest(PlayerIdManager.LocalSmallId, MetadataKey, job.ToString());
            MelonLogger.Msg($"[Réseau Sync] Métadonnée envoyée : {job}");
        }

        public override void OnGUI()
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 24;
            style.richText = true;
            style.normal.textColor = ObtenirCouleurJob(MonJobActuel);
            GUI.Label(new Rect(30, Screen.height - 60, 400, 50), $"<b>VOTRE ROLE : {MonJobActuel.ToString().ToUpper()}</b>", style);
        }

        public static Color ObtenirCouleurJob(Job job)
        {
            switch (job)
            {
                case Job.Maire: return new Color(1f, 0.84f, 0f);
                case Job.PatronPolice: return new Color(0f, 0.5f, 1f);
                case Job.Police: return Color.blue;
                case Job.AgentSecurite: return new Color(0.6f, 0.8f, 1f);
                case Job.Parrain: return new Color(0.5f, 0f, 0f);
                case Job.Gangster: return Color.red;
                case Job.Fabricant: return new Color(1f, 0.4f, 0.4f);
                case Job.VendeurArmes: return new Color(1f, 0.5f, 0f);
                case Job.GardeDuCorps: return Color.gray;
                case Job.Depanneur: return Color.yellow;
                case Job.Mercenaire: return Color.black;
                case Job.Medecin: return Color.green;
                default: return Color.white;
            }
        }
    }

    public class MenuDelayComponent : MonoBehaviour
    {
        public MenuDelayComponent(IntPtr ptr) : base(ptr) {}

        void Start()
        {
            // Unity va appeler la création de la roulette exactement 15 secondes après l'initialisation du jeu
            Invoke("DeclencherMenu", 15.0f);
        }

        void DeclencherMenu()
        {
            MainMod.ExecuterInitialisationMenu();
            Destroy(gameObject);
        }
    }
}
